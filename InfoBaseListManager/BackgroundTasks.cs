using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using InfoBaseListDataClasses;
using InfoBaseListUDPServerNamespace;

namespace InfoBaseListManager
{
    class BackgroundTasks
    {
        private readonly InfoBaseListUdpServer _udpServer;
        private readonly ObservableCollection<Computer> _comps;
        private readonly ICollectionView _cvComps;
                
        private Thread _compsUpdateThread;
        private Thread _parseThread;

        public BackgroundTasks(InfoBaseListUdpServer udpServer, ObservableCollection<Computer> comps, ICollectionView cvComps)
        {
            _udpServer = udpServer;
            _comps = comps;
            _cvComps = cvComps;
        }

        public void Start()
        {
            _compsUpdateThread = new Thread(UpdateCompsThread);
            _compsUpdateThread.Start();
            _parseThread = new Thread(ParseUdpServerQueueThread);
            _parseThread.Start();
        }

        public void Stop()
        {
            if (_compsUpdateThread.IsAlive)
            { 
                _compsUpdateThread.Abort();
                _compsUpdateThread.Join();
            }

            if (_parseThread.IsAlive)
            { 
                _parseThread.Abort();
                _parseThread.Join();
            }
        }
               
        private void UpdateCompsThread()
        {            
            while (true)
            {
                try
                {
                    var dtNow = DateTime.Now;
                    foreach (var connComp in _udpServer.ConnectedComputers)
                    {
                        Computer c = _comps.SingleOrDefault(i => i.ComputerName == connComp.Key);

                        if (c != null)
                        {
                            var onl = !(connComp.Value.LastMessageTime + TimeSpan.FromSeconds(2) < dtNow);
                            if (onl && !c.IsOnline)
                            {
                                c.QueryUsers(_udpServer);
                            }
                            Application.Current.Dispatcher.Invoke(new Action(() => c.IsOnline = onl));
                        }
                        else
                        {
                            c = new Computer {ComputerName = connComp.Key};
                            var onl = !(connComp.Value.LastMessageTime + TimeSpan.FromSeconds(2) < dtNow);
                            c.IsOnline = onl;
                            Application.Current.Dispatcher.Invoke(new Action(() => _comps.Add(c)));
                        }
                    }

                    Application.Current.Dispatcher.Invoke(new Action(() => _cvComps.Refresh()));

                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    // ignored
                }
                
            }

        }
                    
        private void ParseUdpServerQueueThread()
        {
            while (true)
            {
                try
                {
                    if (_udpServer.SyncDataQueue.Count == 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    object nextDataUnit = _udpServer.SyncDataQueue.Dequeue();

                    if (!(nextDataUnit is DataUnitComputer))
                        continue;

                    var duc = nextDataUnit as DataUnitComputer;

                    var comp = _comps.SingleOrDefault(i => i.ComputerName == duc.ComputerName);

                    if (comp == null)
                        continue;

                    switch (duc.Query)
                    {
                        case DataQueries.UserListAnswer:
                            var dataUnitUserList = duc as DataUnitUserList;
                            if (dataUnitUserList != null) comp.LoadUsers(dataUnitUserList.Users);
                            break;
                        case DataQueries.UserInfobaseListAnswer:
                            // ReSharper disable once InconsistentNaming
                            var duIBList = duc as DataUnitUserInfoBaseList;
                            if (duIBList != null)
                            {
                                var user = comp.Users.SingleOrDefault(i => i.UserName == duIBList.UserName);

                                if (user != null)
                                {
                                    user.LoadInfoBases(duIBList.InfoBaseList);
                                }
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                

            }

        }
    }
}
