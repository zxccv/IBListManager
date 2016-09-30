using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using InfoBaseListDataClasses;
using InfoBaseListUDPServerNamespace;

namespace InfoBaseListManager
{
    class BackgroundTasks
    {
        private InfoBaseListUdpServer _udpServer;
        private ObservableCollection<Computer> _comps;
        private ICollectionView _cvComps;
                
        private Thread compsUpdateThread;
        private Thread parseThread;

        public BackgroundTasks(InfoBaseListUdpServer udpServer, ObservableCollection<Computer> comps, ICollectionView cvComps)
        {
            _udpServer = udpServer;
            _comps = comps;
            _cvComps = cvComps;
        }

        public void Start()
        {
            compsUpdateThread = new Thread(UpdateCompsThread);
            compsUpdateThread.Start();
            parseThread = new Thread(ParseUdpServerQueueThread);
            parseThread.Start();
        }

        public void Stop()
        {
            if (compsUpdateThread.IsAlive)
            { 
                compsUpdateThread.Abort();
                compsUpdateThread.Join();
            }

            if (parseThread.IsAlive)
            { 
                parseThread.Abort();
                parseThread.Join();
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
                            c = new Computer();
                            c.ComputerName = connComp.Key;
                            var onl = !(connComp.Value.LastMessageTime + TimeSpan.FromSeconds(2) < dtNow);
                            c.IsOnline = onl;
                            Application.Current.Dispatcher.Invoke(new Action(() => _comps.Add(c)));
                        }
                    }

                    Application.Current.Dispatcher.Invoke(new Action(() => _cvComps.Refresh()));

                    Thread.Sleep(1000);
                }
                catch (Exception) { };
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
                            comp.LoadUsers((duc as DataUnitUserList).Users);
                            break;
                        case DataQueries.UserInfobaseListAnswer:
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
                catch (Exception) { };

            }

        }
    }
}
