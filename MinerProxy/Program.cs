using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

//try to move the logging blocking collection into this class
//then just initialize it once and have every thread add to it, instead of making a new thread
//for each logger for each session

namespace MinerProxy
{
    internal sealed class Program
    {
        public static Settings settings;
        
        public static BlockingCollection<LogMessage> _logMessages = new BlockingCollection<LogMessage>();

        private static Socket listener;
        private static ManualResetEvent allDone;

        static void Main(string[] args)
        {

            Logger.MinerProxyHeader();

            // Load and process settings in the Settings class
            Settings.ProcessArgs(args, out settings);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => File.WriteAllText("Exceptions.txt", e.ExceptionObject.ToString());

            Logger.logFileName = DateTime.Now.ToString("s").Replace(":", ".") + "_log";
            CancellationTokenSource logTokenSource = new CancellationTokenSource();
            CancellationToken logToken = logTokenSource.Token;
            var logQueue = new Task(() => ProcessLogQueue(logToken), TaskCreationOptions.LongRunning);
            if (settings.log) //if logging enabled, let's start the logging queue
                logQueue.Start();

            if (settings.debug)
                Logger.LogToConsole("Debug enabled");

            if (settings.identifyDevFee)
                Logger.LogToConsole("Showing DevFee mining as 'DevFee' rigName");

            Logger.LogToConsole("Coin protocol: " + settings.minedCoin);

            if (Program.settings.replaceWallet)
            {
                Logger.LogToConsole("Replacing Wallets with: " + settings.walletAddress);
                if (!string.IsNullOrWhiteSpace(settings.devFeeWalletAddress))
                    Logger.LogToConsole("Replacing DevFee wallets with " + settings.devFeeWalletAddress);
            }
            else
            {
                Logger.LogToConsole("Not replacing Wallets");
            }
            

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, settings.localPort));
                listener.Listen(100);
            } catch (Exception ex)
            {
                Logger.LogToConsole(string.Format("Error: {0}", ex.Message), color: ConsoleColor.Red);
                return;
            }

            allDone = new ManualResetEvent(false);

            Console.Title = string.Concat("MinerProxy : ", settings.remoteHost, ':', settings.remotePort);
            Logger.LogToConsole(string.Format("Listening for miners on port {0}, on IP {1}", settings.localPort, listener.LocalEndPoint));
            Logger.LogToConsole("Accepting connections from: " + string.Join(", ", settings.allowedAddresses));

            Logger.LogToConsole("Press 'H' for available commands", "HELP");

            var listenerTask = new Task(() => listenerStart(), TaskCreationOptions.LongRunning);
            listenerTask.Start();
            
            string key;

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true).Key.ToString();

                    switch (key)
                    {

                        case "S":
                            settings.showRigStats = !settings.showRigStats;
                            Logger.LogToConsole((settings.showRigStats) ? "RigStats enabled" : "RigStats disabled");
                            break;

                        case "U":
                            //update settings file with new options
                            Settings.writeSettings(settings.settingsFile, settings);
                            Logger.LogToConsole(string.Format("Updated {0} file to newest version", settings.settingsFile));
                            break;

                        case "C":
                            settings.colorizeConsole = (!settings.colorizeConsole);
                            Logger.LogToConsole((settings.colorizeConsole) ? "Colors enabled" : "Colors disabled");
                            break;

                        case "E":
                            settings.showEndpointInConsole = (!settings.showEndpointInConsole);
                            Logger.LogToConsole((settings.showEndpointInConsole) ? "Endpoint prefix enabled" : "Endpoint prefix disabled");
                            break;

                        case "L":
                            settings.log = !settings.log;
                            Logger.LogToConsole((settings.log) ? "Logging enabled" : "Logging disabled");

                            if (settings.log)
                            {
                                logTokenSource = new CancellationTokenSource();
                                logToken = logTokenSource.Token;
                                logQueue = new Task(() => ProcessLogQueue(logToken), TaskCreationOptions.LongRunning);
                                logQueue.Start();
                            }
                            else
                            {
                                logTokenSource.Cancel();
                            }
                            break;

                        case "D":
                            settings.debug = !settings.debug;
                            Logger.LogToConsole((settings.debug) ? "Debug enabled" : "Debug disabled");
                            break;

                        case "Q":
                            Logger.LogToConsole("Shutting down MinerProxy..");
                            System.Environment.Exit(0);
                            return;

                        case "H":
                            lock (Logger.ConsoleBlockLock)
                            {
                                Logger.LogToConsole("MinerProxy Available Commands", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("S key: Enable/Disable showing Rig Stats", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("C key: Enable/Disable console colors", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("E key: Enable/Disable Endpoint prefix on log messages", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("L key: Enable/Disable logging to file", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("D key: Enable/Disable debug messages", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("U key: Update the loaded JSON file with current settings", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("Q key: Quit MinerProxy", "HELP", ConsoleColor.Yellow);
                            }
                                break;

                    }
                }
                Thread.Sleep(1);
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
                        Logger.LogToConsole("Remote host " + remoteAddress + " not allowed; ignoring", color: ConsoleColor.Red);

                        return; //if the address supplied isn't allowed, just return and keep listening.
                    }
                }
                new Redirector(socket, settings.remoteHost, settings.remotePort);
            }
            catch (SocketException se)
            {
                Logger.LogToConsole(string.Format("Accept failed with {0}", se.ErrorCode), color: ConsoleColor.Red);
            }
        }

        private static void ProcessLogQueue(CancellationToken token)
        {
            if (settings.debug) Logger.LogToConsole("Logging queue started");

            foreach (var msg in _logMessages.GetConsumingEnumerable())
            {
                if (!token.IsCancellationRequested)
                {
                    File.AppendAllText(msg.Filepath, msg.Text + "\r\n");
                }
                else
                {
                    if (settings.debug) Logger.LogToConsole("Logging queue stopped");
                    return;
                }
            }

        }
    }
}
