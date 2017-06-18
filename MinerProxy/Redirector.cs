using System;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Timers;

namespace MinerProxy
{
    internal sealed class Redirector : IDisposable
    {
        private readonly string m_name;
        private readonly string m_endpoint;
        private Session m_client, m_server;
        private string m_replacedWallet;
        private string m_rigName = "";
        private string m_workerName = "";
        private string m_displayName = "";
        private bool m_noRigName;
        private bool m_alive;
        private long m_submittedShares = 0;
        private long m_acceptedShares = 0;
        private long m_rejectedShares = 0;

        private long m_hashRate;

        private DateTime m_connectionStartTime;

        private Timer m_statusUpdateTimer;

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

        private void OnServerPacket(byte[] buffer,int length)
        {


            try     // try to deserialize the packet. I didn't want to write a manual deserialization, so this abomination works fine.
                    // yes, it's ugly and I'm sorry.
            {
                try
                {
                    ServerRootObject obj = JsonConvert.DeserializeObject<ServerRootObject>(Encoding.UTF8.GetString(buffer, 0, length));

                    switch (obj.id)
                    {

                        case 0:
                            break;

                        case 3:
                            if (Program.settings.debug)
                                Logger.LogToConsole("eth_getWork from server.", m_endpoint);
                            break;

                        case 4:
                            Logger.LogToConsole("Share accepted?", m_endpoint, ConsoleColor.Green);
                            break;

                        default:
                            if (Program.settings.debug)
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole("From Server1 <----<", m_endpoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Unknown ID: " + obj.id, m_endpoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Param Count: " + obj.result.Count, m_endpoint, ConsoleColor.Gray);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), m_endpoint, ConsoleColor.Gray);
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        ServerRootObjectBool obj = JsonConvert.DeserializeObject<ServerRootObjectBool>(Encoding.UTF8.GetString(buffer, 0, length));

                        if ((obj.error != null) && obj.result.Equals(null))
                        {
                            Logger.LogToConsole(string.Format(("Server error for {0}: {1} {2}"), m_displayName, obj.error.code, obj.error.message), m_endpoint, ConsoleColor.Red);
                        }
                        else if (!obj.result.Equals(null))
                        {
                            if (obj.result == false)
                            {
                                Logger.LogToConsole(string.Format(("Server error for {0}: {1} {2}"), m_displayName, obj.error.code, obj.error.message), m_endpoint, ConsoleColor.Red);
                            }
                        }

                        switch (obj.id)
                        {
                            case 2:
                                if (obj.result == true)
                                {
                                    Logger.LogToConsole("Stratum Authorization success: " + m_displayName,  m_endpoint, ConsoleColor.Green);
                                } else
                                {
                                    Logger.LogToConsole("eth_SubmitLogin failed!",  m_endpoint, ConsoleColor.Red);
                                }
                                break;

                            case 4:
                                if (obj.result == true)
                                {
                                    m_acceptedShares++;

                                    Logger.LogToConsole(string.Format(m_displayName + "'s share got accepted. [{0} shares accepted]", m_acceptedShares),  m_endpoint, ConsoleColor.Green);

                                }
                                else if (obj.result == false)
                                {
                                    m_rejectedShares++;
                                    Logger.LogToConsole(string.Format(m_displayName + "'s share got rejected. [{0} shares rejected]", m_acceptedShares),  m_endpoint, ConsoleColor.Red);
                                }
                                break;

                            case 6:
                                if (Program.settings.debug) Logger.LogToConsole(string.Format("Hashrate accepted: {0}", obj.result),  m_endpoint, ConsoleColor.Green);
                                break;

                            default:
                                if (Program.settings.debug)
                                {
                                    lock (Logger.ConsoleBlockLock)
                                    {
                                        Logger.LogToConsole("From Server2 <----<", m_endpoint);
                                        Logger.LogToConsole("Unknown ID: " + obj.id, m_endpoint);
                                        Logger.LogToConsole("Result: " + obj.result, m_endpoint);
                                        Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), m_endpoint);
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception ex2)
                    {
                        try
                        {
                            ServerRootObjectError obj = JsonConvert.DeserializeObject<ServerRootObjectError>(Encoding.UTF8.GetString(buffer, 0, length));

                            if (obj.error != null && obj.error.Length > 0)
                            {
                                if (obj.result == false)
                                    Logger.LogToConsole(string.Format(("Server error for {0}: {1}"), m_displayName, obj.error), m_endpoint, ConsoleColor.Red);
                            }
                            else
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole(ex2.ToString(), m_endpoint);
                                    Logger.LogToConsole("From Server3 <----<", m_endpoint);
                                    Logger.LogToConsole("ID: " + obj.id, m_endpoint);
                                    Logger.LogToConsole("Result: " + obj.result, m_endpoint);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), m_endpoint);
                                }
                            }
                        }
                        catch (Exception ex3)
                        {
                            if (Program.settings.debug)
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole(ex3.ToString(), m_endpoint);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), m_endpoint);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToConsole(ex.ToString(),  m_endpoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length),  m_endpoint, ConsoleColor.Red);
            }

            if (m_alive && m_client.Disposed == false)
                m_client.Send(buffer, length);

            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " <----<\r\n" + Encoding.UTF8.GetString(buffer,0,length)));

        }
        private void OnClientPacket(byte[] buffer, int length)
        {
            OnEthPacket(buffer, length);

            if (Program.settings.log)
             Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " >---->\r\n" + Encoding.UTF8.GetString(buffer,0,length)));
        }

        private void OnEthPacket(byte[] buffer, int length)
        {
            bool madeChanges = false;
            byte[] newBuffer = null;
            int newLength = 0;

            try   //try to deserialize the packet, if it's not Json it will fail. that's ok.
            {

                ClientRootObject obj;

                obj = JsonConvert.DeserializeObject<ClientRootObject>(Encoding.UTF8.GetString(buffer, 0, length));

                switch (obj.id)
                {
                    case 2: //eth_submitLogin
                        Logger.LogToConsole("Ethereum Login detected!",  m_endpoint, ConsoleColor.DarkGreen);
                        madeChanges = true;
                        if (obj.@params[0].Contains("."))
                        {//There is likely a rigName in the wallet address.

                            m_replacedWallet = obj.@params[0];
                            m_rigName = obj.@params[0].Substring(obj.@params[0].IndexOf(".") + 1);
                            m_displayName = m_rigName;
                            obj.@params[0] = Program.settings.walletAddress + "." + m_rigName;
                        }
                        else if (obj.@params[0].Contains("/"))
                        {//There is likely different rigname, may need to check for email addresses here as well
                            m_replacedWallet = obj.@params[0];
                            m_rigName = obj.@params[0].Substring(obj.@params[0].IndexOf("/") + 1);
                            m_displayName = m_rigName;
                            obj.@params[0] = Program.settings.walletAddress + "/" + m_rigName;
                        }
                        else if (Program.settings.identifyDevFee)
                        { //there is no rigName, so we just replace the wallet
                            m_replacedWallet = obj.@params[0];

                            if (m_replacedWallet != Program.settings.walletAddress)
                                m_displayName = "DevFee";

                            if (obj.worker == null)
                            {
                                //if rigName exists, add the rigname to the new wallet, else just use wallet
                                obj.@params[0] = Program.settings.walletAddress;
                                m_noRigName = true;
                            }
                            else if (obj.worker.Equals("eth1.0"))
                            { //It's probably a DevFee
                                
                                if (m_replacedWallet != Program.settings.walletAddress)
                                { //if the wallet we're replacing isn't ours, it's the DevFee
                                    m_displayName = "DevFee";
                                }
                                else
                                {
                                    m_noRigName = true;
                                    m_displayName = m_name;
                                }
                                obj.@params[0] = Program.settings.walletAddress;
                            }
                            else
                            {
                                m_displayName = obj.worker;
                                m_workerName = obj.worker;
                                obj.@params[0] = Program.settings.walletAddress;
                                if (Program.settings.debug) Logger.LogToConsole(string.Format("Worker: {0}", m_workerName), m_endpoint, ConsoleColor.DarkGreen);
                            }
                        }
                        else
                        { //No rigName, but don't replace it with DevFee, either.
                            m_replacedWallet = obj.@params[0];
                            if (obj.worker != null) m_displayName = obj.worker;
                            obj.@params[0] = Program.settings.walletAddress;
                        }

                        string tempBuffer = JsonConvert.SerializeObject(obj, Formatting.None) + "\n";

                        lock (Logger.ConsoleBlockLock)
                        {
                            Logger.LogToConsole("Old Wallet: " + m_replacedWallet, m_endpoint, ConsoleColor.Yellow);
                            Logger.LogToConsole("New Wallet: " + obj.@params[0], m_endpoint, ConsoleColor.Yellow);
                        }

                        newBuffer = Encoding.UTF8.GetBytes(tempBuffer);
                        newLength = tempBuffer.Length;

                        break;

                    case 3: //eth_getWork
                        if (Program.settings.debug) Logger.LogToConsole("eth_getWork from Client.",  m_endpoint);
                        break;

                    case 4: //eth_submitWork
                        m_submittedShares++;
                        Logger.LogToConsole(string.Format(m_displayName + " found a share. [{0} shares found]", m_submittedShares),  m_endpoint, ConsoleColor.Green);
                        break;

                    case 6: //eth_submitHashrate
                        long hashrate = Convert.ToInt64(obj.@params[0], 16);
                        m_hashRate = hashrate;
                        if (Program.settings.debug)
                        {
                            Logger.LogToConsole(string.Format("Hashrate reported by {0}: {1}", m_displayName, hashrate.ToString("#,##0,Mh/s").Replace(",", ".")),  m_endpoint,ConsoleColor.Magenta);
                        }
                        break;

                    default:
                        if (Program.settings.debug)
                        {
                            lock (Logger.ConsoleBlockLock)
                            {
                                Logger.LogToConsole("From Client >---->", m_endpoint);
                                Logger.LogToConsole("Unknown ID: " + obj.id, m_endpoint);
                                Logger.LogToConsole("Method: " + obj.method, m_endpoint);
                                Logger.LogToConsole("Param Count: " + obj.@params.Count, m_endpoint);
                                Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), m_endpoint);
                            }
                        }
                        break;

                }

            }
            catch (Exception ex)
            {
                madeChanges = false;
                Logger.LogToConsole(ex.Message,  m_endpoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length),  m_endpoint, ConsoleColor.Red);
            }

            if (m_alive && m_server.Disposed == false)
            {
                if (madeChanges == false)
                {
                    //if (m_debug) Logger.LogToConsole("Sending buffer: " + Encoding.UTF8.GetString(buffer, 0, length),  m_endpoint);
                    m_server.Send(buffer, length);
                }
                else
                {
                    //if (m_debug) Logger.LogToConsole("Sending modified buffer: " + Encoding.UTF8.GetString(newBuffer, 0, newLength),  m_endpoint);
                    m_server.Send(newBuffer, newLength);
                }
            }
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
