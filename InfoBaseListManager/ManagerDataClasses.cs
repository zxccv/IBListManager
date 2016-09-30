using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using InfoBaseListDataClasses;

namespace InfoBaseListManager
{
    public class InfoBaseTree
    {
        public InfoBase InfoBase { get; set; }
        public string InfoBaseName { get { return InfoBase.InfobaseName; } }
        public ObservableCollection<InfoBaseTree> ChildInfoBases {get; set;}
        public InfoBaseTree Folder { get; set; }     
 
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
                //Application.Current.Dispatcher.Invoke(new Action(() => Folder.ChildInfoBases.Remove(this)));                
            };

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

        public void Sort()
        {
            ChildInfoBases.OrderBy(i => i.InfoBase.InfobaseName);

            foreach (var child in ChildInfoBases)
                child.Sort();
        }
    }

    public class User : IComparable
    {
        private string _computerName;
        public string UserName { get; set; }
        public InfoBaseTree InfoBaseTree;
        
        public User(string computerName)
        {
            _computerName = computerName;
            
            var emptyInfoBase = new InfoBase();
            emptyInfoBase.InfobaseName = "/";
            InfoBaseTree.InfoBase = emptyInfoBase;            
        }

        public User(string computerName,string uName)
        {
            _computerName = computerName;
            UserName = uName;

            var emptyInfoBase = new InfoBase();
            emptyInfoBase.InfobaseName = "/";
            InfoBaseTree = new InfoBaseTree(emptyInfoBase, null);            
        }

        public int CompareTo(Object o)
        {
            if (o is User)
            {
                var u = o as User;
                
                return System.String.Compare(UserName, u.UserName, System.StringComparison.CurrentCultureIgnoreCase);

            }

            return -1;
        }

        public void QueryInfoBases(InfoBaseListUDPServerNamespace.InfoBaseListUdpServer udpServer)
        {
            DataUnitUserInfoBaseList duc = new DataUnitUserInfoBaseList();
            duc.ComputerName = _computerName;
            duc.UserName = UserName;
            duc.Query = DataQueries.UserInfobaseListRequest;

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
                Application.Current.Dispatcher.Invoke(new Action(() => ibTree.Key.ChildInfoBases.Remove(ibTree.Value)));   
            }

            InfoBaseTree.Sort();
        }

        public void PushInfoBases(InfoBaseListUDPServerNamespace.InfoBaseListUdpServer udpServer)
        {
            List<InfoBase> infoBaseList = new List<InfoBase>();

            InfoBaseTree.FillInfoBaseListRecursive(infoBaseList);

            DataUnitUserInfoBaseList duc = new DataUnitUserInfoBaseList();
            duc.ComputerName = _computerName;
            duc.UserName = UserName;
            duc.Query = DataQueries.UserInfobaseListPush;
            duc.InfoBaseList = infoBaseList;
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
            if (o is Computer)
            {
                var c = o as Computer;

                var dt = DateTime.Now;
                var thisOnline = _isOnline;
                var thatOnline = c.IsOnline;

                if (thisOnline && !thatOnline)
                    return 1;

                if (!thisOnline && thatOnline)
                    return -1;

                return System.String.Compare(ComputerName, c.ComputerName, System.StringComparison.CurrentCultureIgnoreCase);

            }

            return -1;
        }

        public void QueryUsers(InfoBaseListUDPServerNamespace.InfoBaseListUdpServer udpServer)
        {
            DataUnitComputer duc = new DataUnitComputer();
            duc.ComputerName = ComputerName;
            duc.Query = DataQueries.UserListRequest;

            udpServer.Send(duc.ComputerName, duc);
        }

        public void LoadUsers(List<string> users)
        {
            foreach (var userName in users)
            {
                var u = Users.SingleOrDefault(i => i.UserName == userName);

                if (u == null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => Users.Add(new User(ComputerName,userName))));
                }
            }

            var toRemove = new List<User>();
            foreach (var user in Users)
            {
                if(users.SingleOrDefault(i=> i==user.UserName) == null)
                    toRemove.Add(user);                    
            }

            foreach(var user in toRemove)
                Application.Current.Dispatcher.Invoke(new Action(() => Users.Remove(user)));

            
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnProperyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
    }
}
