using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using InfoBaseListDataClasses;
using InfoBaseListUDPServerNamespace;
// ReSharper disable LoopCanBeConvertedToQuery

namespace InfoBaseListManager
{
    public class InfoBaseTree
    {
        public InfoBase InfoBase { get; set; }
        public string InfoBaseName { get { return InfoBase.InfobaseName; } }
        public ObservableCollection<InfoBaseTree> ChildInfoBases {get; set;}
        public InfoBaseTree Folder { get; set; }     
 
        // ReSharper disable once InconsistentNaming
        public string IBText { get { return InfoBase.InfobaseName; } }
                
        public InfoBaseTree(InfoBase ib, InfoBaseTree folder)
        {
            ChildInfoBases = new ObservableCollection<InfoBaseTree>();
            InfoBase = ib;
            Folder = folder;            
        }

        public void FillInfoBaseListRecursive(List<InfoBase> ibList)
        {
            if (InfoBase.InfobaseName != "/")
            {
                ibList.Add(new InfoBase(InfoBase));
            }
            
            foreach(var child in ChildInfoBases)
            {
                child.FillInfoBaseListRecursive(ibList);
            }
        }

        public void AddInfoBase(InfoBase infobase, List<InfoBase> ibList)
        {
            InfoBaseTree folder;
            string folderName = "";

            var u = Search(infobase);

            if (u == null)
            {
                if (infobase.Folder != "/")
                {
                    for (int i = infobase.Folder.Length - 1; i >= 0; i--)
                    {
                        if (infobase.Folder[i] != '/')
                            folderName = infobase.Folder[i].ToString() + folderName;
                        else
                            break;
                    }

                    folder = Search(folderName);
                }
                else
                {
                    folder = Search("/");
                }

                if (folder == null)
                {
                    // ReSharper disable once InconsistentNaming
                    var folderIB = ibList.SingleOrDefault(i => i.InfobaseName == folderName);
                    AddInfoBase(folderIB, ibList);
                    folder = Search(folderName);
                }

                Application.Current.Dispatcher.Invoke(new Action(() => folder.ChildInfoBases.Add(new InfoBaseTree(infobase, folder))));
            } else
            {

            }
        }

        public void RemoveNotInList(List<InfoBase> infobases,ref Dictionary<InfoBaseTree,InfoBaseTree> toRemove)
        {
            foreach(var child in ChildInfoBases)
            {
                child.RemoveNotInList(infobases,ref toRemove);
            }

            if (InfoBase.InfobaseName == "/")
                return;

            if (infobases.SingleOrDefault(i => i.Equals(InfoBase)) == null) 
            {
                toRemove.Add(Folder, this);
            }

        }

        public InfoBaseTree Search(InfoBase ib)
        {
            if (InfoBase.Equals(ib))
                return this;
            
            
            foreach(var child in ChildInfoBases)
            {
                var res = child.Search(ib);

                if (res != null)
                    return res;
            }
            return null;
        }

        public InfoBaseTree Search(string name)
        {
            if (InfoBase.InfobaseName == name)
                return this;


            foreach (var child in ChildInfoBases)
            {
                var res = child.Search(name);

                if (res != null)
                    return res;
            }
            return null;
        }
    }

    public class User : IComparable
    {
        private readonly string _computerName;
        public string UserName { get; set; }
        public InfoBaseTree InfoBaseTree;
        
        public User(string computerName)
        {
            _computerName = computerName;

            var emptyInfoBase = new InfoBase {InfobaseName = "/"};
            InfoBaseTree.InfoBase = emptyInfoBase;            
        }

        public User(string computerName,string uName)
        {
            _computerName = computerName;
            UserName = uName;

            var emptyInfoBase = new InfoBase {InfobaseName = "/"};
            InfoBaseTree = new InfoBaseTree(emptyInfoBase, null);            
        }

        public int CompareTo(Object o)
        {
            var user = o as User;
            if (user != null)
            {
                return String.Compare(UserName, user.UserName, StringComparison.CurrentCultureIgnoreCase);
            }

            return -1;
        }

        public void QueryInfoBases(InfoBaseListUdpServer udpServer)
        {
            DataUnitUserInfoBaseList duc = new DataUnitUserInfoBaseList
            {
                ComputerName = _computerName,
                UserName = UserName,
                Query = DataQueries.UserInfobaseListRequest
            };

            udpServer.Send(duc.ComputerName, duc);
        }
           
        public void LoadInfoBases(List<InfoBase> infoBases)
        {
            foreach (var infobase in infoBases)
            {                
                InfoBaseTree.AddInfoBase(infobase, infoBases);
            }

            var toRemove = new Dictionary<InfoBaseTree, InfoBaseTree>();
            InfoBaseTree.RemoveNotInList(infoBases,ref toRemove);

            foreach(var ibTree in toRemove)
            {
                var tree = ibTree;
                Application.Current.Dispatcher.Invoke(new Action(() => tree.Key.ChildInfoBases.Remove(tree.Value)));
            }
        }

        public void PushInfoBases(InfoBaseListUdpServer udpServer)
        {
            List<InfoBase> infoBaseList = new List<InfoBase>();

            InfoBaseTree.FillInfoBaseListRecursive(infoBaseList);

            DataUnitUserInfoBaseList duc = new DataUnitUserInfoBaseList
            {
                ComputerName = _computerName,
                UserName = UserName,
                Query = DataQueries.UserInfobaseListPush,
                InfoBaseList = infoBaseList
            };
            udpServer.Send(duc.ComputerName, duc);
        }


    }

    public class Computer : INotifyPropertyChanged, IComparable
    {
        public string ComputerName { get; set; }

        public ObservableCollection<User> Users = new ObservableCollection<User>();

        private bool _isOnline;
        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                if (_isOnline == value)
                    return;
                _isOnline = value;
                if (!_isOnline)
                {
                    Users.Clear();
                } 
                OnProperyChanged(new PropertyChangedEventArgs("IsOnline"));
            }
        }
        
        public int CompareTo(Object o)
        {
            var computer = o as Computer;
            if (computer != null)
            {
                var thisOnline = _isOnline;
                var thatOnline = computer.IsOnline;

                if (thisOnline && !thatOnline)
                    return 1;

                if (!thisOnline && thatOnline)
                    return -1;

                return string.Compare(ComputerName, computer.ComputerName, StringComparison.CurrentCultureIgnoreCase);

            }

            return -1;
        }

        public void QueryUsers(InfoBaseListUdpServer udpServer)
        {
            DataUnitComputer duc = new DataUnitComputer
            {
                ComputerName = ComputerName,
                Query = DataQueries.UserListRequest
            };

            udpServer.Send(duc.ComputerName, duc);
        }

        public void LoadUsers(List<string> users)
        {
            foreach (var userName in users)
            {
                var u = Users.SingleOrDefault(i => i.UserName == userName);

                if (u == null)
                {
                    var name = userName;
                    Application.Current.Dispatcher.Invoke(new Action(() => Users.Add(new User(ComputerName,name))));
                }
            }

            var toRemove = new List<User>();
            foreach (var user in Users)
            {
                if(users.SingleOrDefault(i=> i==user.UserName) == null)
                    toRemove.Add(user);                    
            }

            foreach(var user in toRemove)
            {
                var user1 = user;
                Application.Current.Dispatcher.Invoke(new Action(() => Users.Remove(user1)));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnProperyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
    }
}
