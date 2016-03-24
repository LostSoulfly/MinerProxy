using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpProxy
{
    internal sealed class Program
    {
        private static int localPort;
        private static string remoteHost;
        private static int remotePort;

        private static Socket listener;
        private static ManualResetEvent allDone;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => File.WriteAllText("Exceptions.txt", e.ExceptionObject.ToString());

            if (args.Length < 3)
            {
                Console.WriteLine("Usage : TcpProxy <local port> <remote host> <remote port>");
                return;
            }

            localPort = Convert.ToInt32(args[0]);
            remoteHost = args[1];
            remotePort = Convert.ToInt32(args[2]);

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, localPort));
            listener.Listen(100);

            allDone = new ManualResetEvent(false);

            Console.Title = string.Concat("TcpProxy : ", remoteHost, ':', remotePort);
            Console.WriteLine("Listening on port {0} for connections", localPort);

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
                new Redirector(socket, remoteHost, remotePort);
            }
            catch (SocketException se)
            {
                Console.WriteLine("Accept failed with {0}", se.ErrorCode); 
            }
        }
    }
}
