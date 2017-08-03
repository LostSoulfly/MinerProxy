using System;
using System.Text;
using System.Net.Sockets;
using System.Timers;
using MinerProxy.CoinHandlers;
using MinerProxy.Logging;
using MinerProxy.Miners;
using System.Net;

namespace MinerProxy.Network
{
    internal sealed class Redirector : IDisposable
    {
        internal Session m_client, m_server;
        internal dynamic m_coinHandler;
        internal bool m_changingServers;
        internal byte[] m_loginBuffer;
        internal int m_loginLength;

        internal Donations.DonateList m_donation;

        internal MinerStats thisMiner;
        
        public Timer statusUpdateTimer;

        private void OnStatusUpdate(object source, ElapsedEventArgs e)
        {
            DateTime timeNow = DateTime.Now;
            TimeSpan timeSpan = (timeNow - thisMiner.connectionStartTime);
            CalculateConnectedTime();

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
                
                if (thisMiner.hashrate != 0) // Nicehash doesn't report hashrate
                    Logger.LogToConsole(string.Format("Hashrate: {0}", thisMiner.hashrate.ToString("#,##0,Mh/s").Replace(",", ".")), thisMiner.endPoint, color);

                if (thisMiner.submittedShares != thisMiner.acceptedShares) //No reason to show if they match, save space with multiple rigs
                    Logger.LogToConsole(string.Format("Found shares: {0}", thisMiner.submittedShares), thisMiner.endPoint, color);

                if (thisMiner.acceptedShares > 0) Logger.LogToConsole(string.Format("Accepted shares: {0}", thisMiner.acceptedShares), thisMiner.endPoint, color);
                if (thisMiner.rejectedShares > 0) Logger.LogToConsole(string.Format("Rejected shares: {0}", thisMiner.rejectedShares), thisMiner.endPoint, color);
                Logger.LogToConsole(string.Format("Time connected: {0}", timeSpan.ToString("hh\\:mm")), thisMiner.endPoint, color);
                Logger.LogToConsole(string.Format("Shares per minute: {0}", string.Format("{0:N2}", sharesPerMinuteTruncated)), thisMiner.endPoint, color);
                Logger.LogToConsole(string.Format("Shares per hour: {0}", string.Format("{0:N2}", sharesPerHourTruncated)), thisMiner.endPoint, color);
            }
        }

        private void CalculateConnectedTime()
        {

            TimeSpan calculatedSpan = (DateTime.Now - thisMiner.lastCalculatedTime);     //determine how long it's been since the last cycle
            thisMiner.lastCalculatedTime = DateTime.Now;                            //set the lastCalc time to now, so we can do it again next time
            MinerManager.AddConnectedTime(thisMiner.displayName, calculatedSpan);
        }

        public Redirector(Socket client, string ip, int port)
        {
            thisMiner = new MinerStats();
            IPEndPoint remoteEndPoint = client.RemoteEndPoint as IPEndPoint;
            
            thisMiner.connectionName = client.RemoteEndPoint.ToString();
            thisMiner.connectionStartTime = DateTime.Now;
            thisMiner.lastCalculatedTime = DateTime.Now;
            thisMiner.endPoint = remoteEndPoint.Address.ToString() + "_" + remoteEndPoint.Port.ToString();

            SetupCoinHandler();

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
            
            Logger.LogToConsole(string.Format("Session started: ({0})", thisMiner.connectionName), thisMiner.endPoint, ConsoleColor.DarkGreen);

            thisMiner.connectionAlive = false;
            MinerManager.SetConnectionAlive(thisMiner.displayName, thisMiner.connectionAlive);
            
            m_client = new Session(client);
            m_client.OnDataReceived = OnClientPacket;
            m_client.OnDisconnected = Dispose;
            m_client.ip = remoteEndPoint.Address.ToString();
            m_client.port = remoteEndPoint.Port.ToString();

            var outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            outSocket.BeginConnect(ip, port, EndConnect, outSocket);
        }

        private void EndConnect(IAsyncResult iar)
        {
            try
            {
                var outSocket = iar.AsyncState as Socket;

                outSocket.EndConnect(iar);
                IPEndPoint remoteEndPoint = outSocket.RemoteEndPoint as IPEndPoint;

                m_server = new Session(outSocket);
                m_server.OnDataReceived = OnServerPacket;
                m_server.OnDisconnected = Dispose;
                m_server.ip = remoteEndPoint.Address.ToString();
                m_server.port = remoteEndPoint.Port.ToString();

                thisMiner.connectionAlive = true;
                MinerManager.SetConnectionAlive(thisMiner.displayName, thisMiner.connectionAlive);

                m_server.Receive();

                if (!m_changingServers)
                {
                    m_client.Receive();
                    Program.IncrementClientCount();
                }

                if (m_changingServers)
                {
                    if (Program.settings.debug)
                        Logger.LogToConsole(string.Format("ChangeServer successful. New server: {0}:{1}", m_server.ip, m_server.port), thisMiner.endPoint);
                    m_server.Send(m_loginBuffer, m_loginLength);
                    
                    m_changingServers = false;
                }

            }
            catch (SocketException se)
            {
                if (m_changingServers)
                {
                    Logger.LogToConsole(string.Format("Donation pool connection failed, using original pool {0} ({1})", se.ErrorCode, thisMiner.connectionName), thisMiner.endPoint);
                    ChangeServer(Program.settings.remotePoolAddress, Program.settings.remotePoolPort);
                }
                else
                {
                    m_client.Dispose();
                    statusUpdateTimer.Enabled = false;
                    Logger.LogToConsole(string.Format("Connection bridge failed with {0} ({1})", se.ErrorCode, thisMiner.connectionName), thisMiner.endPoint, ConsoleColor.Red);
                }
            }
        }

        internal void ChangeServer(string ip, int port)
        {
            m_changingServers = true;
            Logger.LogToConsole(string.Format("ChangeServer initiated to {0}:{1}", ip, port), thisMiner.endPoint);
            m_server.Dispose();
            try
            {
                var outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                outSocket.BeginConnect(ip, port, EndConnect, outSocket);
            }
            catch (Exception ex)
            {
                Logger.LogToConsole(ex.Message);
            }
        }

        public void Dispose()
        {
            if (thisMiner.connectionAlive)
            {
                thisMiner.connectionAlive = false;
                MinerManager.SetConnectionAlive(thisMiner.displayName, thisMiner.connectionAlive);
                
                CalculateConnectedTime();   //update the miner's connected time, more accurately than just in the status timer

                if (!m_changingServers)
                {
                    Logger.LogToConsole(string.Format("Closing session: ({0})", thisMiner.connectionName), thisMiner.endPoint);
                    statusUpdateTimer.Enabled = false;
                    Program.DecrementClientCount();
                }
                else
                {
                    Logger.LogToConsole(string.Format("Closing server connection: {0}:{1}", m_server.ip, m_server.port), thisMiner.endPoint);
                }

                if (m_client != null && !m_changingServers)
                    m_client.Dispose();

                if (m_server != null)
                    m_server.Dispose();

            }
        }

        private void OnServerPacket(byte[] buffer,int length)
        {
            // Is it slow comparing strings every packet?
            switch (Program.settings.minedCoin)
            {
                case "UBQ":
                case "UBIQ":
                case "EXP":
                case "ETC":
                case "ETH":
                    m_coinHandler.OnEthServerPacket(buffer, length);
                    break;

                case "SIA":
                case "SC":
                    m_coinHandler.OnSiaServerPacket(buffer, length);
                    break;
                    
                case "ZCASH":
                case "HUSH":
                case "ZEC":
                    m_coinHandler.OnZcashServerPacket(buffer, length);
                    break;

                case "PASC":
                    break;

                case "DCR":
                    break;

                case "LBRY":
                    break;

                case "CRYPTONOTE":
                case "CRY":
                    break;

                case "NICEHASH":
                    m_coinHandler.OnEthServerPacket(buffer, length);
                    break;

                case "XMR":
                    m_coinHandler.OnMoneroServerPacket(buffer, length);
                    break;

                case "TCP":
                 if (thisMiner.connectionAlive && m_client.Disposed == false)
                        m_client.Send(buffer, length);
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
                case "UBQ":
                case "UBIQ":
                case "EXP":
                case "ETC":
                case "ETH":
                    m_coinHandler.OnEthClientPacket(buffer, length);
                    break;

                case "SIA":
                case "SC":
                    m_coinHandler.OnSiaClientPacket(buffer, length);
                    break;
                    
                case "ZCASH":
                case "HUSH":
                case "ZEC":
                    m_coinHandler.OnZcashClientPacket(buffer, length);
                    break;

                case "PASC":
                    break;

                case "DCR":
                    break;

                case "LBRY":
                    break;

                case "CRYPTONOTE":
                case "CRY":
                    break;

                case "NICEHASH":
                    m_coinHandler.OnEthClientPacket(buffer, length);
                    break;

                case "XMR":
                    m_coinHandler.OnMoneroClientPacket(buffer, length);
                    break;


                case "TCP":
                    if (thisMiner.connectionAlive && m_server.Disposed == false)
                        m_server.Send(buffer, length);
                    break;

            }


            if (Program.settings.log)
             Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " >---->\r\n" + Encoding.UTF8.GetString(buffer,0,length)));
        }
        
        private void SetupCoinHandler()
        {
            switch (Program.settings.minedCoin)
            {
                case "UBQ":
                case "UBIQ":
                case "EXP":
                case "ETC":
                case "ETH":
                    m_coinHandler = new EthCoin(this); //initialize the coinhandler with the EthCoin class and reference this Redirector instance
                    break;

                case "SIA":
                case "SC":
                    m_coinHandler = new SiaCoin(this);
                    break;

                case "HUSH":
                case "ZEC":
                case "ZCASH":
                    m_coinHandler = new Zcash(this);
                    break;

                case "PASC":
                    break;

                case "DCR":
                    break;

                case "LBRY":
                    break;


                case "CRYPTONOTE":
                case "CRY":
                    break;

                case "NICEHASH":
                    m_coinHandler = new NiceHash(this);
                    break;

                case "XMR":
                    m_coinHandler = new MoneroCoin(this);
                    break;



                case "TCP":
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
            if (string.IsNullOrEmpty(thisMiner.displayName))
                thisMiner.displayName = thisMiner.rigName;

            if (Program.settings.useRigNameAsEndPoint)
                thisMiner.endPoint = thisMiner.displayName;
            
            MinerManager.AddNewMiner(thisMiner.displayName);
            MinerManager.SetConnectionStartTime(thisMiner.displayName, thisMiner.connectionStartTime);
            MinerManager.SetWorkerName(thisMiner.displayName, thisMiner.workerName);
            MinerManager.SetRigName(thisMiner.displayName, thisMiner.rigName);
            MinerManager.SetEndpoint(thisMiner.displayName, thisMiner.endPoint);
            MinerManager.SetConnectionName(thisMiner.displayName, thisMiner.connectionName);
            MinerManager.AddConnectionCount(thisMiner.displayName);
            MinerManager.SetConnectionAlive(thisMiner.displayName, true);
            MinerManager.AddMinerWallet(thisMiner.displayName, thisMiner.replacedWallet);
        }

    }
}
