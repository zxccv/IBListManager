using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using InfoBaseListDataClasses;

namespace InfoBaseListUDPServerNamespace
{
    public class UdpState
    {
        public UdpClient U;
        public IPEndPoint E;
    }

    public class ConnectedComputer
    {
        public string ComputerName { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastMessageTime { get; set; }
    }

    public class Datagram
    {
        public IPEndPoint EP;
        public byte[] Data;
    }

    public class InfoBaseListUdpServer
    {
        private int _port;
        private string _poolName;
        private readonly Thread _workThread;
        public UdpClient UdpClient;
        public int Port 
        {
            get { return _port; }
        }

        public Dictionary<string, ConnectedComputer> ConnectedComputers;
        public Queue SyncDataQueue;
        
        private readonly Queue _syncSendQueue;

        private readonly Timer _sendTimer;
        private bool _sendTimerActive;
        
        public InfoBaseListUdpServer()
        {
            _workThread = new Thread(Work);
            ConnectedComputers = new Dictionary<string, ConnectedComputer>();
            var dataQueue = new Queue();
            SyncDataQueue = Queue.Synchronized(dataQueue);
            var sendQueue = new Queue();
            _syncSendQueue = Queue.Synchronized(sendQueue);
            _sendTimer = new Timer(SendDatagram);
        }

        private void SendDatagram(object obj)
        {
            if (_syncSendQueue.Count > 0)
            {
                var datagram = _syncSendQueue.Dequeue() as Datagram;
                if (datagram != null) 
                    UdpClient.Send(datagram.Data, datagram.Data.Count(), datagram.EP);
            }
            else
            {
                lock ((object)_sendTimerActive)
                {
                    _sendTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _sendTimerActive = false;
                }
            }
                
        }

        public void Send(string computerName, DataUnitComputer duc)
        {
            if (!ConnectedComputers.ContainsKey(computerName))
                return;

            var cc = ConnectedComputers[computerName];

            var ms = new MemoryStream();

            var bf = new BinaryFormatter();

            bf.Serialize(ms, duc);
            var bytes = ms.GetBuffer();

            var dg = new Datagram() {Data = bytes, EP = cc.EndPoint};

            _syncSendQueue.Enqueue(dg);

            lock ((object) _sendTimerActive)
            {
                if (!_sendTimerActive)
                {
                    _sendTimerActive = true;
                    _sendTimer.Change(0, 1);
                }
            }
        }

        private void Work()
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                try
                {
                    Byte[] receivedBytes = UdpClient.Receive(ref ep);

                    var ms = new MemoryStream(receivedBytes);
                    var bf = new BinaryFormatter();

                    var newDataUnit = bf.Deserialize(ms);

                    if (!(newDataUnit is DataUnitComputer))
                        continue;

                    var poolName = ((DataUnitComputer)newDataUnit).PoolName;

                    if (!_poolName.Equals(poolName))
                        continue;

                    var computerName = ((DataUnitComputer)newDataUnit).ComputerName;

                    if (!ConnectedComputers.ContainsKey(computerName))
                    {
                        var newConnectedComputer = new ConnectedComputer {ComputerName = computerName};
                        ConnectedComputers.Add(computerName, newConnectedComputer);
                    }
                    ConnectedComputers[computerName].EndPoint = ep;
                    ConnectedComputers[computerName].LastMessageTime = DateTime.Now;

                    SyncDataQueue.Enqueue(newDataUnit);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            
        }

        

        public void Start(int port,string poolName)
        {
            _port = port;
            _poolName = poolName;

            UdpClient = new UdpClient(_port) { Client = { ReceiveTimeout = 3000 } };
            
            if (!_workThread.IsAlive)
            {
                _workThread.Start();
            }
        }

        public void Stop()
        {
            if (_workThread.IsAlive)
            {                
                _workThread.Abort();
                _workThread.Join();
                UdpClient.Close();
            }
        }
    }
}
