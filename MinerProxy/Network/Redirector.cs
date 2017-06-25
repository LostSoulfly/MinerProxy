using System;
using System.Text;
using System.Net.Sockets;
using System.Timers;
using MinerProxy.CoinHandlers;
using MinerProxy.JsonProtocols;
using MinerProxy.Logging;
using MinerProxy.Miners;

namespace MinerProxy.Network
{
    internal sealed class Redirector : IDisposable
    {
        internal Session m_client, m_server;
        internal dynamic m_coinHandler;

        internal MinerStats thisMiner;
        
        public Timer statusUpdateTimer;

        private void OnStatusUpdate(object source, ElapsedEventArgs e)
        {
            DateTime timeNow = DateTime.Now;
            TimeSpan timeSpan = (timeNow - thisMiner.connectionStartTime);
            TimeSpan calculatedSpan = (timeNow - thisMiner.lastCalculatedTime);     //determine how long it's been since the last cycle
            thisMiner.lastCalculatedTime = DateTime.Now;                            //set the lastCalc time to now, so we can do it again next time
            MinerManager.AddConnectedTime(thisMiner.displayName, calculatedSpan);

            if (!Program.settings.showRigStats)
                return;

            double hours = timeSpan.TotalHours;
            double minutes = timeSpan.TotalMinutes;
            double sharesPerMinute = (thisMiner.submittedShares / minutes);
            double sharesPerMinuteTruncated = Math.Truncate(sharesPerMinute * 100) / 100;
            double sharesPerHour = (thisMiner.submittedShares / hours);
            double sharesPerHourTruncated = Math.Truncate(sharesPerHour * 100) / 100;
            ConsoleColor color;

            lock (Logger.ConsoleBlockLock)
            {
                Logger.LogToConsole(string.Format(thisMiner.displayName + "'s status update: "), thisMiner.endPoint, ConsoleColor.Cyan);

                color = ConsoleColor.DarkCyan;
                if (Program.settings.minedCoin != "NICEHASH") // Nicehash doesn't report hashrate
                    Logger.LogToConsole(string.Format("Hashrate: {0}", thisMiner.hashrate.ToString("#,##0,Mh/s").Replace(",", ".")), thisMiner.endPoint, color);

                if (thisMiner.submittedShares != thisMiner.acceptedShares) //No reason to show if they match, save space with multiple rigs
                    Logger.LogToConsole(string.Format("Found shares: {0}", thisMiner.submittedShares), thisMiner.endPoint, color);

                Logger.LogToConsole(string.Format("Accepted shares: {0}", thisMiner.acceptedShares), thisMiner.endPoint, color);
                if (thisMiner.rejectedShares > 0) Logger.LogToConsole(string.Format("Rejected shares: {0}", thisMiner.rejectedShares), thisMiner.endPoint, color);
                Logger.LogToConsole(string.Format("Time connected: {0}", timeSpan.ToString("hh\\:mm")), thisMiner.endPoint, color);
                Logger.LogToConsole(string.Format("Shares per minute: {0}", string.Format("{0:N2}", sharesPerMinuteTruncated)), thisMiner.endPoint, color);
                Logger.LogToConsole(string.Format("Shares per hour: {0}", string.Format("{0:N2}", sharesPerHourTruncated)), thisMiner.endPoint, color);
            }
        }

        public Redirector(Socket client, string ip, int port)
        {
            SetupCoinHandler();
            thisMiner = new MinerStats();

            thisMiner.connectionName = client.RemoteEndPoint.ToString();
            thisMiner.connectionStartTime = DateTime.Now;

            statusUpdateTimer = new Timer();
            statusUpdateTimer.Elapsed += new ElapsedEventHandler(OnStatusUpdate);

            if ((Program.settings.rigStatsIntervalSeconds) > 0 && (Program.settings.rigStatsIntervalSeconds <= 3600))
            {
                statusUpdateTimer.Interval = Program.settings.rigStatsIntervalSeconds * 1000;
            }
            else
            {
                statusUpdateTimer.Interval = 60000;
            }
            statusUpdateTimer.Enabled = true;

            int index = thisMiner.connectionName.IndexOf(":");
            thisMiner.endPoint = thisMiner.connectionName.Substring(0, index);
            thisMiner.endPoint = thisMiner.endPoint + "_" + thisMiner.connectionName.Substring(index + 1);

            Logger.LogToConsole(string.Format("Session started: ({0})", thisMiner.connectionName),  thisMiner.endPoint, ConsoleColor.DarkGreen);

            thisMiner.connectionAlive = false;
            MinerManager.SetConnectionAlive(thisMiner.displayName, thisMiner.connectionAlive);
            
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

                thisMiner.connectionAlive = true;
                MinerManager.SetConnectionAlive(thisMiner.displayName, thisMiner.connectionAlive);

                m_server.Receive();
                m_client.Receive();
            }
            catch (SocketException se)
            {
                m_client.Dispose();
                statusUpdateTimer.Enabled = false;
                Logger.LogToConsole(string.Format("Connection bridge failed with {0} ({1})",se.ErrorCode,thisMiner.connectionName),  thisMiner.endPoint, ConsoleColor.Red);
            }
        }

        private void SetupCoinHandler()
        {
            switch (Program.settings.minedCoin)
            {
                case "ETC":
                case "ETH":
                    m_coinHandler = new EthCoin(this); //initialize the coinhandler with the EthCoin class and reference this Redirector instance
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

                case "NICEHASH":
                    m_coinHandler = new NiceHash(this);
                    break;

            }
        }

        internal void SubmittedShare()
        {
            thisMiner.submittedShares++;
            MinerManager.AddSubmittedShare(thisMiner.displayName);
            //todo: add to MinerStats global list
        }

        internal void RejectedShare()
        {
            thisMiner.rejectedShares++;
            MinerManager.AddRejectedShare(thisMiner.displayName);
            //todo: add to MinerStats global list
        }

        internal void AcceptedShare()
        {
            thisMiner.acceptedShares++;
            MinerManager.AddAcceptedShare(thisMiner.displayName);
            //todo: add to MinerStats global list
        }

        internal void SetupMinerStats()
        {
            MinerManager.AddNewMiner(thisMiner.displayName);
            MinerManager.SetConnectionStartTime(thisMiner.displayName, thisMiner.connectionStartTime);
            MinerManager.SetWorkerName(thisMiner.displayName, thisMiner.workerName);
            MinerManager.SetRigName(thisMiner.displayName, thisMiner.rigName);
            MinerManager.SetEndpoint(thisMiner.displayName, thisMiner.endPoint);
            MinerManager.SetConnectionName(thisMiner.displayName, thisMiner.connectionName);
            MinerManager.AddConnectionCount(thisMiner.displayName);
            MinerManager.SetConnectionAlive(thisMiner.displayName, true);
        }

        private void OnServerPacket(byte[] buffer,int length)
        {
            // Is it slow comparing strings every packet?
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

                case "NICEHASH":
                    m_coinHandler.OnEthServerPacket(buffer, length);
                    break;

            }

            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " <----<\r\n" + Encoding.UTF8.GetString(buffer, 0, length)));
        }
        private void OnClientPacket(byte[] buffer, int length)
        {
            // Is it slow comparing strings every packet?
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

                case "NICEHASH":
                    m_coinHandler.OnEthClientPacket(buffer, length);
                    break;

            }
            

            if (Program.settings.log)
             Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " >---->\r\n" + Encoding.UTF8.GetString(buffer,0,length)));
        }

        public void Dispose()
        {
            if (thisMiner.connectionAlive)
            {
                thisMiner.connectionAlive = false;
                MinerManager.SetConnectionAlive(thisMiner.displayName, thisMiner.connectionAlive);

                if (m_client != null)
                    m_client.Dispose();

                if (m_server != null)
                    m_server.Dispose();

                Logger.LogToConsole(string.Format("Closing session: ({0})", thisMiner.connectionName),  thisMiner.endPoint);
                statusUpdateTimer.Enabled = false;
            }
        }
    }
}
