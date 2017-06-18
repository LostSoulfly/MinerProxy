using System;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        private long m_acceptedShares= 0;
        private long m_hashRate;
        private DateTime m_connectionStartTime;
        private Timer m_statusUpdateTimer;
        
        private void OnStatusUpdate(object source, ElapsedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            DateTime timeNow = DateTime.Now;
            TimeSpan timeSpan = (timeNow - m_connectionStartTime);
            double hours = timeSpan.TotalHours;
            double minutes = timeSpan.TotalMinutes;
            double sharesPerMinute = (m_submittedShares / minutes);
            double sharesPerMinuteTruncated = Math.Truncate(sharesPerMinute * 100) / 100;
            double sharesPerHour = (m_submittedShares / hours);
            double sharesPerHourTruncated = Math.Truncate(sharesPerHour * 100) / 100;

            Logger.LogToConsole(string.Format(m_rigName + "'s status update: "));
            Logger.LogToConsole(string.Format("Hashrate: {0}", m_hashRate.ToString("#,##0,Mh/s").Replace(",", ".")));
            Logger.LogToConsole(string.Format("Found shares: {0}", m_submittedShares));
            Logger.LogToConsole(string.Format("Accepted shares: {0}", m_acceptedShares));
            Logger.LogToConsole(string.Format("Rejected shares: {0}", m_rejectedShares));
            Logger.LogToConsole(string.Format("Time connected: {0}", timeSpan.ToString("hh\\:mm")));
            Logger.LogToConsole(string.Format("Shares per minute: {0}", string.Format("{0:N2}", sharesPerMinuteTruncated)));
            Logger.LogToConsole(string.Format("Shares per hour: {0}", string.Format("{0:N2}", sharesPerHourTruncated)));

            Console.ResetColor();
        }
        
        public Redirector(Socket client, string ip, int port)
        {
            m_name = client.RemoteEndPoint.ToString();
            m_connectionStartTime = DateTime.Now;

            m_statusUpdateTimer = new Timer();
            m_statusUpdateTimer.Elapsed += new ElapsedEventHandler(OnStatusUpdate);
            m_statusUpdateTimer.Interval = 60000;
            m_statusUpdateTimer.Enabled = true;
            
            Logger.LogToConsole(string.Format("Session started: ({0})", m_name));

            int index = m_name.IndexOf(":");
            m_endpoint = m_name.Substring(0, index);
            m_endpoint = m_endpoint + "_" + m_name.Substring(index + 1);

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
                Logger.LogToConsole(string.Format("Connection bridge failed with {0} ({1})",se.ErrorCode,m_name));
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
                            //Logger.LogToConsole("eth_getWork from server.");
                            break;

                        case 4:
                            Logger.LogToConsole("Share accepted?");
                            break;

                        default:
                            if (Program.settings.debug)
                            {
                                Logger.LogToConsole("From Server1 <----<");
                                Logger.LogToConsole("Unknown ID: " + obj.id);
                                Logger.LogToConsole("Param Count: " + obj.result.Count);
                                Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length));
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
                            Logger.LogToConsole(string.Format(("Server error for {0}: {1} {2}"), m_displayName, obj.error.code, obj.error.message));
                        }
                        else if (!obj.result.Equals(null))
                        {
                            if (obj.result == false)
                            {
                                Logger.LogToConsole(string.Format(("Server error for {0}: {1} {2}"), m_displayName, obj.error.code, obj.error.message));
                            }
                        }
                        switch (obj.id)
                        {
                            case 2:
                                if (obj.result == true)
                                {
                                    Logger.LogToConsole("Stratum Authorization success: " + m_displayName);
                                }
                                else
                                {
                                    Logger.LogToConsole("eth_SubmitLogin failed!");
                                }
                                break;

                            case 4:
                                m_acceptedShares++;
                                Logger.LogToConsole(string.Format("Share accepted: " + m_displayName + " [{0}] ", m_acceptedShares));
                                break;

                            case 6:
                                if (Program.settings.debug) Logger.LogToConsole(string.Format("Hashrate accepted: {0}", obj.result));
                                break;

                            default:
                                if (Program.settings.debug)
                                {
                                    Logger.LogToConsole("From Server2 <----<");
                                    Logger.LogToConsole("Unknown ID: " + obj.id);
                                    Logger.LogToConsole("Result: " + obj.result);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length));
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
                                Logger.LogToConsole(string.Format(("Server error for {0}: {1}"), m_displayName, obj.error));
                            }
                            else
                            {
                                Logger.LogToConsole(ex2.ToString());
                                Logger.LogToConsole("From Server3 <----<");
                                Logger.LogToConsole("ID: " + obj.id);
                                Logger.LogToConsole("Result: " + obj.result);
                                Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length));
                            }

                        }
                        catch (Exception ex3)
                        {
                            if (Program.settings.debug)
                            {
                                Logger.LogToConsole(ex3.ToString());
                                Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length));
                            }
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                Logger.LogToConsole(ex.ToString());
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length));
            }

            if (m_alive && m_client.Disposed == false)
                m_client.Send(buffer, length);

            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage(m_endpoint + ".txt", DateTime.Now.ToLongTimeString() + " <----<\r\n" + Encoding.UTF8.GetString(buffer,0,length)));

        }
        private void OnClientPacket(byte[] buffer, int length)
        {
            OnEthPacket(buffer, length);

            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage(m_endpoint + ".txt", DateTime.Now.ToLongTimeString() + " >---->\r\n" + Encoding.UTF8.GetString(buffer,0,length)));
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
                        Logger.LogToConsole("Ethereum Login detected!");
                        madeChanges = true;
                        if (obj.@params[0].Contains("."))
                        {   //There is likely a rigName in the wallet address.

                            m_replacedWallet = obj.@params[0];
                            m_rigName = obj.@params[0].Substring(obj.@params[0].IndexOf(".") + 1);
                            m_displayName = m_rigName;
                            obj.@params[0] = Program.settings.walletAddress + "." + m_rigName;

                        }
                        else if (Program.settings.identifyDevFee)
                        { //there is no rigName, so we just replace the wallet
                            m_replacedWallet = obj.@params[0];

                            if (m_replacedWallet != Program.settings.walletAddress)
                                m_displayName = "DevFee";
                            
                            //Still no rigName? Not even from the Worker field?
                            if (obj.worker == null)
                            {
                                //No rigname and no worker? Just set the wallet address to our own and call it a day
                                obj.@params[0] = Program.settings.walletAddress;
                                m_noRigName = true;
                            }
                            else if (obj.worker.Equals("eth1.0")) {
                                //It's probably a DevFee - Could leave the DevFee wallet alone in future release but give it unique rigName
                                //Here's an address of his: 0xc6F31A79526c641de4E432CB22a88BB577A67eaC
                                //All mining is 'default' but we could stand out a bit :D
                                if (m_replacedWallet != Program.settings.walletAddress)
                                { //if the wallet we're replacing isn't ours, it's the DevFee
                                    m_displayName = "DevFee";
                                } else
                                {
                                    m_noRigName = true;
                                    m_displayName = m_name;
                                }
                                obj.@params[0] = Program.settings.walletAddress;     //regardless what the wallet was, let's replace it
                            }
                            else {
                                m_displayName = obj.worker;
                                m_workerName = obj.worker;
                                obj.@params[0] = Program.settings.walletAddress;
                                if (Program.settings.debug) Logger.LogToConsole(string.Format("Worker: {0}", m_workerName));
                            }
                        }
                        else
                        { //No rigName, but don't replace it with DevFee, either.
                            m_replacedWallet = obj.@params[0];
                            if (obj.worker != null) m_displayName = obj.worker;
                            obj.@params[0] = Program.settings.walletAddress;
                        }
                        string tempBuffer = JsonConvert.SerializeObject(obj, Formatting.None) + "\n"; 
                        //if (Program.settings.debug) Logger.LogToConsole("Before: " + Encoding.UTF8.GetString(buffer, 0, length));
                        Logger.LogToConsole("Old Wallet: " + m_replacedWallet);
                        Logger.LogToConsole("New Wallet: " + obj.@params[0]);

                        newBuffer = Encoding.UTF8.GetBytes(tempBuffer);
                        newLength = tempBuffer.Length;

                        //if (Program.settings.debug) Logger.LogToConsole("After: " + Encoding.UTF8.GetString(newBuffer, 0, newLength));
                        break;

                    case 3: //eth_getWork
                        if (Program.settings.debug) Logger.LogToConsole("eth_getWork from Client");
                        break;

                    case 4: //eth_submitWork
                        m_submittedShares++;
                        Logger.LogToConsole(string.Format("Share found: " + m_displayName + " [{0}] ", m_submittedShares));
                        break;

                    case 6: //eth_submitHashrate
                        long hashrate = Convert.ToInt64(obj.@params[0], 16);
                        m_hashRate = hashrate;
                        if (Program.settings.debug)
                        {
                            Logger.LogToConsole(string.Format("Hashrate reported by {0}: {1}", m_displayName, hashrate.ToString("#,##0,Mh/s").Replace(",", "."))); ;
                        }
                        break;

                    default:
                        if (Program.settings.debug)
                        {
                            Logger.LogToConsole("From Client >---->");
                            Logger.LogToConsole("Unknown ID: " + obj.id);
                            Logger.LogToConsole("Method: " + obj.method);
                            Logger.LogToConsole("Param Count: " + obj.@params.Count);
                            Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length));
                        }
                        break;

                }

            }
            catch (Exception ex)
            {
                madeChanges = false;    //make sure we don't pass an empty newBuffer to the server
                Logger.LogToConsole(ex.Message);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length));
            }

            if (m_alive && m_server.Disposed == false)
            {
                if (madeChanges == false)
                {
                    //if (Program.settings.debug) Logger.LogToConsole("Sending buffer: " + Encoding.UTF8.GetString(buffer, 0, length));
                    m_server.Send(buffer, length);
                }
                else
                {
                    //if (Program.settings.debug) Logger.LogToConsole("Sending modified buffer: " + Encoding.UTF8.GetString(newBuffer, 0, newLength));
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

                Logger.LogToConsole(string.Format("Closing session: ({0})", m_name));
            }
        }
    }
}
