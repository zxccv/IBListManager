using System;
using System.Collections.Generic;

namespace InfoBaseListDataClasses
{
    [Serializable]
    public enum DataQueries
    { 
        Ping, 
        Pong,
        ComputerNameRequest,
        ComputerNameAnswer,
        UserListRequest,
        UserListAnswer,
        UserInfobaseListRequest,
        UserInfobaseListAnswer,
        UserInfobaseListPush
    }

    [Serializable]
    public class DataUnitQuery
    {
        public DataQueries Query;
        public string ID;
        public string PoolName;
    }

    [Serializable]
    public class DataUnitComputer:DataUnitQuery
    {
        public string ComputerName;
    } 
    
    [Serializable]
    public class DataUnitUserList : DataUnitComputer
    {
        public List<string> Users = new List<string>();
    }

    [Serializable]
    public class DataUnitUserInfoBaseList : DataUnitComputer
    {
        public string UserName;
        public List<InfoBase> InfoBaseList = new List<InfoBase>();
    }

    [Serializable]
    public class InfoBase :IComparable
    {
        public string InfobaseName {get;set;}
        public string Connect { get; set; }
        public string ID { get; set; }
        public string Folder { get; set; }
        public string OrderInList { get; set; }
        public string OrderInTree { get; set; }
        public string External { get; set; }
        public string ClientConnectionSpeed { get; set; }
        public string App { get; set; }
        public string Version { get; set; }
        public string WA { get; set; }
               
        public InfoBase()
        {

        }

        public InfoBase(InfoBase ib)
        {
            var typeInfoBase = typeof(InfoBase);

            var typeInfoBaseFields = typeInfoBase.GetProperties();

            foreach (var typeInfoBaseField in typeInfoBaseFields)
            {
                typeInfoBaseField.SetValue(this, typeInfoBaseField.GetValue(ib,null),null);                
            }
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            InfoBase ib = obj as InfoBase;

            if (ib == null)
                return false;


            var typeInfoBase = typeof(InfoBase);

            var typeInfoBaseFields = typeInfoBase.GetProperties();

            foreach (var typeInfoBaseField in typeInfoBaseFields)
            {
                if (typeInfoBaseField.GetValue(ib, null) == null && typeInfoBaseField.GetValue(this, null) == null)
                    continue;

                if (typeInfoBaseField.GetValue(ib, null) == null)
                    return false;

                if (typeInfoBaseField.GetValue(this, null) == null)
                    return false;

                if(!(typeInfoBaseField.GetValue(ib,null).Equals(typeInfoBaseField.GetValue(this,null))))
                    return false;
            }

            /*if (this.InfobaseName == ib.InfobaseName
                && this.Folder == ib.Folder
                && this.Connect == ib.Connect)
            {
                return true;
            }
            else
            {
                return false;
            }*/

            return true;

        }        

        public override int GetHashCode()
        {
            var str = InfobaseName + Folder + Connect;
            return str.GetHashCode();
        }

        public int CompareTo(object o)
        {
            return InfobaseName.CompareTo(o);
        }

       
    }
}
