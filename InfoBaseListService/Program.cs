using System.ServiceProcess;

namespace InfoBaseListService
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// 

        public static string[] Args; 

        static void Main(string[] args)
        {
            Args = args;
            var servicesToRun = new ServiceBase[] 
            { 
                new InfoBaseListService() 
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
