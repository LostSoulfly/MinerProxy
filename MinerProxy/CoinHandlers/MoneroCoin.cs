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
    class MoneroCoin
    {
        internal Redirector redirector;

        public MoneroCoin(Redirector r)
        {
            redirector = r; //when this class is initialized, a reference to the Redirector class must be passed
            if (Program.settings.debug) Logger.LogToConsole("MoneroCoin handler initialized", redirector.thisMiner.endPoint);
        }

        internal void OnMoneroClientPacket(byte[] buffer, int length)
        {
            bool madeChanges = false;
            byte[] newBuffer = null;
            int newLength = 0;
            bool isDonating = false;
            bool isDevFee = false;

            try   //try to deserialize the packet, if it's not Json it will fail. that's ok.
            {

                MoneroClientRootObject obj;

                obj = JsonConvert.DeserializeObject<MoneroClientRootObject>(Encoding.UTF8.GetString(buffer, 0, length));
                switch (obj.id) {

                    case 1: //Monero login
                        if (!string.IsNullOrEmpty(obj.@params.login))
                        {

                            string wallet;

                            DonateList d = new DonateList();

                            isDonating = CheckForDonation(out d, "XMR");

                            if (string.IsNullOrWhiteSpace(Program.settings.devFeeWalletAddress))
                            {
                                wallet = Program.settings.walletAddress;
                            }
                            else
                            {
                                wallet = Program.settings.devFeeWalletAddress;
                            }

                            Logger.LogToConsole("Monero login detected!", redirector.thisMiner.endPoint);
                            if (Program.settings.replaceWallet)
                            {
                                madeChanges = true;
                                redirector.thisMiner.replacedWallet = obj.@params.login;

                                if (!obj.@params.login.Contains("."))
                                {
                                    if (redirector.thisMiner.replacedWallet != Program.settings.walletAddress && Program.settings.identifyDevFee)
                                    {
                                        redirector.thisMiner.rigName = "DevFee";
                                        isDevFee = true;

                                        if (Program.settings.useDotWithRigName)
                                            obj.@params.login = wallet + ".DevFee";
                                    }
                                    else
                                    {
                                        redirector.thisMiner.rigName = redirector.thisMiner.endPoint;
                                    }
                                }
                                else
                                {
                                    if (Program.settings.useDotWithRigName)
                                    {
                                        redirector.thisMiner.rigName = obj.@params.login.Substring(obj.@params.login.IndexOf(".") + 1);
                                        redirector.thisMiner.displayName = redirector.thisMiner.rigName;

                                        if (Program.settings.replaceWallet)
                                            obj.@params.login = wallet + "." + redirector.thisMiner.rigName;

                                    }
                                    else
                                    {
                                        obj.@params.login = wallet;
                                    }
                                }
                                if (Program.settings.usePasswordAsRigName)
                                {
                                    redirector.thisMiner.rigName = obj.@params.pass;
                                    obj.@params.pass = "x";
                                }

                                Logger.LogToConsole("Old Wallet: " + redirector.thisMiner.replacedWallet, redirector.thisMiner.endPoint, ConsoleColor.Yellow);
                                Logger.LogToConsole("New Wallet: " + obj.@params.login, redirector.thisMiner.endPoint, ConsoleColor.Yellow);

                                redirector.SetupMinerStats();
                                string tempBuffer = JsonConvert.SerializeObject(obj, Formatting.None) + "\n";
                                newBuffer = Encoding.UTF8.GetBytes(tempBuffer);
                                newLength = tempBuffer.Length;
                                
                                /*
                                 * Removed until I can re-tool this.
                                 * Claymore's Monero miner
                                if (isDevFee && isDonating)
                                {
                                    redirector.m_loginBuffer = newBuffer;
                                    redirector.m_loginLength = newLength;
                                    redirector.ChangeServer(d.donatePoolAddress, d.donatePoolPort);
                                    return;
                                }
                                */
                            }
                        }
                        else
                        {
                            switch (obj.method)
                            {
                                case "submit":
                                    redirector.SubmittedShare();
                                    Logger.LogToConsole(string.Format(redirector.thisMiner.displayName + " found a share. [{0} shares found]", redirector.thisMiner.submittedShares), redirector.thisMiner.endPoint, ConsoleColor.Green);

                                    break;
                            }
                        }

                        
                        break;

                    default:
                        if (Program.settings.debug)
                        {
                            lock (Logger.ConsoleBlockLock)
                            {
                                Logger.LogToConsole("From Client >---->", redirector.thisMiner.endPoint);
                                Logger.LogToConsole("Agent: " + obj.@params.agent, redirector.thisMiner.endPoint);
                                Logger.LogToConsole("Unknown ID: " + obj.id, redirector.thisMiner.endPoint);
                                Logger.LogToConsole("Method: " + obj.method, redirector.thisMiner.endPoint);
                                Logger.LogToConsole("Result: " + obj.@params.result, redirector.thisMiner.endPoint);
                                Logger.LogToConsole("Agent: " + obj.@params.agent, redirector.thisMiner.endPoint);
                                Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint);
                            }
                        }
                        break;

                }

            }
            catch (Exception ex)
            {
                madeChanges = false;
                Logger.LogToConsole(ex.Message, redirector.thisMiner.endPoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint, ConsoleColor.Red);
            }

            if (redirector.thisMiner.connectionAlive && redirector.m_server.Disposed == false)
            {
                if (madeChanges == false)
                {
                    //Logger.LogToConsole("Sending buffer: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endpoint);
                    redirector.m_server.Send(buffer, length);
                }
                else
                {
                    //Logger.LogToConsole("Sending modified buffer: " + Encoding.UTF8.GetString(newBuffer, 0, newLength), redirector.thisMiner.endpoint);
                    redirector.m_server.Send(newBuffer, newLength);
                }
            }
        }

        internal void OnMoneroServerPacket(byte[] buffer, int length)
        {
                try
                {
                    MoneroServerRootObject obj = JsonConvert.DeserializeObject<MoneroServerRootObject>(Encoding.UTF8.GetString(buffer, 0, length));

                    switch (obj.id) {

                    case 0: //new job?
                        Logger.LogToConsole("New Job from server", redirector.thisMiner.endPoint);
                        break;

                    case 1:
                        switch (obj.method)
                        {
                            case "job":
                                Logger.LogToConsole("New Job from server", redirector.thisMiner.endPoint);
                                break;

                            default:
                                if (Program.settings.debug)
                                {
                                    lock (Logger.ConsoleBlockLock)
                                    {
                                        Logger.LogToConsole("From Server1 <----<", redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                        Logger.LogToConsole("ID: " + obj.id, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                        Logger.LogToConsole("Result ID: " + obj.result.id, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                        Logger.LogToConsole("Result Status: " + obj.result.status, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                        Logger.LogToConsole("Status: " + obj.result.status, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                        Logger.LogToConsole("JsonRPC: " + obj.jsonrpc, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                        //Logger.LogToConsole("error: " + obj.error.ToString(), redirector.thisMiner.endPoint, ConsoleColor.Gray);

                                        Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    }
                                }
                                break;
                        }
                        switch (obj.result.status)
                        {
                            case "OK":
                                Logger.LogToConsole("Server returned OK", redirector.thisMiner.endPoint);
                                break;
                        }
                        break;

                        default:
                            if (Program.settings.debug)
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole("From Server2 <----<", redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Unknown ID: " + obj.id, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Result ID: " + obj.result.id, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Result Status: " + obj.result.status, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Status: " + obj.result.status, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("JsonRPC: " + obj.jsonrpc, redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("error: " + obj.error.ToString(), redirector.thisMiner.endPoint, ConsoleColor.Gray);

                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint, ConsoleColor.Gray);
                                }
                            }
                            break;
                    }
            }
            catch (Exception ex)
            {
                Logger.LogToConsole(ex.ToString(), redirector.thisMiner.endPoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.thisMiner.endPoint, ConsoleColor.Red);
            }

            if (redirector.thisMiner.connectionAlive && redirector.m_client.Disposed == false)
                redirector.m_client.Send(buffer, length);

            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " <----<\r\n" + Encoding.UTF8.GetString(buffer, 0, length)));

        }

    }
}
