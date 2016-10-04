using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using InfoBaseListDataClasses;
using InfoBaseListUDPServerNamespace;


namespace UdpServerTest
{
    class UdpServerConsoleTest
    {
        private static InfoBaseListUdpServer _udpServer;
        private static Dictionary<string, List<string>> _userLists;
        private static Dictionary<string, Dictionary<string, List<InfoBase>>> _userInfoBases;
        
        static void ParseQueue()
        {
            while (true)
            {
                if (_udpServer.SyncDataQueue.Count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                DataUnitComputer duc = _udpServer.SyncDataQueue.Dequeue() as DataUnitComputer;

                Debug.Assert(duc != null, "duc != null");
                switch(duc.Query)
                {
                    case DataQueries.Ping:
                        var ducSend = new DataUnitComputer
                        {
                            ComputerName = duc.ComputerName,
                            Query = DataQueries.Pong
                        };
                        _udpServer.Send(duc.ComputerName,ducSend);
                        break;
                    case DataQueries.UserListAnswer:
                        if (!(duc is DataUnitUserList))
                            break;
                        var duUserList = duc as DataUnitUserList;
                        
                        if(!_userLists.ContainsKey(duc.ComputerName))
                            _userLists.Add(duc.ComputerName,new List<string>());

                        _userLists[duc.ComputerName].Clear();

                        foreach (var username in duUserList.Users)
                        {
                            _userLists[duc.ComputerName].Add(username);
                        }
                        break;
                    case DataQueries.UserInfobaseListAnswer:
                        if (!(duc is DataUnitUserInfoBaseList))
                            break;
                        
                        var duIBList = duc as DataUnitUserInfoBaseList;

                        if (!_userInfoBases.ContainsKey(duc.ComputerName))
                            _userInfoBases.Add(duc.ComputerName, new Dictionary<string, List<InfoBase>>());

                        if (!_userInfoBases[duc.ComputerName].ContainsKey(duIBList.UserName))
                            _userInfoBases[duc.ComputerName].Add(duIBList.UserName, new List<InfoBase>());

                        _userInfoBases[duc.ComputerName][duIBList.UserName].Clear();
                        foreach(var ib in duIBList.InfoBaseList)
                        {
                            _userInfoBases[duc.ComputerName][duIBList.UserName].Add(ib);
                        }

                        break;
                }

            }
        }

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {            
            _udpServer = new InfoBaseListUdpServer();
            _userLists = new Dictionary<string,List<string>>();
            _userInfoBases = new Dictionary<string, Dictionary<string, List<InfoBase>>>();
            
            _udpServer.Start(55300,"");

            Thread th = new Thread(ParseQueue);
            th.Start();

            while (true)
            {
                //Thread.Sleep(100);
                string s = Console.ReadLine();

                if(s=="u")
                {
                    foreach (var connComp in _udpServer.ConnectedComputers)
                    {
                        DataUnitComputer duc = new DataUnitComputer
                        {
                            ComputerName = connComp.Key,
                            Query = DataQueries.UserListRequest
                        };

                        _udpServer.Send(connComp.Key,duc);                        
                    }
                }

                if (s == "b")
                {
                    foreach (var connComp in _udpServer.ConnectedComputers)
                    {
                        if(!_userLists.ContainsKey(connComp.Key))
                            continue;

                        _userInfoBases.Clear();
                        
                        foreach(var username in _userLists[connComp.Key])
                        {

                            DataUnitUserInfoBaseList duc = new DataUnitUserInfoBaseList
                            {
                                ComputerName = connComp.Key,
                                Query = DataQueries.UserInfobaseListRequest,
                                UserName = username
                            };

                            _udpServer.Send(connComp.Key, duc);
                        }                        
                    }
                }

                if (s == "q")
                    break;

                Console.Clear();
                foreach (var connComp in _udpServer.ConnectedComputers)
                {
                    Console.WriteLine("{0} - {1}",connComp.Value.ComputerName,connComp.Value.LastMessageTime);
                    if(_userLists.ContainsKey(connComp.Value.ComputerName))
                    {
                        foreach(var username in _userLists[connComp.Value.ComputerName])
                        {
                            Console.WriteLine("--- {0}", username);

                            if(_userInfoBases.ContainsKey(connComp.Key) && _userInfoBases[connComp.Key].ContainsKey(username))
                            {
                                foreach(var ib in _userInfoBases[connComp.Key][username])
                                {
                                    Console.WriteLine("----->>>> {0}", ib.InfobaseName);
                                }
                            }
                        }
                    }
                }
            }

            //Console.ReadLine();
            th.Abort();
            _udpServer.Stop();

        }
    }
}
