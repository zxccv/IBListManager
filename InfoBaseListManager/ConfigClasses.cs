using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using InfoBaseListDataClasses;

namespace InfoBaseListManager
{
    [Serializable]
    public class InfoBaseCollection
    {
        private string _name;
        private ObservableCollection<InfoBase> _infoBaseList;
        
        public string Name { get { return _name; } set { _name = value; } }
        public ObservableCollection<InfoBase> InfoBaseList { get { return _infoBaseList; } set { _infoBaseList = value; } }

        public InfoBaseCollection()
        {
            _infoBaseList = new ObservableCollection<InfoBase>();
        }
    }
    
    [Serializable]
    public class Pool
    {
        private string _name;
        private ObservableCollection<InfoBaseCollection> _infoBaseCollectionList;
 
        public string Name { get { return _name; } set { _name = value; } }
        public ObservableCollection<InfoBaseCollection> InfoBaseCollectionList { get { return _infoBaseCollectionList; } set { _infoBaseCollectionList = value; } }

        public Pool()
        {
            _infoBaseCollectionList = new ObservableCollection<InfoBaseCollection>();
        }

        public override string ToString()
        {
            return _name;
        }
    }

    [Serializable]
    public class Config
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Config _configurationData = new Config();
        public static Config ConfigurationData { get { return _configurationData; } }
        
        protected Config() 
        {
            _poolList = new List<Pool>();
            var cfg = Load();
            if(cfg != null)
            { 
                _port = cfg.Port;
                _poolList = cfg.PoolList;
                _currentPool = cfg.CurrentPool;
            }
        }

        private const string FileName = "managerconfig.bin";
        private int _port;
        private List<Pool> _poolList;
        private Pool _currentPool;
        
        public int Port { get { return _port; } set { _port = value; } }
        public List<Pool> PoolList
        {
            get { return _poolList; }
            set { _poolList = value; }
        } 
        public Pool CurrentPool
        {
            get { return _currentPool; }
            set { _currentPool = value; }
        }
        
        public void Save()
        {
            Stream configFileStream = File.Create(FileName);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(configFileStream, this);
            configFileStream.Close();    
        }

        public Config Load()
        {
            if (File.Exists(FileName))
            {
                Stream configFileStream = File.OpenRead(FileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                var cfg = (Config)deserializer.Deserialize(configFileStream);
                configFileStream.Close();
                return cfg;
            }

            return null;
        }
    }

}
