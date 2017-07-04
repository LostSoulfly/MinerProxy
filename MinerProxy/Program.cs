using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MinerProxy.Web;
using MinerProxy.Logging;
using WebSocketSharp.Server;
using System.Collections.Generic;
using MinerProxy.Network;
using MinerProxy.Miners;
using static MinerProxy.Donations;

namespace MinerProxy
{
    internal sealed class Program
    {
        public static Settings settings;
        
        public static BlockingCollection<LogMessage> _logMessages = new BlockingCollection<LogMessage>();
        public static SlidingBuffer<ConsoleList> _webConsoleQueue = new SlidingBuffer<ConsoleList>(60);
        public static BlockingCollection<ConsoleList> _consoleQueue = new BlockingCollection<ConsoleList>();

        public static List<MinerStatsFull> _minerStats = new List<MinerStatsFull>();
        public static int currentClients;

        public static HttpServer webSock;
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

            var consoleQueue = new Task(() => ProcessConsoleQueue(), TaskCreationOptions.LongRunning);
            consoleQueue.Start();

            if (settings.debug)
                Logger.LogToConsole("Debug enabled", "MinerProxy");

            if (settings.identifyDevFee)
                Logger.LogToConsole("Identifying DevFee connections as 'DevFee'", "MinerProxy");

            Logger.LogToConsole("Coin protocol: " + settings.minedCoin, "MinerProxy");

            if (settings.donateDevFee)
            {
                Logger.LogToConsole(string.Format("You are donating {0}% of DevFees to LostSoulfly and Samut3. Thanks!", Program.settings.percentToDonate), "MinerProxy");
                SetUpDonateLists();
            } else
            {
                Logger.LogToConsole("You are not donating a DevFee percentage to MinerProxy maintainers.", "MinerProxy");
            }

            if (Program.settings.replaceWallet)
            {
                Logger.LogToConsole("Replacing Wallets with: " + settings.walletAddress, "MinerProxy", ConsoleColor.Yellow);
                if (!string.IsNullOrWhiteSpace(settings.devFeeWalletAddress))
                    Logger.LogToConsole("Replacing DevFee wallets with " + settings.devFeeWalletAddress, "MinerProxy", ConsoleColor.Yellow);
            }
            else
            {
                Logger.LogToConsole("Not replacing Wallets", "MinerProxy");
            }
            
            //initialize webSock
            if (Program.settings.useWebSockServer)
            {
                try
                {
                    webSock = new HttpServer(settings.webSocketPort);
                    webSock.RootPath = Directory.GetCurrentDirectory() + @"\web\";
                    Directory.CreateDirectory(webSock.RootPath);
                    if (settings.debug) Logger.LogToConsole(string.Format("Web root: {0}", webSock.RootPath), "MinerProxy");

                    webSock.OnGet += new EventHandler<HttpRequestEventArgs>(Web.WebIndex.OnGet);
                    webSock.AddWebSocketService<WebIndex>("/");
                    webSock.AddWebSocketService<WebConsole>("/console");
                    webSock.Start();
                    Logger.LogToConsole(string.Format("WebSockServer listening on port {0}", settings.webSocketPort), "MinerProxy");
                } catch (Exception ex)
                {
                    Logger.LogToConsole(string.Format("Unable to start WebSocketServer on port {0}. Error: {1}", settings.webSocketPort, ex.Message));
                }
            }

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, settings.proxyListenPort));
                listener.Listen(100);
            } catch (Exception ex)
            {
                Logger.LogToConsole(string.Format("Error: {0}", ex.Message), color: ConsoleColor.Red);
                return;
            }

            allDone = new ManualResetEvent(false);

            UpdateConsoleTitle();
            Logger.LogToConsole(string.Format("Listening for miners on port {0}, on IP {1}", settings.proxyListenPort, listener.LocalEndPoint), "MinerProxy");
            Logger.LogToConsole("Accepting connections from: " + string.Join(", ", settings.allowedAddresses), "MinerProxy");

            Logger.LogToConsole("Press 'H' for available commands", "MinerProxy");

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
                            Logger.LogToConsole((settings.showRigStats) ? "RigStats enabled" : "RigStats disabled", "MinerProxy");
                            break;

                        case "U":
                            //update settings file with new options
                            Settings.writeSettings(settings.settingsFile, settings);
                            Logger.LogToConsole(string.Format("Updated {0} file to newest version", settings.settingsFile), "MinerProxy");
                            break;

                        case "C":
                            settings.colorizeConsole = (!settings.colorizeConsole);
                            Logger.LogToConsole((settings.colorizeConsole) ? "Colors enabled" : "Colors disabled", "MinerProxy");
                            break;

                        case "E":
                            settings.showEndpointInConsole = (!settings.showEndpointInConsole);
                            Logger.LogToConsole((settings.showEndpointInConsole) ? "Endpoint prefix enabled" : "Endpoint prefix disabled", "MinerProxy");
                            break;

                        case "L":
                            settings.log = !settings.log;
                            Logger.LogToConsole((settings.log) ? "Logging enabled" : "Logging disabled", "MinerProxy");

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
                            Logger.LogToConsole((settings.debug) ? "Debug enabled" : "Debug disabled", "MinerProxy");
                            break;

                        case "Q":
                            Logger.LogToConsole("Shutting down MinerProxy..", "MinerProxy");
                            System.Environment.Exit(0);
                            return;

                        case "M":
                            for (int i = 0; i < _minerStats.Count; i++)
                            {
                                Console.WriteLine("Miner displayName: {0}", _minerStats[i].displayName);
                                Console.WriteLine("Miner workerName: {0}", _minerStats[i].workerName);
                                Console.WriteLine("Miner rigName: {0}", _minerStats[i].rigName);
                                Console.WriteLine("Miner numberOfConnects: {0}", _minerStats[i].numberOfConnects);
                                Console.WriteLine("Miner connectionAlive: {0}", _minerStats[i].connectionAlive);
                                Console.WriteLine("Miner endPoint: {0}", _minerStats[i].endPoint);
                                Console.WriteLine("Miner connectionName: {0}", _minerStats[i].connectionName);
                                Console.WriteLine("Miner firstConnectTime: {0}", _minerStats[i].firstConnectTime.ToString());
                                Console.WriteLine("Miner connectionStartTime: {0}", _minerStats[i].connectionStartTime.ToString());
                                Console.WriteLine("Miner totalTimeConnected: {0}", _minerStats[i].totalTimeConnected.ToString());
                                Console.WriteLine("Miner submittedShares: {0}", _minerStats[i].submittedShares);
                                Console.WriteLine("Miner acceptedShares: {0}", _minerStats[i].acceptedShares);
                                Console.WriteLine("Miner rejectedShares: {0}", _minerStats[i].rejectedShares);
                                Console.WriteLine("Miner hashrate: {0}", _minerStats[i].hashrate);
                                Console.WriteLine("Miner GetAverageHashrate: {0}", _minerStats[i].GetAverageHashrate());
                                Console.WriteLine("Miner Wallets:");
                                Console.WriteLine(string.Join("\n", MinerManager.GetMinerWallets(_minerStats[i].displayName).ToArray()));
                            }
                            break;

                        case "X":
                                File.WriteAllText("MinerStats.json", Newtonsoft.Json.JsonConvert.SerializeObject(_minerStats, Newtonsoft.Json.Formatting.Indented));
                            Logger.LogToConsole("Exported MinerStats to MinerStats.json", "MinerProxy");
                            break;

                        case "O":
                            DonateList d = new DonateList();
                            double success = 0;
                            for (int i = 0; i < 1000; i++)  //be careful doing this with debug and more than 10k. Could take a while.
                            {
                                if (CheckForDonation(out d, "ETH"))
                                {
                                    if (settings.debug) Logger.LogToConsole(d.donatePoolAddress + " " + d.donatePoolPort + " " + d.donateWallet);
                                    success++;
                                }
                            }
                            Logger.LogToConsole("Success percentage: " + ((success / 1000) * 100) + "% out of 1,000", "MinerProxy");
                            Logger.LogToConsole("Win: " + success + " - Lose: " + (1000 - success) + ". Donate percent: " + settings.percentToDonate, "MinerProxy");
                            break;

                        case "H":
                            lock (Logger.ConsoleBlockLock)
                            {
                                Logger.LogToConsole("MinerProxy Available Commands", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("S key: Enable/Disable showing Rig Stats", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("C key: Enable/Disable console colors", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("E key: Enable/Disable Endpoint prefix on log messages", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("L key: Enable/Disable logging to file", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("D key: Enable/Disable debug messages", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("M key: Print all miner stats to console", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("X key: Export all miner stats to MinerStats.json", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("O key: Run a test of DevFee donation percentages. Turn debug off for faster tests.");
                                Logger.LogToConsole("U key: Update the loaded JSON file with current settings", "HELP", ConsoleColor.Yellow);
                                Logger.LogToConsole("Q key: Quit MinerProxy", "HELP", ConsoleColor.Yellow);
                            }
                                break;
                    }
                }
                Thread.Sleep(1);
            }
        }

        private static void WebSock_OnGet(object sender, HttpRequestEventArgs e)
        {
            throw new NotImplementedException();
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
                        Logger.LogToConsole("Remote host " + remoteAddress + " not allowed; ignoring", "MinerProxy", ConsoleColor.Red);

                        return; //if the address supplied isn't allowed, just return and keep listening.
                    }
                }
                
                new Redirector(socket, settings.remotePoolAddress, settings.remotePoolPort);
            
            }
            catch (SocketException se)
            {
                Logger.LogToConsole(string.Format("Accept failed with {0}", se.ErrorCode), color: ConsoleColor.Red);
            }
        }

        private static void UpdateConsoleTitle()
        {
            Console.Title = string.Format("MinerProxy: {0}:{1} Clients: {2}", settings.remotePoolAddress, settings.remotePoolPort, currentClients);
        }

        public static void DecrementClientCount()
        {
            currentClients--;
            UpdateConsoleTitle();
        }

        public static void IncrementClientCount()
        {
            currentClients++;
            UpdateConsoleTitle();
        }

        
        private static void ProcessConsoleQueue()
        {
            if (settings.debug) Logger.LogToConsole("Console queue started", "MinerProxy");
            settings.consoleQueueStarted = true;
            foreach (var msg in _consoleQueue.GetConsumingEnumerable())
            {
                lock (Logger.ConsoleBlockLock)
                {
                    Logger.LogToConsole(msg.message, msg.endPoint, msg.color, true);
                }
            }
        }

        private static void ProcessLogQueue(CancellationToken token)
        {
            if (settings.debug) Logger.LogToConsole("Logging queue started", "MinerProxy");

            foreach (var msg in _logMessages.GetConsumingEnumerable())
            {
                if (!token.IsCancellationRequested)
                {
                    File.AppendAllText(msg.Filepath, msg.Text + "\r\n");
                }
                else
                {
                    if (settings.debug) Logger.LogToConsole("Logging queue stopped", "MinerProxy");
                    return;
                }
            }

        }
    }
}
