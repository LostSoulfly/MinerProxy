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
        private static int localPort;
        private static string remoteHost;
        private static int remotePort;
        private static bool log;
        private static bool debug;
        private static bool replaceRigName;
        private static string walletAddress;
        private static string allowedAddress;
        public static BlockingCollection<LogMessage> _logMessages = new BlockingCollection<LogMessage>();

        private static Socket listener;
        private static ManualResetEvent allDone;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => File.WriteAllText("Exceptions.txt", e.ExceptionObject.ToString());

            if (args.Length < 6)
            {
                Console.WriteLine("Usage : MinerProxy.exe <local port> <remote host> <remote port> <Allowed IP> <Your Wallet Address> <Identify DevFee> <Log to file> <debug>");
                Console.WriteLine("MinerProxy.exe 9000 us1.ethermine.org 4444 127.0.0.1 0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332 True False False");
                return;
            }

            try
            {
                localPort = Convert.ToInt32(args[0]);
                remoteHost = args[1];
                remotePort = Convert.ToInt32(args[2]);
                allowedAddress = args[3];
                walletAddress = args[4];
                replaceRigName = Convert.ToBoolean(args[5]);
                log = Convert.ToBoolean(args[6]);
                debug = Convert.ToBoolean(args[7]);
            }
            catch (Exception ex)
            {
                Logger.LogToConsole("Check your command arguments: " + ex.Message);
                Console.ReadKey();
                return;
            }

            if (log) { //if logging enabled, let's start the logging queue
                var task = new Task(() => ProcessLogQueue(), TaskCreationOptions.LongRunning);
                task.Start();
            }

            if (debug)
                Logger.LogToConsole("Debug enabled");

            if (replaceRigName)
                Logger.LogToConsole("Showing DevFee mining as 'DevFee' rigName");

            Logger.LogToConsole("Replacing Wallets with: " + walletAddress);

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, localPort));
            listener.Listen(100);

            allDone = new ManualResetEvent(false);

            Console.Title = string.Concat("MinerProxy : ", remoteHost, ':', remotePort);
            Logger.LogToConsole(string.Format("Listening for miners on port {0}, on IP {1}", localPort, allowedAddress));
            
            while (true)
            {
                allDone.Reset();
                listener.BeginAccept(new AsyncCallback(AcceptCallback),null);
                allDone.WaitOne();
            }
        }

        private static void AcceptCallback(IAsyncResult iar)
        {
            allDone.Set();
            
            try
            {
                var socket = listener.EndAccept(iar);

                IPAddress remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address;

                if (allowedAddress != "0.0.0.0")
                {
                    if (!remoteAddress.Equals(IPAddress.Parse(allowedAddress)))
                    {
                        Logger.LogToConsole("Remote host " + remoteAddress + " not allowed; ignoring.");

                        return; //if the address supplied isn't allowed, just retrun and keep listening.
                    }
                }
                new Redirector(socket, remoteHost, remotePort, walletAddress, replaceRigName, debug, log);
            }
            catch (SocketException se)
            {
                Logger.LogToConsole(string.Format("Accept failed with {0}", se.ErrorCode)); 
            }
        }

        private static void ProcessLogQueue()
        {
            Logger.LogToConsole("Logging queue started..");

            foreach (var msg in _logMessages.GetConsumingEnumerable())
            {

                File.AppendAllText(msg.Filepath, msg.Text + "\r\n");
            }

        }
    }
}
