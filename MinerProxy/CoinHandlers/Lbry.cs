using System;
using System.Text;
using Newtonsoft.Json;
using MinerProxy.JsonProtocols;
using MinerProxy.Logging;
using MinerProxy.Network;
using MinerProxy.Miners;
using static MinerProxy.Donations;

namespace MinerProxy.CoinHandlers
{
    class Lbry
    {
        internal Redirector redirector;

        public Lbry(Redirector r)
        {
            redirector = r; //when this class is initialized, a reference to the Redirector class must be passed
            if (Program.settings.debug) Logger.LogToConsole("Lbry handler initialized", redirector.thisMiner.endPoint);
        }

        internal void OnLbryClientPacket(byte[] buffer, int length)
        {
            bool madeChanges = false;
            byte[] newBuffer = null;
            int newLength = 0;
            bool isDonating = false;
            bool isDevFee = false;

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
                                Logger.LogToConsole("Lbry login detected!", redirector.thisMiner.endPoint, ConsoleColor.DarkGreen);
                                madeChanges = true;

                                Newtonsoft.Json.Linq.JValue val = dyn.@params[0];
                                string wallet = val.Value.ToString();
                                //Logger.LogToConsole("wallet: " + wallet, redirector.thisMiner.endPoint);

                                if (wallet.Contains(".") && Program.settings.useDotWithRigName)
                                {//There is likely a rigName in the wallet address.
                                    redirector.thisMiner.replacedWallet = wallet;
                                    redirector.thisMiner.rigName = wallet.Substring(wallet.IndexOf(".") + 1);
                                    redirector.thisMiner.displayName = redirector.thisMiner.rigName;
                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress + "." + redirector.thisMiner.rigName;
                                }
                                else if (wallet.Contains("/") && Program.settings.useSlashWithRigName)
                                {//There is likely different rigname, may need to check for email addresses here as well
                                    redirector.thisMiner.replacedWallet = wallet;
                                    redirector.thisMiner.rigName = wallet.Substring(wallet.IndexOf("/") + 1);
                                    redirector.thisMiner.displayName = redirector.thisMiner.rigName;
                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress + "/" + redirector.thisMiner.rigName;
                                }
                                else if (Program.settings.identifyDevFee)
                                {//there is no rigName, so we just replace the wallet
                                    redirector.thisMiner.replacedWallet = wallet;

                                    if (redirector.thisMiner.replacedWallet != Program.settings.walletAddress)
                                        redirector.thisMiner.displayName = "DevFee";

                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress + "." + redirector.thisMiner.displayName;
                                }
                                else
                                {
                                    if (Program.settings.replaceWallet) dyn.@params[0] = Program.settings.walletAddress;
                                    if (Program.settings.debug) Logger.LogToConsole(string.Format("Worker: {0}", redirector.thisMiner.workerName));
                                }

                                string tempBuffer = JsonConvert.SerializeObject(dyn, Formatting.None) + "\n";

                                val = dyn.@params[0];
                                wallet = val.Value.ToString();

                                if (Program.settings.replaceWallet)
                                {
                                    lock (Logger.ConsoleBlockLock)
                                    {
                                        Logger.LogToConsole("Old Wallet: " + redirector.thisMiner.replacedWallet, redirector.thisMiner.endPoint, ConsoleColor.Yellow);
                                        Logger.LogToConsole("New Wallet: " + wallet, redirector.thisMiner.endPoint, ConsoleColor.Yellow);
                                    }
                                }
                                else
                                {
                                    Logger.LogToConsole(string.Format("Wallet for {0}: {1}", redirector.thisMiner.displayName, wallet));
                                }

                                redirector.SetupMinerStats();

                                newBuffer = Encoding.UTF8.GetBytes(tempBuffer);
                                newLength = tempBuffer.Length;
                            }
                            break;
                        case "mining.submit":
                            redirector.SubmittedShare();
                            Logger.LogToConsole(string.Format(redirector.thisMiner.displayName + " found a share. [{0} shares found]", redirector.thisMiner.submittedShares), redirector.thisMiner.endPoint, ConsoleColor.Green);
                            break;
                    }
                }
            } catch (Exception ex)
            {
                madeChanges = false;
                Logger.LogToConsole(ex.ToString(), redirector.thisMiner.endPoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint);
            }

            if (redirector.thisMiner.connectionAlive && redirector.m_server.Disposed == false)
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

        internal void OnLbryServerPacket(byte[] buffer, int length)
        {
            try
            {
                dynamic dyn = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer, 0, length));

                if (dyn.id != null)
                {
                    switch ((int)dyn.id)
                    {
                        case 2: //Login authorize
                            if ((bool)dyn.result)
                            {
                                Logger.LogToConsole("Stratum Authorization success: " + redirector.thisMiner.displayName, redirector.thisMiner.endPoint, ConsoleColor.DarkGreen);
                            }
                            else
                            {
                                Logger.LogToConsole("Stratum Authorization failure: " + redirector.thisMiner.displayName, redirector.thisMiner.endPoint, ConsoleColor.Red);
                            }
                            break;
                        case 4: //Share
                            if ((bool)dyn.result)
                            {
                                redirector.AcceptedShare();
                                Logger.LogToConsole(string.Format(redirector.thisMiner.displayName + "'s share got accepted. [{0} shares accepted]", redirector.thisMiner.acceptedShares), redirector.thisMiner.endPoint, ConsoleColor.Green);
                            }
                            else
                            {
                                redirector.RejectedShare();
                                Logger.LogToConsole(string.Format(redirector.thisMiner.displayName + "'s share got rejected. [{0} shares rejected]", redirector.thisMiner.rejectedShares), redirector.thisMiner.endPoint, ConsoleColor.Red);
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
                                Logger.LogToConsole(string.Format(redirector.thisMiner.displayName + " got a job"), redirector.thisMiner.endPoint);
                            break;
                        case "mining.set_difficulty":
                            Newtonsoft.Json.Linq.JValue val = dyn.@params[0];
                            string diff = val.Value.ToString();
                            Logger.LogToConsole(string.Format("Pool set difficulty to: " + diff), redirector.thisMiner.endPoint);
                            break;
                    }
                }
            } catch (Exception ex)
            {
                Logger.LogToConsole(ex.ToString(), redirector.thisMiner.endPoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint, ConsoleColor.Red);
            }

            if (redirector.thisMiner.connectionAlive && redirector.m_client.Disposed == false)
                redirector.m_client.Send(buffer, length);
        }
    }
}
