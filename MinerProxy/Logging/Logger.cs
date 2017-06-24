using System;
using MinerProxy.Web;
namespace MinerProxy.Logging
{
    class Logger
    {

        public static string asciiLogo = @"    __  __ _                 _____                     
   |  \/  (_)               |  __ \                    
   | \  / |_ _ __   ___ _ __| |__) | __ _____  ___   _ 
   | |\/| | | '_ \ / _ \ '__|  ___/ '__/ _ \ \/ / | | |
   | |  | | | | | |  __/ |  | |   | | | (_) >  <| |_| |
   |_|  |_|_|_| |_|\___|_|  |_|   |_|  \___/_/\_\\__, |
                                                  __/ |
                                                 |___/ ";

        public static string credits = "Programmed by LostSoulfly | Modified by Samut";

        public static string logFileName;
        public static readonly object ConsoleColorLock = new object();
        public static readonly object ConsoleBlockLock = new object();
        
        public static void MinerProxyHeader()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;

            Console.WriteLine(asciiLogo + version);
            Console.WriteLine(credits + '\n');

            Program._consoleQueue.Add(new ConsoleList("MinerProxy v" + version, ConsoleColor.White));
            Program._consoleQueue.Add(new ConsoleList(credits, ConsoleColor.White));
        }

        public static void LogToConsole(string msg, string m_endpoint = "NONE", ConsoleColor color = ConsoleColor.White)
        {
            string message;

            
                if (Program.settings.showEndpointInConsole)
                {
                    message = string.Format("[{0}] {1}: {2}", m_endpoint, DateTime.Now.ToLongTimeString(), msg);
                }
                else
                {
                    message = string.Format("{0}: {1}", DateTime.Now.ToLongTimeString(), msg);
                }

            lock (ConsoleColorLock)
            {
                if (Program.settings.colorizeConsole) Console.ForegroundColor = color;
                Console.WriteLine(message);
                if (Program.settings.colorizeConsole) Console.ResetColor();
            }

            if (Program.settings.log)
            {
               Program._logMessages.Add(new LogMessage(logFileName + ".txt", string.Format("[{0}] {1}: {2}", m_endpoint, DateTime.Now.ToLongTimeString(), msg)));
            }

            Program._consoleQueue.Add(new ConsoleList(message, color));
            
        }
    }
}
