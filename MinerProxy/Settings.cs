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
        public string minedCoin { get; set; }
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
                        IncorrectSettingsMessage("Local port missing!", settings, settingsJson);
                    }

                    if (string.IsNullOrEmpty(settings.remoteHost))
                    {
                        IncorrectSettingsMessage("Remote host missing!", settings, settingsJson);
                    }

                    if (settings.remotePort == 0)
                    {
                        IncorrectSettingsMessage("Remote port missing!", settings, settingsJson);
                    }
                    if (settings.allowedAddresses.Count == 0)
                    {
                        IncorrectSettingsMessage("No allowed addresses!", settings, settingsJson);
                    }
                    if (string.IsNullOrEmpty(settings.walletAddress))
                    {
                        IncorrectSettingsMessage("Wallet address missing!", settings, settingsJson);
                    }
                    if ((string.IsNullOrEmpty(settings.minedCoin)) | !ValidateCoin(settings.minedCoin))
                    {
                        IncorrectSettingsMessage(string.Format("Unknown coin specified {0}", settings.minedCoin), settings, settingsJson);
                    }
                    else
                    {
                        settings.minedCoin = settings.minedCoin.ToUpper();
                    }
                    settings.settingsFile = settingsJson;
                    return;

                }
                catch (Exception ex)
                {
                    IncorrectSettingsMessage(string.Format("Unable to load {0}", ex.Message), settings);
                    System.Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("No {0} found! Generating generic one", settingsJson);

                settings = GetGenericSettings(settings);

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

        public static void IncorrectSettingsMessage(string error, Settings settings, string settingsJson = "Settings.json")
        {
            settings.settingsFile = settingsJson;

            ConsoleColor color = ConsoleColor.Red;
            Logger.LogToConsole("Unable to load settings: " + error, "ERROR", color);
            Logger.LogToConsole("Would you like to update the current JSON to the newest settings? Y/N", "ERROR", color);
            string key = Console.ReadKey().Key.ToString();
            if (key == "Y")
            {
                writeSettings(settings.settingsFile, GetGenericSettings(settings));
            }
            System.Environment.Exit(1);
        }

        public static Settings GetGenericSettings(Settings settings)
        {
            if (settings.allowedAddresses.Count == 0)
            {
                settings.allowedAddresses.Add("127.0.0.1");
                settings.allowedAddresses.Add("0.0.0.0");
            }
            if (settings.debug != true) settings.debug = false;
            if (settings.log != true) settings.log = false;
            if (settings.localPort == 0) settings.localPort = 9000;
            if (settings.remotePort == 0) settings.remotePort = 4444;
            if (string.IsNullOrEmpty(settings.remoteHost)) settings.remoteHost = "us1.ethermine.org";
            if (string.IsNullOrEmpty(settings.walletAddress)) settings.walletAddress = "0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332.MinerProxy";
            settings.identifyDevFee = true;
            if (settings.showEndpointInConsole != false) settings.showEndpointInConsole = true;
            if (settings.rigStatsIntervalSeconds == 0) settings.rigStatsIntervalSeconds = 60;
            settings.showRigStats = true;
            settings.colorizeConsole = true;
            if (string.IsNullOrEmpty(settings.minedCoin)) settings.minedCoin = "ETH";

            return settings;
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

        public static bool ValidateCoin(string coin) //Pass the string of a coin, convert to uppercase, and return true if it's valid, else false
        {
            coin = coin.ToUpper();
            switch (coin)
            {
                case "ETC":
                case "ETH":
                    return true;

                case "SIA":
                case "SC":
                    return true;

                case "ZEC":
                    return true;

                case "PASC":
                    return true;

                case "DCR":
                    return true;

                case "LBRY":
                    return true;

                case "UBIQ":
                case "UBQ":
                    return true;

                case "CRYPTONOTE":
                case "CRY":
                    return true;

                default:
                    return false;
            }

        }
    }
}
