using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static string credits = " Programmed by LostSoulfly | Original code by RajanGrewal";

        public static void LogToConsole(string msg)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now.ToLongTimeString(), msg);
            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage("console.txt", DateTime.Now.ToLongTimeString() + ": " + msg));
        }
    }
}
