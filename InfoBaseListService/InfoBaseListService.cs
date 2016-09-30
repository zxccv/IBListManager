using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceProcess;
using System.Threading;
using InfoBaseListDataClasses;

namespace InfoBaseListService
{
    public partial class InfoBaseListService : ServiceBase
    {
        private string _host;
        private int _port;
        private string _poolName;
        private DateTime _lastMessageFromServerTime;
        private Timer _pingServerTimer;
        private ComputerData _computerData;

        private Queue _dataQueue;
        private Queue _syncQueue;

        private UdpClient _udpClient;
        Thread _receiveThread;
        Thread _parseThread;        

        public InfoBaseListService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Thread.Sleep(10000);

            _dataQueue = new Queue();
            _syncQueue = Queue.Synchronized(_dataQueue);
                        
            _host = Program.Args[0];
            _port = Convert.ToInt32(Program.Args[1]);
            _poolName = Program.Args[2];

            _udpClient = new UdpClient();            

            PingServer(null);

            _udpClient.Client.ReceiveTimeout = 3000;
            
            _receiveThread = new Thread(ReceiveData);
            _receiveThread.Start();

            _parseThread = new Thread(ParseData);
            _parseThread.Start();

             //_parseDataTimer = new Timer(ParseData, null, 15, 10);
            _pingServerTimer = new Timer(PingServer,null,1000,1000);

            RegistryClass.GetPrivileges();

            _computerData = new ComputerData();
        }

        protected override void OnStop()
        {
            //_parseDataTimer.Dispose();
            _pingServerTimer.Dispose();

            _receiveThread.Abort();

            _parseThread.Abort();
        }
                
        private void ReceiveData()
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            
            while (true)
            {
                try
                {
                    Byte[] receivedBytes = _udpClient.Receive(ref ep);

                    var ms = new MemoryStream(receivedBytes);
                    var bf = new BinaryFormatter();

                    var newDataUnit = bf.Deserialize(ms);

                    if (!(newDataUnit is DataUnitComputer))
                        return;

                    _lastMessageFromServerTime = DateTime.Now;

                    _syncQueue.Enqueue(newDataUnit);
                }
                catch (Exception) { };           

            }            
        }

        private void ParseData()
        {
            while (true)
            {
                if (_syncQueue.Count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                DataUnitComputer duc = _syncQueue.Dequeue() as DataUnitComputer;

                DataUnitUserInfoBaseList du;

                switch (duc.Query)
                {
                    case DataQueries.Ping:
                        var ducSend = new DataUnitComputer();
                        ducSend.ComputerName = duc.ComputerName;
                        ducSend.Query = DataQueries.Pong;
                        Send(ducSend);
                        break;
                    case DataQueries.UserListRequest:
                        Send(_computerData.GetUsers());
                        break;
                    case DataQueries.UserInfobaseListRequest:
                        du = (DataUnitUserInfoBaseList)duc;
                        Send(_computerData.GetUserInfoBases(du.UserName));
                        break;
                    case DataQueries.UserInfobaseListPush:
                        du = (DataUnitUserInfoBaseList)duc;
                        _computerData.SetUserInfoBases(du.UserName, du.InfoBaseList);
                        break;
                    default:
                        break;
                }

            }
        }
                
        private void PingServer(object obj)
        {
            DataUnitComputer duc = new DataUnitComputer();
            duc.ComputerName = Environment.MachineName;
            duc.Query = DataQueries.Ping;

            Send(duc);            
        }

        private void Send(DataUnitComputer duc)
        {
            if(duc.PoolName == null || duc.PoolName.Equals(""))
            {
                duc.PoolName = _poolName;
            }

            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, duc);
            byte[] buf = ms.GetBuffer();

            _udpClient.Send(buf, buf.Length, _host, _port);
        }
    }
}
