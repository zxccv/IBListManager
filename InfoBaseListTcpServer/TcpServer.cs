using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using InfoBaseListDataClasses;

namespace InfoBaseListTcpServerNamespace
{
    public class ActiveConnection
    {
        public TcpClient Client;
        public DateTime LastMessageTime;
        public string ComputerName;
    }

    public class Query
    {
        public string ComputerName;
        public object DataUnit;
    }

    public class InfoBaseListTcpServer
    {
        private int _port;

        private TcpListener _tcpListener;
        private Timer _tcpListenerPendingTimer;
        private Timer _dropDeadConnectionsTimer;
        private Timer _getDataTimer;
        private Timer _sendPingTimer;

        public List<ActiveConnection> ConnectionList;

        public List<Query> DataList; 

        public InfoBaseListTcpServer()
        {
        }

        public void Start(int port)
        {
            if(_tcpListener != null) throw new Exception("Сервер уже запущен");
            _port = port;
            var ip = new IPAddress(0);

            _tcpListener = new TcpListener(ip,_port);
            _tcpListener.Start();

            _tcpListenerPendingTimer = new Timer(AcceptPendingConnections,null,0,100);
            _dropDeadConnectionsTimer = new Timer(DropDeadConnections,null,5000,5000);
            _getDataTimer = new Timer(GetDataFromConnections,null,30,30);
            _sendPingTimer = new Timer(SendPingRequests,null,5000,5000);

            ConnectionList = new List<ActiveConnection>();
            DataList = new List<Query>();
        }

        public void Stop()
        {
            if(_tcpListener == null) throw new Exception("Сервер не запущен");

            _tcpListener.Stop();
            _tcpListener = null;

            _tcpListenerPendingTimer.Dispose();
            _dropDeadConnectionsTimer.Dispose();
            _getDataTimer.Dispose();
            _sendPingTimer.Dispose();
        }

        private void AcceptPendingConnections(object obj)
        {
            while (_tcpListener.Pending())
            {
                lock (ConnectionList)
                {
                    var tcpClient = _tcpListener.AcceptTcpClient();

                    var newConnection = new ActiveConnection();
                    newConnection.Client = tcpClient;
                    newConnection.LastMessageTime = DateTime.Now;
                    
                    ConnectionList.Add(newConnection);

                    SendDataUnit(newConnection,new DataUnitQuery(){Query = DataQueries.ComputerNameRequest});
                }
            }
        }

        private void GetDataFromConnections(object obj)
        {
            foreach (var activeConnection in ConnectionList.ToArray())
            {
                while (activeConnection.Client.Connected && activeConnection.Client.Available > 0)
                {
                    try
                    {
                        int av = activeConnection.Client.Available;
                        var bf = new BinaryFormatter();
                        var ns = activeConnection.Client.GetStream();

                        var newDataUnit = bf.Deserialize(ns);

                        if (!(newDataUnit is DataUnitQuery))
                            continue;

                        av++;

                        activeConnection.LastMessageTime = DateTime.Now;

                        var duQuery = (DataUnitQuery) newDataUnit;

                        if (duQuery.Query == DataQueries.Pong || duQuery.Query == DataQueries.ComputerNameAnswer)
                        {
                            if (duQuery.Query == DataQueries.ComputerNameAnswer)
                            {

                                activeConnection.ComputerName = ((DataUnitComputer) duQuery).ComputerName;
                            }
                        }
                        else
                        {
                            lock (DataList)
                            {
                                var newQuery = new Query()
                                {
                                    DataUnit = newDataUnit,
                                    ComputerName = activeConnection.ComputerName
                                };

                                DataList.Add(newQuery);
                            }    
                        }

                            
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
            
        }

        private void DropDeadConnections(object obj)
        {
            lock (ConnectionList)
            {
                var toRemove = new List<ActiveConnection>();

                foreach (var activeConnection in ConnectionList)
                {
                    var ts = DateTime.Now - activeConnection.LastMessageTime;
                    if(ts.TotalSeconds > 10)
                        toRemove.Add(activeConnection);
                }

                foreach (var activeConnection in toRemove)
                {
                    ConnectionList.Remove(activeConnection);
                }
            }
        }

        private void SendPingRequests(object obj)
        {
            var du = new DataUnitQuery(){Query = DataQueries.Ping};
            foreach (var activeConnection in ConnectionList.ToArray())
            {
                SendDataUnit(activeConnection,du);
            }
        }

        private void SendDataUnit(ActiveConnection activeConnection, object dataUnit, string id = "")
        {
            var du = dataUnit as DataUnitQuery;
            if (du != null)
            {
                if (id == "")
                {
                    id = Guid.NewGuid().ToString();
                }
                du.Id = id;
            }
            try
            {
                var bf = new BinaryFormatter();
                var ns = activeConnection.Client.GetStream();

                bf.Serialize(ns,dataUnit);
            }
            catch (Exception)
            {
                
            }   
        }

        public void SendDataUnit(string computerName, object dataUnit, string id = "")
        {
            foreach (var activeConnection in ConnectionList)
            {
                if (activeConnection.ComputerName == computerName)
                {
                    SendDataUnit(activeConnection,dataUnit,id);
                    return;
                }
            }
        }

        public DataUnitQuery SendDataRequestAndReturnAnswer(string computerName, DataUnitQuery request, int tryCount = 20)
        {
            Guid reqGuid = Guid.NewGuid();

            SendDataUnit(computerName,request,reqGuid.ToString());

            for (int i = 0; i < tryCount; i++)
            {
                //lock (DataList)
                //{
                    //GetDataFromConnections(null);
                    foreach (var query in DataList.ToArray())
                    {
                        
                        var du = query.DataUnit as DataUnitQuery;
                        if(du.Id == reqGuid.ToString())
                        {
                            DataList.Remove(query);
                            return du;
                        }
                    }
                //}
                Thread.Sleep(50);
                //Monitor.Wait(ConnectionList);
            }

            //Thread.Sleep(15000);

            return null;

        }


    }
}
