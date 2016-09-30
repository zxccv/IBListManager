using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using System.ServiceProcess;
using System.Collections.Generic;

namespace InfoBaseListServiceInstaller
{
    class Program
    {
        static void Install(string filename,string host,string port,string compname)
        {

        }

        static string InteractiveInstall(string result = "")
        {
            Console.Clear();

            if(result != "")
            {
                Console.WriteLine(result);
                Console.WriteLine();
            }

            ServiceController[] services = ServiceController.GetServices();
            ServiceController service = null;
            List<string> commands = new List<string>();
            
            
            foreach(var sc in services)
            {
                if (sc.ServiceName == "InfoBaseListService")
                {
                    service = sc;
                    break;
                }
            }

            if (service != null)
            {
                Console.WriteLine("Служба InfobaseListService установлена на данном компьютере");

                Console.WriteLine("В данный момент служба {0}", service.Status == ServiceControllerStatus.Running ? "запущена" : "остановлена");
                
                commands.Add(Convert.ToString(commands.Count + 1) + ") Удалить службу");
                if (service.Status == ServiceControllerStatus.Running)
                {
                    commands.Add(Convert.ToString(commands.Count + 1) + ") Перезапустить службу");
                    commands.Add(Convert.ToString(commands.Count + 1) + ") Остановить службу");
                }
                else
                {
                    commands.Add(Convert.ToString(commands.Count + 1) + ") Запустить службу");
                }
            } else
            {
                Console.WriteLine("Служба InfobaseListService НЕ установлена на данном компьютере");
                commands.Add(Convert.ToString(commands.Count + 1) + ") Установить службу");
            }

            commands.Add(Convert.ToString(commands.Count + 1) + ") Выход");

            Console.WriteLine();
            Console.WriteLine("Доступные команды:");
            foreach(var command in commands)
            {
                Console.WriteLine(command);
            }
            var key = Console.ReadKey(true);

            string selectedCommand = "";
            foreach(var command in commands)
            {
                if(command[0] == key.KeyChar)
                {
                    selectedCommand = command.Substring(3);
                }
            }

            if(selectedCommand == "")
            {
                return "Неправильный номер команды.";
            }

            if(selectedCommand == "Выход")
            {
                return "exit";
            }

            if (selectedCommand == "Запустить службу")
            {
                Console.WriteLine("Запускаем службу.....");
                
                try
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 0, 5000));
                    return "Служба успешно запущена";
                 
                } catch(Exception)
                {
                    return "Ошибка при запуске службы";
                }                         
            }

            if (selectedCommand == "Перезапустить службу")
            {
                Console.WriteLine("Перезапускаем службу.....");

                try
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 0, 0, 5000));
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 0, 0, 5000));
                    return "Служба успешно перезапущена";

                }
                catch (Exception)
                {
                    return "Ошибка при перезапуске службы";
                }
            }

            if (selectedCommand == "Остановить службу")
            {
                Console.WriteLine("Останавливаем службу.....");

                try
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 0, 0, 5000));
                    return "Служба успешно остановлена";

                }
                catch (Exception)
                {
                    return "Ошибка при остановке службы";
                }
            }

            if (selectedCommand == "Удалить службу")
            {
                string[] arg = new string[2];
                arg[0] = "/u";
                arg[1] = "InfoBaseListService.exe";

                if(!File.Exists("InfoBaseListService.exe"))
                {
                    return "Не обнаружен запускаемый файл службы InfoBaseListService.exe";
                }

                Console.WriteLine("Начато удаление службы. Ожидайте.");

                Main(arg);
            }

            if (selectedCommand == "Установить службу")
            {
                string[] arg = new string[5];
                arg[0] = "/i";
                arg[1] = "InfoBaseListService.exe";

                if (!File.Exists("InfoBaseListService.exe"))
                {
                    return "Не обнаружен запускаемый файл службы InfoBaseListService.exe";
                }

                Console.Write("Введите адрес сервера: ");
                arg[2] = Console.ReadLine();
                Console.Write("Введите порт сервера: ");
                arg[3] = Console.ReadLine();
                Console.Write("Введите название организации: ");
                arg[4] = Console.ReadLine();

                Console.WriteLine("Начата установка службы. Ожидайте.");

                Main(arg);
            }

            return "";
           
        }

        static void Main(string[] args)
        {
            if (args.Length == 5 && args[0] == "/i" || (args.Length == 2 && args[0] == "/u"))
            {
                if (args.Length == 5)
                {
                    var filename = args[1];
                    var host = args[2];
                    var port = args[3];
                    var poolName = args[4];


                    //Установка службы
                    var comline = "%SystemRoot%\\Microsoft.NET\\Framework\\v2.0.50727\\InstallUtil.exe";

                    comline = Environment.ExpandEnvironmentVariables(comline);
                    
                    var psi = new ProcessStartInfo
                    {
                        FileName = comline,
                        Arguments = "\"" + filename + "\"",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false
                    };

                    var p = new Process {StartInfo = psi};

                    p.Start();

                    Console.Write(p.StandardOutput.ReadToEnd());
                    


                    //Командная строка службы
                    var rk = Registry.LocalMachine;

                    var rkiblist = rk.OpenSubKey("SYSTEM\\CurrentControlSet\\services\\InfoBaseListService", true);

                    if(rkiblist == null)
                    { 
                        Console.WriteLine("Какая-то ошибка...");
                        Console.ReadKey();
                        return;
                    }
                    var imagePath = rkiblist.GetValue("ImagePath");

                    imagePath = Environment.CurrentDirectory + "\\InfoBaseListService.exe " + host + " " + port + " \"" + poolName + "\"";
                    
                    
                    rkiblist.SetValue("ImagePath",imagePath);


                    //Запуск службы
                    p.StartInfo.FileName = "net";
                    p.StartInfo.Arguments = "start InfoBaseListService";

                    p.Start();

                    Console.Write(p.StandardOutput.ReadToEnd());

                }
                else
                {
                    var filename = args[1];
                    
                    var comline = "%SystemRoot%\\Microsoft.NET\\Framework\\v2.0.50727\\InstallUtil.exe";

                    comline = Environment.ExpandEnvironmentVariables(comline);

                    var psi = new ProcessStartInfo
                    {
                        FileName = comline,
                        Arguments = "/u \"" + filename + "\"",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false
                    };

                    var p = new Process { StartInfo = psi };

                    p.Start();

                    Console.Write(p.StandardOutput.ReadToEnd());
                }

                Console.WriteLine("Для продолжения нажмите любую кнопку...");
                Console.ReadKey();

            }
            else
            {
                //Console.WriteLine("Неправильные аргументы.");
                string res = InteractiveInstall();
                while(res!="exit")
                {
                    res = InteractiveInstall(res);
                }
                
            }
            
            

            

        }
    }
}
