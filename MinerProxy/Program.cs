using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;
//try to move the logging blocking collection into this class
//then just initialize it once and have every thread add to it, instead of making a new thread
//for each logger for each session

namespace MinerProxy
{
    internal sealed class Program
    {

        public static Settings settings = new Settings();

        public static BlockingCollection<LogMessage> _logMessages = new BlockingCollection<LogMessage>();

        private static Socket listener;
        private static ManualResetEvent allDone;

        static void Main(string[] args)
        {

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;

            Console.WriteLine(Logger.asciiLogo + version);
            Console.WriteLine(Logger.credits + '\n');

            if (args.Length < 6 && args.Length > 0) //check if they're using command args
            {
                Console.WriteLine("Usage : MinerProxy.exe <local port> <remote host> <remote port> <Allowed IP> <Your Wallet Address> <Identify DevFee> <Log to file> <debug>");
                Console.WriteLine("MinerProxy.exe 9000 us1.ethermine.org 4444 127.0.0.1 0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332 True False False");
                return;
            }
            else if (args.Length >= 6) //if they are, and the args match the 6 we're looking for..
            {
                try
                {
                    Logger.LogToConsole("Command arguments specified; not loading settings.json.");
                    settings.localPort = Convert.ToInt32(args[0]);
                    settings.remoteHost = args[1];
                    settings.remotePort = Convert.ToInt32(args[2]);
                    settings.allowedAddresses.Add(args[3]);
                    settings.walletAddress = args[4];
                    settings.identifyDevFee = Convert.ToBoolean(args[5]);
                    settings.log = Convert.ToBoolean(args[6]);
                    settings.debug = Convert.ToBoolean(args[7]);

                    //switch (args[5].ToString.ToLower) {

                    //    case: ""
                    //}
                }
                catch (Exception ex)
                {
                    Logger.LogToConsole("Check your command arguments: " + ex.Message);
                    Console.ReadKey();
                    return;
                }
            }
            else //there were no args, so we can check for a settings.json file
            {
                if (File.Exists("settings.json"))
                {
                    try
                    {
                        settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json"));
                        if (settings.localPort == 0)
                        {
                            Logger.LogToConsole("Local port missing!");
                            return;
                        }

                        if (settings.remoteHost.Length == 0) 
                        {
                            Logger.LogToConsole("Remote host missing!");
                            return;
                        }

                        if (settings.remotePort == 0)
                        {
                            Logger.LogToConsole("Remote port missing!");
                            return;
                        }
                        if (settings.walletAddress.Length == 0)
                        {
                            Logger.LogToConsole("Wallet address missing!");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to load settings: " + ex.Message);
                        return;
                    }
                } else
                {
                    Console.WriteLine("No settings.json found! Generating generic one..");

                    settings.allowedAddresses.Add("127.0.0.1");
                    settings.allowedAddresses.Add("127.0.0.2");
                    settings.debug = false;
                    settings.log = false;
                    settings.localPort = 9000;
                    settings.remotePort = 4444;
                    settings.remoteHost = "us1.ethermine.org";
                    settings.walletAddress = "0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332.MineProxy";
                    settings.identifyDevFee = true;

                    File.WriteAllText("settings.json",JsonConvert.SerializeObject(settings, Formatting.Indented));

                    Console.WriteLine("Edit the new settings.json file and don't forget to change the wallet address!");
                    Console.ReadKey();
                    return;
                }
            }
            

            AppDomain.CurrentDomain.UnhandledException += (s, e) => File.WriteAllText("Exceptions.txt", e.ExceptionObject.ToString());

            if (settings.log) { //if logging enabled, let's start the logging queue
                var task = new Task(() => ProcessLogQueue(), TaskCreationOptions.LongRunning);
                task.Start();
            }

            if (settings.debug)
                Logger.LogToConsole("Debug enabled");

            if (settings.identifyDevFee)
                Logger.LogToConsole("Showing DevFee mining as 'DevFee' rigName");

            Logger.LogToConsole("Replacing Wallets with: " + settings.walletAddress);

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, settings.localPort));
                listener.Listen(100);
            } catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                return;
            }
            allDone = new ManualResetEvent(false);

            Console.Title = string.Concat("MinerProxy : ", settings.remoteHost, ':', settings.remotePort);
            Logger.LogToConsole(string.Format("Listening for miners on port {0}, on IP {1}", settings.localPort, listener.LocalEndPoint));
            Logger.LogToConsole("Accepting connections from: " + string.Join(", ", settings.allowedAddresses));

            string key;

            // We can accept console input, but we need to set up the listener on its own thread
            // otherwise we can't listen for key events in the console
            var listenerTask = new Task(() => listenerStart(), TaskCreationOptions.LongRunning);
            listenerTask.Start();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true).Key.ToString();
                    
                    switch (key)
                    {
                        case "S":
                            //output current stats, like hashrate and shares and uptime
                            break;

                        case "C":
                            //output current connection status
                            //number of users, their addresses, and their rigNames
                            break;

                        case "Q":
                            Console.WriteLine("Shutting down..");
                            return;
                    }
                }
            }
        }

        private static void listenerStart()
        {
            while (true)
            {
                allDone.Reset();
                listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
                allDone.WaitOne();
            }
        }

        private static void AcceptCallback(IAsyncResult iar)
        {
            allDone.Set();
            
            try
            {
                var socket = listener.EndAccept(iar);

                string remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();

                if (!settings.allowedAddresses.Contains("0.0.0.0"))
                {
                    if (!settings.allowedAddresses.Contains(remoteAddress))
                    {
                        Logger.LogToConsole("Remote host " + remoteAddress + " not allowed; ignoring.");

                        return; //if the address supplied isn't allowed, just retrun and keep listening.
                    }
                }
                new Redirector(socket, settings.remoteHost, settings.remotePort, settings.walletAddress, settings.identifyDevFee, settings.debug, settings.log);
            }
            catch (SocketException se)
            {
                Logger.LogToConsole(string.Format("Accept failed with {0}", se.ErrorCode)); 
            }
        }

        private static void ProcessLogQueue()
        {
            if (settings.debug) Logger.LogToConsole("Logging queue started.");

            foreach (var msg in _logMessages.GetConsumingEnumerable())
            {

                File.AppendAllText(msg.Filepath, msg.Text + "\r\n");
            }

        }
    }
}
