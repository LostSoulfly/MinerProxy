using System;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Timers;
using MinerProxy.CoinHandlers;
using MinerProxy.JsonProtocols;

namespace MinerProxy.CoinHandlers
{
    class NiceHash
    {
        internal Redirector redirector;

        public NiceHash(Redirector r)
        {
            if (Program.settings.debug) Logger.LogToConsole("NiceHash handler initialized");
            redirector = r; //when this class is initialized, a reference to the Redirector class must be passed
        }

        internal void OnEthClientPacket(byte[] buffer, int length)
        {
            bool madeChanges = false;
            byte[] newBuffer = null;
            int newLength = 0;

            try
            {
                dynamic dyn = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer, 0, length));

                if (dyn.method != null)
                {
                    switch ((string)dyn.method)
                    {
                        case "mining.authorize":
                            if (dyn.@params != null)
                            {
                                Logger.LogToConsole("Ethereum Login detected!", redirector.m_endpoint, ConsoleColor.DarkGreen);
                                madeChanges = true;

                                Newtonsoft.Json.Linq.JValue val = dyn.@params[0];
                                string wallet = val.Value.ToString();
                                Logger.LogToConsole("wallet: " + wallet, redirector.m_endpoint);


                                if (wallet.Contains(".") && Program.settings.useDotWithRigName)
                                {//There is likely a rigName in the wallet address.
                                    redirector.m_replacedWallet = wallet;
                                    redirector.m_rigName = wallet.Substring(wallet.IndexOf(".") + 1);
                                    redirector.m_displayName = redirector.m_rigName;
                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress + "." + redirector.m_rigName;
                                }
                                else if (wallet.Contains("/") && Program.settings.useSlashWithRigName)
                                {//There is likely different rigname, may need to check for email addresses here as well
                                    redirector.m_replacedWallet = wallet;
                                    redirector.m_rigName = wallet.Substring(wallet.IndexOf("/") + 1);
                                    redirector.m_displayName = redirector.m_rigName;
                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress + "/" + redirector.m_rigName;
                                }
                                else if (Program.settings.identifyDevFee)
                                {//there is no rigName, so we just replace the wallet
                                    redirector.m_replacedWallet = wallet;

                                    if (redirector.m_replacedWallet != Program.settings.walletAddress)
                                        redirector.m_displayName = "DevFee";

                                    
                                }
                                else
                                {
                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress;
                                    if (Program.settings.debug) Logger.LogToConsole(string.Format("Worker: {0}", redirector.m_workerName));
                                }

                                string tempBuffer = JsonConvert.SerializeObject(dyn, Formatting.None) + "\n";

                                val = dyn.@params[0];
                                wallet = val.Value.ToString();

                                if (Program.settings.replaceWallet)
                                {
                                    lock (Logger.ConsoleBlockLock)
                                    {
                                        Logger.LogToConsole("Old Wallet: " + redirector.m_replacedWallet, redirector.m_endpoint, ConsoleColor.Yellow);
                                        Logger.LogToConsole("New Wallet: " + wallet, redirector.m_endpoint, ConsoleColor.Yellow);
                                    }
                                }
                                else
                                {
                                    Logger.LogToConsole(string.Format("Wallet for {0}: {1}", redirector.m_displayName, wallet));
                                }

                                newBuffer = Encoding.UTF8.GetBytes(tempBuffer);
                                newLength = tempBuffer.Length;
                            }
                            break;
                        case "mining.submit":
                            redirector.m_submittedShares++;
                            Logger.LogToConsole(string.Format(redirector.m_displayName + " found a share. [{0} shares found]", redirector.m_submittedShares), redirector.m_endpoint, ConsoleColor.Green);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                madeChanges = false;
                Logger.LogToConsole(ex.ToString(), redirector.m_endpoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint);
            }

            if (redirector.m_alive && redirector.m_server.Disposed == false)
            {
                if (!madeChanges)
                {
                    redirector.m_server.Send(buffer, length);
                }
                else
                {
                    redirector.m_server.Send(newBuffer, newLength);
                }
            }
        }

        internal void OnEthServerPacket(byte[] buffer, int length)
        {
            try
            {
                dynamic dyn = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer, 0, length));

                //Logger.LogToConsole("dyn.method: " + dyn.method);

                if (dyn.id != null)
                {
                    switch ((int)dyn.id)
                    {
                        case 2: //Login authorize
                            if ((bool)dyn.result)
                            {
                                Logger.LogToConsole("Stratum Authorization success: " + redirector.m_displayName, redirector.m_endpoint, ConsoleColor.DarkGreen);
                            }
                            else
                            {
                                Logger.LogToConsole("Stratum Authorization failure: " + redirector.m_displayName, redirector.m_endpoint, ConsoleColor.Red);
                            }
                            break;
                        case 4: //Share
                            if ((bool)dyn.result)
                            {
                                redirector.m_acceptedShares++;
                                Logger.LogToConsole(string.Format(redirector.m_displayName + "'s share got accepted. [{0} shares accepted]", redirector.m_acceptedShares), redirector.m_endpoint, ConsoleColor.Green);
                            }
                            else
                            {
                                redirector.m_rejectedShares++;
                                Logger.LogToConsole(string.Format(redirector.m_displayName + "'s share got rejected. [{0} shares rejected]", redirector.m_acceptedShares), redirector.m_endpoint, ConsoleColor.Red);
                            }
                            break;
                    }
                }

                if (dyn.method != null)
                {
                    switch ((string)dyn.method)
                    {
                        case "mining.notify":
                            if (Program.settings.debug)
                                Logger.LogToConsole(string.Format(redirector.m_displayName + " got a job"), redirector.m_endpoint);
                            break;
                        case "mining.set_difficulty":
                            Newtonsoft.Json.Linq.JValue val = dyn.@params[0];
                            string diff = val.Value.ToString();
                            Logger.LogToConsole(string.Format("Pool set difficulty to: " + diff), redirector.m_endpoint);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogToConsole(ex.ToString(), redirector.m_endpoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint, ConsoleColor.Red);
            }

            if (redirector.m_alive && redirector.m_client.Disposed == false)
                redirector.m_client.Send(buffer, length);
        }
    }
}
