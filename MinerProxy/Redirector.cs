using System;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Timers;
using MinerProxy.CoinHandlers;
using MinerProxy.JsonProtocols;

namespace MinerProxy
{
    internal sealed class Redirector : IDisposable
    {
        internal readonly string m_name;
        internal readonly string m_endpoint;
        internal Session m_client, m_server;
        internal string m_replacedWallet;
        internal string m_rigName = "";
        internal string m_workerName = "";
        internal string m_displayName = "";
        internal bool m_noRigName;
        internal bool m_alive;
        internal long m_submittedShares = 0;
        internal long m_acceptedShares = 0;
        internal long m_rejectedShares = 0;
        internal dynamic m_coinHandler;

        public long m_hashRate;

        public DateTime m_connectionStartTime;

        public Timer m_statusUpdateTimer;

        private void OnStatusUpdate(object source, ElapsedEventArgs e)
        {
            if (!Program.settings.showRigStats)
                return;

            DateTime timeNow = DateTime.Now;
            TimeSpan timeSpan = (timeNow - m_connectionStartTime);
            double hours = timeSpan.TotalHours;
            double minutes = timeSpan.TotalMinutes;
            double sharesPerMinute = (m_submittedShares / minutes);
            double sharesPerMinuteTruncated = Math.Truncate(sharesPerMinute * 100) / 100;
            double sharesPerHour = (m_submittedShares / hours);
            double sharesPerHourTruncated = Math.Truncate(sharesPerHour * 100) / 100;
            ConsoleColor color;

            lock (Logger.ConsoleBlockLock)
            {
                Logger.LogToConsole(string.Format(m_displayName + "'s status update: "), m_endpoint, ConsoleColor.Cyan);

                color = ConsoleColor.DarkCyan;
                Logger.LogToConsole(string.Format("Hashrate: {0}", m_hashRate.ToString("#,##0,Mh/s").Replace(",", ".")), m_endpoint, color);

                if (m_submittedShares != m_acceptedShares) //No reason to show if they match, save space with multiple rigs
                    Logger.LogToConsole(string.Format("Found shares: {0}", m_submittedShares), m_endpoint, color);

                Logger.LogToConsole(string.Format("Accepted shares: {0}", m_acceptedShares), m_endpoint, color);
                if (m_rejectedShares > 0) Logger.LogToConsole(string.Format("Rejected shares: {0}", m_rejectedShares), m_endpoint, color);
                Logger.LogToConsole(string.Format("Time connected: {0}", timeSpan.ToString("hh\\:mm")), m_endpoint, color);
                Logger.LogToConsole(string.Format("Shares per minute: {0}", string.Format("{0:N2}", sharesPerMinuteTruncated)), m_endpoint, color);
                Logger.LogToConsole(string.Format("Shares per hour: {0}", string.Format("{0:N2}", sharesPerHourTruncated)), m_endpoint, color);
            }
        }

        public Redirector(Socket client, string ip, int port)
        {
            SetupCoinHandler();

            m_name = client.RemoteEndPoint.ToString();
            m_connectionStartTime = DateTime.Now;

            m_statusUpdateTimer = new Timer();
            m_statusUpdateTimer.Elapsed += new ElapsedEventHandler(OnStatusUpdate);

            if ((Program.settings.rigStatsIntervalSeconds) > 0 && (Program.settings.rigStatsIntervalSeconds <= 3600))
            {
                m_statusUpdateTimer.Interval = Program.settings.rigStatsIntervalSeconds * 1000;
            }
            else
            {
                m_statusUpdateTimer.Interval = 60000;
            }
            m_statusUpdateTimer.Enabled = true;

            int index = m_name.IndexOf(":");
            m_endpoint = m_name.Substring(0, index);
            m_endpoint = m_endpoint + "_" + m_name.Substring(index + 1);

            Logger.LogToConsole(string.Format("Session started: ({0})", m_name),  m_endpoint, ConsoleColor.DarkGreen);

            m_alive = false;
            
            m_client = new Session(client);
            m_client.OnDataReceived = OnClientPacket;
            m_client.OnDisconnected = Dispose;
            
            var outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            outSocket.BeginConnect(ip, port, EndConnect, outSocket);
        }

        private void EndConnect(IAsyncResult iar)
        {
            try
            {
                var outSocket = iar.AsyncState as Socket;

                outSocket.EndConnect(iar);

                m_server = new Session(outSocket);
                m_server.OnDataReceived = OnServerPacket;
                m_server.OnDisconnected = Dispose;

                m_alive = true;

                m_server.Receive();
                m_client.Receive();
            }
            catch (SocketException se)
            {
                m_client.Dispose();
                m_statusUpdateTimer.Enabled = false;
                Logger.LogToConsole(string.Format("Connection bridge failed with {0} ({1})",se.ErrorCode,m_name),  m_endpoint, ConsoleColor.Red);
            }
        }

        private void SetupCoinHandler()
        {
            switch (Program.settings.minedCoin)
            {
                case "ETC":
                case "ETH":
                    m_coinHandler = new CoinHandlers.EthCoin(this); //initialize the coinhandler with the EthCoin class and reference this Redirector instance
                    break;

                case "SC":

                    break;

                case "ZEC":
                    break;

                case "PASC":
                    break;

                case "DCR":
                    break;

                case "LBRY":
                    break;

                case "UBIQ":
                case "UBQ":
                    break;

                case "CRYPTONOTE":
                case "CRY":
                    break;

            }
        }

        private void OnServerPacket(byte[] buffer,int length)
        {

            switch (Program.settings.minedCoin)
            {
                case "ETC":
                case "ETH":
                    m_coinHandler.OnEthServerPacket(buffer, length);
                    break;

                case "SC":

                    break;

                case "ZEC":
                    break;

                case "PASC":
                    break;

                case "DCR":
                    break;

                case "LBRY":
                    break;

                case "UBIQ":
                case "UBQ":
                    break;

                case "CRYPTONOTE":
                case "CRY":
                    break;

            }
        }
        private void OnClientPacket(byte[] buffer, int length)
        {
            switch (Program.settings.minedCoin)
            {
                case "ETC":
                case "ETH":
                    m_coinHandler.OnEthClientPacket(buffer, length);
                    break;

                case "SC":

                    break;

                case "ZEC":
                    break;

                case "PASC":
                    break;

                case "DCR":
                    break;

                case "LBRY":
                    break;

                case "UBIQ":
                case "UBQ":
                    break;

                case "CRYPTONOTE":
                case "CRY":
                    break;

            }
            

            if (Program.settings.log)
             Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " >---->\r\n" + Encoding.UTF8.GetString(buffer,0,length)));
        }

        public void Dispose()
        {
            if (m_alive)
            {
                m_alive = false;

                if (m_client != null)
                    m_client.Dispose();

                if (m_server != null)
                    m_server.Dispose();

                Logger.LogToConsole(string.Format("Closing session: ({0})", m_name),  m_endpoint);
                m_statusUpdateTimer.Enabled = false;
            }
        }
    }
}
