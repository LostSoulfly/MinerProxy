using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MinerProxy
{
    public class Settings
    {
        public int localPort { get; set; }
        public string remoteHost { get; set; }
        public int remotePort { get; set; }
        public bool log { get; set; }
        public bool debug { get; set; }
        public bool identifyDevFee { get; set; }
        public bool showEndpointInConsole { get; set; }
        public bool showRigStats { get; set; }
        public int rigStatsIntervalSeconds { get; set; }
        public string walletAddress { get; set; }
        public bool colorizeConsole { get; set; }
        internal string settingsFile { get; set; }
        public List<string> allowedAddresses = new List<string>();

        public static void LoadSettings(out Settings settings, string settingsJson = "settings.json")
        {

            settings = new Settings();

            if (File.Exists(settingsJson))
            {
                Logger.LogToConsole(string.Format("Loading settings from {0}", settingsJson));
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsJson));
                    if (settings.localPort == 0)
                    {
                        Logger.LogToConsole("Local port missing!", color: ConsoleColor.Red);
                        System.Environment.Exit(1);
                    }

                    if (settings.remoteHost.Length == 0)
                    {
                        Logger.LogToConsole("Remote host missing!", color: ConsoleColor.Red);
                        System.Environment.Exit(1);
                    }

                    if (settings.remotePort == 0)
                    {
                        Logger.LogToConsole("Remote port missing!", color: ConsoleColor.Red);
                        System.Environment.Exit(1);
                    }
                    if (settings.allowedAddresses.Count == 0)
                    {
                        Logger.LogToConsole("No allowed IP addresses!", color: ConsoleColor.Red);
                        System.Environment.Exit(1);
                    }
                    if (settings.walletAddress.Length == 0)
                    {
                        Logger.LogToConsole("Wallet address missing!", color: ConsoleColor.Red);
                        System.Environment.Exit(1);
                    }
                    settings.settingsFile = settingsJson;
                    return;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to load settings: " + ex.Message);
                    System.Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("No {0} found! Generating generic one", settingsJson);

                settings.allowedAddresses.Add("127.0.0.1");
                settings.allowedAddresses.Add("127.0.0.2");
                settings.debug = false;
                settings.log = false;
                settings.localPort = 9000;
                settings.remotePort = 4444;
                settings.remoteHost = "us1.ethermine.org";
                settings.walletAddress = "0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332.MinerProxy";
                settings.identifyDevFee = true;
                settings.showEndpointInConsole = true;
                settings.rigStatsIntervalSeconds = 60;
                settings.showRigStats = true;
                settings.colorizeConsole = true;

                writeSettings(settingsJson, settings);

                Console.WriteLine("Edit the new {0} file and don't forget to change the wallet address!", settingsJson);
                Console.Write("Press any key to exit..");
                Console.ReadKey();
                System.Environment.Exit(1);
            }
        }

        public static void writeSettings(string settingsJson, Settings settings)
        {
            try
            {
                File.WriteAllText(settingsJson, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Save settings error: {0}", ex.Message);
            }
        }

        public static void ProcessArgs(string[] args, out Settings settings)
        {

            settings = new Settings();

            if (args.Length < 6 && args.Length > 1) //check if they're using command args
            {
                Console.WriteLine("Usage : MinerProxy.exe <JsonFile>");
                Console.WriteLine("MinerProxy.exe Ethermine.json");
                System.Environment.Exit(1);
            }
            else if (args.Length == 1)
            {
                LoadSettings(out settings, args[0]);    //first supplied argument should be a json file
                return;
            }
            else if (args.Length >= 2) //if they are, and the args match the 6 we're looking for..
            {
                Console.WriteLine("Command arguments are no longer accepted; pass a JSON file instead.");
                Console.Write("Press any key to exit..");
                Console.ReadKey();
                System.Environment.Exit(1);
            }
            else //there were no args, so we can check for a settings.json file
            {
                LoadSettings(out settings);
                return;
            }
        }
    }
}
