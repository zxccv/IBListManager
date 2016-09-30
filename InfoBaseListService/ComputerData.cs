using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Security.Principal;
using System.Text;
using System.Threading;
using InfoBaseListDataClasses;
using Microsoft.Win32;

namespace InfoBaseListService
{
    class WindowsUser
    {
        public string UserName = "";
        public string UserAppFolder = "";
        public string UserInfoBasesFile = "";
    }

    class ComputerData
    {
        private List<WindowsUser> windowsUsers;

        private Timer _getDataFromRegistryTimer;

        public ComputerData()
        {
            windowsUsers = new List<WindowsUser>();
            _getDataFromRegistryTimer = new Timer(GetDataFromRegistry,null,0,60000);
        }

        private string GetUserNameBySID(string stringSid)
        {
            return new SecurityIdentifier(stringSid).Translate(typeof(NTAccount)).ToString();
        }

        public void GetDataFromRegistry(object obj)
        {
            var rhklm = Registry.LocalMachine;
            var rku = Registry.Users;

            var rkprofiles = rhklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");

            var profileslist = rkprofiles.GetSubKeyNames();

            foreach (var profileSID in profileslist)
            {
                if(profileSID.Length < 15)
                    continue;

                var userName = "";

                try
                {
                    userName = GetUserNameBySID(profileSID);
                }
                catch (Exception)
                {
                    continue;
                }

                WindowsUser currentUser = null;

                foreach (var windowsUser in windowsUsers)
                {
                    if (windowsUser.UserName == userName)
                    {
                        currentUser = windowsUser;
                        break;
                    }
                }

                if (currentUser == null)
                {
                    currentUser = new WindowsUser();
                    currentUser.UserName = userName;
                }
                else
                {
                    if (!currentUser.UserInfoBasesFile.Equals(""))
                        continue;
                }

                var rkLoadedFromFile = false;

                var rkuser = rku.OpenSubKey(profileSID);
                
                if (rkuser == null)
                {
                    var userFolder =
                        Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\" + profileSID,"ProfileImagePath",null);

                    if(userFolder == null)
                        continue;

                    var fileName = userFolder + "\\NTUSER.DAT";

                    if(!File.Exists(fileName))
                        continue;

                    try
                    {
                        RegistryClass.LoadUser(profileSID, fileName);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    rkuser = rku.OpenSubKey(profileSID);
                    rkLoadedFromFile = true;

                    if (rkuser == null)
                    {
                        try
                        {
                            RegistryClass.UnloadUser(profileSID);
                        }
                        catch (Exception)
                        {
                            continue;
                        }   
                    }

                }

                var rkUserShellfolders = rkuser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders");

                if(rkUserShellfolders == null)
                    continue;

                currentUser.UserAppFolder = (string)rkUserShellfolders.GetValue("AppData");

                if (!Directory.Exists(currentUser.UserAppFolder))
                    continue;

                if (File.Exists(currentUser.UserAppFolder + @"\1C\1CEStart\ibases.v8i"))
                    currentUser.UserInfoBasesFile = currentUser.UserAppFolder + @"\1C\1CEStart\ibases.v8i";
                
                windowsUsers.Add(currentUser);

                rkUserShellfolders.Close();
                rkuser.Close();
                
                if (rkLoadedFromFile)
                {
                    try
                    {
                        RegistryClass.UnloadUser(profileSID);
                    }
                    catch (Exception)
                    {
                        continue;
                    }     
                }
            }
        }

        public DataUnitUserList GetUsers()
        {
            var duUserList = new DataUnitUserList();

            duUserList.Query = DataQueries.UserListAnswer;
            duUserList.ComputerName = Environment.MachineName;

            foreach (var windowsUser in windowsUsers)
            {
                duUserList.Users.Add(windowsUser.UserName);
            }

            return duUserList;
        }

        public DataUnitUserInfoBaseList GetUserInfoBases(string userName)
        {
            var duInfoBaseList = new DataUnitUserInfoBaseList();

            duInfoBaseList.ComputerName = Environment.MachineName;
            duInfoBaseList.Query = DataQueries.UserInfobaseListAnswer;
            duInfoBaseList.UserName = userName;
            var userInfoBasesFile = "";
            foreach (var windowsUser in windowsUsers)
            {
                if (windowsUser.UserName == userName)
                {
                    userInfoBasesFile = windowsUser.UserInfoBasesFile;
                    break;
                }
            }

            if (userInfoBasesFile != "")
                duInfoBaseList.InfoBaseList = LoadInfoBasesFromFile(userInfoBasesFile);

            return duInfoBaseList;
        }

        public void SetUserInfoBases(string userName,List<InfoBase> infoBaseList)
        {
            var userInfoBasesFile = "";
            foreach (var windowsUser in windowsUsers)
            {
                if (windowsUser.UserName == userName)
                {
                    userInfoBasesFile = windowsUser.UserInfoBasesFile;
                    break;
                }
            }

            if (userInfoBasesFile == "")
                return;

            SaveInfoBasesToFile(userInfoBasesFile, infoBaseList);
        }
            
        private string[] EditInfoBaseInFile(string[] fileLines,InfoBase ib)
        {

            var typeInfoBase = typeof(InfoBase);
            var typeInfoBaseFields = typeInfoBase.GetProperties();

            var fileLinesList = new List<string>(fileLines);
            var first = -1;
            var last = -1;
            for (var i = 0; i < fileLinesList.Count; i++)
            {
                if (first == -1 && fileLinesList[i].Equals("[" + ib.InfobaseName + "]"))
                { 
                    first = i;
                    continue;
                }

                if (first != -1 && fileLinesList[i].StartsWith("["))
                {
                    last = i - 1;
                    break;
                }
            }

            var ibLinesList = new List<string>();
                        
            if (first != -1)
            {
                if (last == -1)
                    last = fileLinesList.Count - 1;
                for (var i = last; i >= first; i--)
                {
                    ibLinesList.Insert(0,fileLinesList[i]);
                    fileLinesList.RemoveAt(i);
                }
            }

            if(ibLinesList.Count == 0)
            {
                ibLinesList.Add("[" + ib.InfobaseName + "]");
            };

            foreach (var typeInfoBaseField in typeInfoBaseFields)
            {
                if (typeInfoBaseField.Name.Equals("InfobaseName"))
                    continue;

                var foundIndex = -1;
                for (var i = 0; i < ibLinesList.Count; i++ )
                {
                    if(!ibLinesList[i].Contains("="))
                        continue;

                    var fieldname = ibLinesList[i].Substring(0, ibLinesList[i].IndexOf("="));

                    if (typeInfoBaseField.Name.Equals(fieldname))
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex == -1)
                {
                    ibLinesList.Add("");
                    foundIndex = ibLinesList.Count - 1;
                }; 

                ibLinesList[foundIndex] = typeInfoBaseField.Name + "=" + (string)typeInfoBaseField.GetValue(ib, null);                
            }

            if(first != -1)
                fileLinesList.InsertRange(first, ibLinesList);
            else
                fileLinesList.AddRange(ibLinesList);

            return fileLinesList.ToArray();
        }

        private string[] RemoveInfoBaseFromFile(string[] fileLines,InfoBase ib)
        {
            var fileLinesList = new List<string>(fileLines);
            var first = -1;
            var last = -1;
            for (var i = 0; i < fileLinesList.Count; i++)
            {
                if (first == -1 && fileLinesList[i].Equals("[" + ib.InfobaseName + "]"))
                {
                    first = i;
                    continue;
                }

                if (first != -1 && fileLinesList[i].StartsWith("["))
                {
                    last = i - 1;
                    break;
                }
            }
                        
            if (first != -1)
            {
                if (last == -1)
                    last = fileLinesList.Count - 1;                
                for (var i = last; i >= first; i--)
                {
                    fileLinesList.RemoveAt(i);
                }
            }

            return fileLinesList.ToArray();
            
        }

        private void SaveInfoBasesToFile(string fileName,List<InfoBase> infoBaseList)
        {
            if (!File.Exists(fileName))
                throw new Exception("Файл с базами не найден");


            var ibListFromFile = LoadInfoBasesFromFile(fileName);
            
            var fileLines = File.ReadAllLines(fileName, Encoding.UTF8);

            var edited = false;
            foreach(var ib in infoBaseList)
            {
                if(!ibListFromFile.Contains(ib))
                {
                    fileLines = EditInfoBaseInFile(fileLines, ib);
                    edited = true;
                    continue;
                }
            }

            foreach(var fileIB in ibListFromFile)
            {
                var baseFound = false;                
                foreach(var ib in infoBaseList)
                {
                    if (ib.InfobaseName.Equals(fileIB.InfobaseName))
                    {
                        baseFound = true;
                        break;
                    }
                }

                if (!baseFound)
                {
                    fileLines = RemoveInfoBaseFromFile(fileLines, fileIB);
                    edited = true;
                }                
            }

            if(edited)
            {
                File.WriteAllLines(fileName,fileLines);
            }
        }

        private List<InfoBase> LoadInfoBasesFromFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new Exception("Файл с базами не найден");

            var fileLines = File.ReadAllLines(fileName, Encoding.UTF8);

            var typeInfoBase = typeof(InfoBase);

            var typeInfoBaseFields = typeInfoBase.GetProperties();

            var ibList = new List<InfoBase>();
                        
            InfoBase currIB = null;
            foreach (var fileLine in fileLines)
            {
                if (fileLine.StartsWith("["))
                {
                    if(currIB != null)
                    {                        
                        ibList.Add(currIB);
                    }

                    currIB = new InfoBase(){InfobaseName = fileLine.Substring(1,fileLine.Length - 2)};
                    continue;
                }

                if(currIB == null)
                    continue;

                var fieldname = fileLine.Substring(0, fileLine.IndexOf("="));
                var fieldvalue = fileLine.Substring(fileLine.IndexOf("=") + 1);

                foreach (var typeInfoBaseField in typeInfoBaseFields)
                {
                    if (typeInfoBaseField.Name.Equals(fieldname))
                    {
                        typeInfoBaseField.SetValue(currIB,fieldvalue,null);
                        break;
                    }
                }

            }

            if (currIB != null)
                ibList.Add(currIB);

            return ibList;

        }


    
    }
}
