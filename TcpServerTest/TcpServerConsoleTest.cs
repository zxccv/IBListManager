using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using InfoBaseListDataClasses;
using InfoBaseListTcpServerNamespace;
using InfoBaseListManager;

namespace TcpServerTest
{
    class TcpServerConsoleTest
    {                
        static void Main(string[] args)
        {
            InfoBaseListManager.Config.ConfigurationData.Port = 22;

            var ib = new InfoBase();
            ib.InfobaseName = "sdgsdgsd";

            var ibc = new InfoBaseCollection();
            ibc.InfoBaseList.Add(ib);

            var pool = new Pool();
            pool.InfoBaseCollectionList.Add(ibc);

            Config.ConfigurationData.PoolList = new List<Pool>();
            Config.ConfigurationData.PoolList.Add(pool);


            Config.ConfigurationData.Save();

            
        }
    }
}
