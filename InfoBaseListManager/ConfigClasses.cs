using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using InfoBaseListDataClasses;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace InfoBaseListManager
{
    [Serializable]
    public class InfoBaseCollection
    {
        private string name;
        private ObservableCollection<InfoBase> infoBaseList;
        
        public string Name { get { return name; } set { name = value; } }
        public ObservableCollection<InfoBase> InfoBaseList { get { return infoBaseList; } set { infoBaseList = value; } }

        public InfoBaseCollection()
        {
            infoBaseList = new ObservableCollection<InfoBase>();
        }
    }
    
    [Serializable]
    public class Pool
    {
        private string name;
        private ObservableCollection<InfoBaseCollection> infoBaseCollectionList;
 
        public string Name { get { return name; } set { name = value; } }
        public ObservableCollection<InfoBaseCollection> InfoBaseCollectionList { get { return infoBaseCollectionList; } set { infoBaseCollectionList = value; } }

        public Pool()
        {
            infoBaseCollectionList = new ObservableCollection<InfoBaseCollection>();
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
    public class Config
    {
        private static Config configurationData = new Config();
        public static Config ConfigurationData { get { return configurationData; } }
        
        protected Config() 
        {
            poolList = new List<Pool>();
            var cfg = Load();
            if(cfg != null)
            { 
                port = cfg.Port;
                poolList = cfg.PoolList;
                currentPool = cfg.CurrentPool;
            }
        }

        private const string fileName = "managerconfig.bin";
        private int port;
        private List<Pool> poolList;
        private Pool currentPool;
        
        public int Port { get { return port; } set { port = value; } }
        public List<Pool> PoolList
        {
            get { return poolList; }
            set { poolList = value; }
        } 
        public Pool CurrentPool
        {
            get { return currentPool; }
            set { currentPool = value; }
        }
        
        public void Save()
        {
            Stream ConfigFileStream = File.Create(fileName);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(ConfigFileStream, this);
            ConfigFileStream.Close();    
        }

        public Config Load()
        {
            if (File.Exists(fileName))
            {
                Stream ConfigFileStream = File.OpenRead(fileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                var cfg = (Config)deserializer.Deserialize(ConfigFileStream);
                ConfigFileStream.Close();
                return cfg;
            }

            return null;
        }
    }

}
