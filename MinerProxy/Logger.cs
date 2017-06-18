using System;

namespace MinerProxy
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

        public static void MinerProxyHeader()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;

            Console.WriteLine(asciiLogo + version);
            Console.WriteLine(credits + '\n');
        }

        public static void LogToConsole(string msg, string m_endpoint = "NONE")
        {
            Console.WriteLine("[{0}] {1}: {2}", m_endpoint, DateTime.Now.ToLongTimeString(), msg);


            if (Program.settings.log)
            {
               Program._logMessages.Add(new LogMessage(logFileName + ".txt", string.Format("[{0}] {1}: {2}", m_endpoint, DateTime.Now.ToLongTimeString(), msg)));
            }
        }
    }
}
