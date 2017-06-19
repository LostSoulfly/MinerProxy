using System;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Timers;
using MinerProxy.CoinHandlers;
using MinerProxy.JsonProtocols;

namespace MinerProxy.CoinHandlers
{
    class EthCoin
    {
        internal Redirector redirector;

        public EthCoin(Redirector r)
        {
            if (Program.settings.debug) Logger.LogToConsole("EthCoin handler initialized");
            redirector = r; //when this class is initialized, a reference to the Redirector class must be passed
        }

        internal void OnEthClientPacket(byte[] buffer, int length)
        {
            bool madeChanges = false;
            byte[] newBuffer = null;
            int newLength = 0;

            try   //try to deserialize the packet, if it's not Json it will fail. that's ok.
            {

                EthClientRootObject obj;

                obj = JsonConvert.DeserializeObject<EthClientRootObject>(Encoding.UTF8.GetString(buffer, 0, length));
                switch (obj.id)
                {
                    case 2: //eth_submitLogin
                        Logger.LogToConsole("Ethereum Login detected!", redirector.m_endpoint, ConsoleColor.DarkGreen);
                        madeChanges = true;
                        if (obj.@params[0].Contains(".") && Program.settings.useDotWithRigName)
                        {//There is likely a rigName in the wallet address.

                            redirector.m_replacedWallet = obj.@params[0];
                            redirector.m_rigName = obj.@params[0].Substring(obj.@params[0].IndexOf(".") + 1);
                            redirector.m_displayName = redirector.m_rigName;
                            if (Program.settings.replaceWallet) obj.@params[0] = Program.settings.walletAddress + "." + redirector.m_rigName;
                        }
                        else if (obj.@params[0].Contains("/") && Program.settings.useSlashWithRigName)
                        {//There is likely different rigname, may need to check for email addresses here as well
                            redirector.m_replacedWallet = obj.@params[0];
                            redirector.m_rigName = obj.@params[0].Substring(obj.@params[0].IndexOf("/") + 1);
                            redirector.m_displayName = redirector.m_rigName;
                            if (Program.settings.replaceWallet) obj.@params[0] = Program.settings.walletAddress + "/" + redirector.m_rigName;
                        }
                        else if (Program.settings.identifyDevFee)
                        { //there is no rigName, so we just replace the wallet
                            redirector.m_replacedWallet = obj.@params[0];

                            if (redirector.m_replacedWallet != Program.settings.walletAddress)
                                redirector.m_displayName = "DevFee";

                            if (obj.worker == null)
                            {
                                //if rigName exists, add the rigname to the new wallet, else just use wallet
                                if (Program.settings.replaceWallet) obj.@params[0] = Program.settings.walletAddress;
                                redirector.m_noRigName = true;
                            }
                            else if (obj.worker.Equals("eth1.0"))
                            { //It's probably a DevFee

                                if (redirector.m_replacedWallet != Program.settings.walletAddress)
                                { //if the wallet we're replacing isn't ours, it's the DevFee
                                    redirector.m_displayName = "DevFee";
                                    if (Program.settings.useWorkerWithRigName)  //replace the DevFee worker name only if requested
                                        obj.worker = "DevFee";
                                    if (Program.settings.useSlashWithRigName && Program.settings.replaceWallet)
                                        obj.@params[0] = Program.settings.walletAddress + "/" + redirector.m_displayName;
                                    if (Program.settings.useDotWithRigName && Program.settings.replaceWallet)
                                        obj.@params[0] = Program.settings.walletAddress + "." + redirector.m_displayName;
                                }
                                else
                                {
                                    redirector.m_noRigName = true;
                                    redirector.m_displayName = redirector.m_name;
                                    if (Program.settings.replaceWallet) obj.@params[0] = Program.settings.walletAddress;
                                }
  
                            }
                            else
                            {
                                if (Program.settings.useWorkerWithRigName)
                                {
                                    redirector.m_displayName = obj.worker;
                                    redirector.m_workerName = obj.worker;
                                }
                                if (Program.settings.replaceWallet) obj.@params[0] = Program.settings.walletAddress;
                                if (Program.settings.debug) Logger.LogToConsole(string.Format("Worker: {0}", redirector.m_workerName), redirector.m_endpoint, ConsoleColor.DarkGreen);
                            }
                        }
                        else
                        { //Don't worry about rigName, just replace the wallet.
                            redirector.m_replacedWallet = obj.@params[0];
                            if (obj.worker != null) redirector.m_displayName = obj.worker;
                            if (Program.settings.replaceWallet) obj.@params[0] = Program.settings.walletAddress;
                        }

                        string tempBuffer = JsonConvert.SerializeObject(obj, Formatting.None) + "\n";

                        if (Program.settings.replaceWallet)
                        {
                            lock (Logger.ConsoleBlockLock)
                            {
                                Logger.LogToConsole("Old Wallet: " + redirector.m_replacedWallet, redirector.m_endpoint, ConsoleColor.Yellow);
                                Logger.LogToConsole("New Wallet: " + obj.@params[0], redirector.m_endpoint, ConsoleColor.Yellow);
                            }
                        } 
                        else
                        {
                            Logger.LogToConsole(string.Format("Wallet for {0}: {1}", redirector.m_displayName, obj.@params[0]));
                        }

                        newBuffer = Encoding.UTF8.GetBytes(tempBuffer);
                        newLength = tempBuffer.Length;

                        break;

                    case 3: //eth_getWork
                        if (Program.settings.debug) Logger.LogToConsole("eth_getWork from Client.", redirector.m_endpoint);
                        break;

                    case 4: //eth_submitWork
                        redirector.m_submittedShares++;
                        Logger.LogToConsole(string.Format(redirector.m_displayName + " found a share. [{0} shares found]", redirector.m_submittedShares), redirector.m_endpoint, ConsoleColor.Green);
                        break;

                    case 6: //eth_submitHashrate
                        long hashrate = Convert.ToInt64(obj.@params[0], 16);
                        redirector.m_hashRate = hashrate;
                        if (Program.settings.debug)
                        {
                            Logger.LogToConsole(string.Format("Hashrate reported by {0}: {1}", redirector.m_displayName, hashrate.ToString("#,##0,Mh/s").Replace(",", ".")), redirector.m_endpoint, ConsoleColor.Magenta);
                        }
                        break;

                    default:
                        if (Program.settings.debug)
                        {
                            lock (Logger.ConsoleBlockLock)
                            {
                                Logger.LogToConsole("From Client >---->", redirector.m_endpoint);
                                Logger.LogToConsole("Unknown ID: " + obj.id, redirector.m_endpoint);
                                Logger.LogToConsole("Method: " + obj.method, redirector.m_endpoint);
                                Logger.LogToConsole("Param Count: " + obj.@params.Count, redirector.m_endpoint);
                                Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint);
                            }
                        }
                        break;

                }

            }
            catch (Exception ex)
            {
                madeChanges = false;
                Logger.LogToConsole(ex.Message, redirector.m_endpoint);
                if (Program.settings.debug) Logger.LogToConsole("Json Err: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint, ConsoleColor.Red);
            }

            if (redirector.m_alive && redirector.m_server.Disposed == false)
            {
                if (madeChanges == false)
                {
                    //Logger.LogToConsole("Sending buffer: " + Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint);
                    redirector.m_server.Send(buffer, length);
                }
                else
                {
                    //Logger.LogToConsole("Sending modified buffer: " + Encoding.UTF8.GetString(newBuffer, 0, newLength), redirector.m_endpoint);
                    redirector.m_server.Send(newBuffer, newLength);
                }
            }
        }

        internal void OnEthServerPacket(byte[] buffer, int length)
        {
            
            try     // try to deserialize the packet. I didn't want to write a manual deserialization, so this abomination works fine.
                    // yes, it's ugly and I'm sorry.
            {
                try
                {
                    EthServerRootObject obj = JsonConvert.DeserializeObject<EthServerRootObject>(Encoding.UTF8.GetString(buffer, 0, length));

                    switch (obj.id)
                    {

                        case 0:
                            break;

                        case 3:
                            if (Program.settings.debug)
                                Logger.LogToConsole("eth_getWork from server.", redirector.m_endpoint);
                            break;

                        case 4:
                            Logger.LogToConsole("Share accepted?", redirector.m_endpoint, ConsoleColor.Green);
                            break;

                        default:
                            if (Program.settings.debug)
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole("From Server1 <----<", redirector.m_endpoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Unknown ID: " + obj.id, redirector.m_endpoint, ConsoleColor.Gray);
                                    Logger.LogToConsole("Param Count: " + obj.result.Count, redirector.m_endpoint, ConsoleColor.Gray);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint, ConsoleColor.Gray);
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        EthServerRootObjectBool obj = JsonConvert.DeserializeObject<EthServerRootObjectBool>(Encoding.UTF8.GetString(buffer, 0, length));

                        if ((obj.error != null) && obj.result.Equals(null))
                        {
                            Logger.LogToConsole(string.Format(("Server error for {0}: {1} {2}"), redirector.m_displayName, obj.error.code, obj.error.message), redirector.m_endpoint, ConsoleColor.Red);
                        }
                        else if (!obj.result.Equals(null))
                        {
                            if (obj.result == false)
                            {
                                Logger.LogToConsole(string.Format(("Server error for {0}: {1} {2}"), redirector.m_displayName, obj.error.code, obj.error.message), redirector.m_endpoint, ConsoleColor.Red);
                            }
                        }

                        switch (obj.id)
                        {
                            case 2:
                                if (obj.result == true)
                                {
                                    Logger.LogToConsole("Stratum Authorization success: " + redirector.m_displayName, redirector.m_endpoint, ConsoleColor.DarkGreen);
                                }
                                else
                                {
                                    Logger.LogToConsole("eth_SubmitLogin failed!", redirector.m_endpoint, ConsoleColor.Red);
                                }
                                break;

                            case 4:
                                if (obj.result == true)
                                {
                                    redirector.m_acceptedShares++;

                                    Logger.LogToConsole(string.Format(redirector.m_displayName + "'s share got accepted. [{0} shares accepted]", redirector.m_acceptedShares), redirector.m_endpoint, ConsoleColor.Green);

                                }
                                else if (obj.result == false)
                                {
                                    redirector.m_rejectedShares++;
                                    Logger.LogToConsole(string.Format(redirector.m_displayName + "'s share got rejected. [{0} shares rejected]", redirector.m_acceptedShares), redirector.m_endpoint, ConsoleColor.Red);
                                }
                                break;

                            case 6:
                                if (Program.settings.debug) Logger.LogToConsole(string.Format("Hashrate accepted: {0}", obj.result), redirector.m_endpoint, ConsoleColor.DarkGreen);
                                break;

                            default:
                                if (Program.settings.debug)
                                {
                                    lock (Logger.ConsoleBlockLock)
                                    {
                                        Logger.LogToConsole("From Server2 <----<", redirector.m_endpoint);
                                        Logger.LogToConsole("Unknown ID: " + obj.id, redirector.m_endpoint);
                                        Logger.LogToConsole("Result: " + obj.result, redirector.m_endpoint);
                                        Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint);
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception ex2)
                    {
                        try
                        {
                            EthServerRootObjectError obj = JsonConvert.DeserializeObject<EthServerRootObjectError>(Encoding.UTF8.GetString(buffer, 0, length));

                            if (obj.error != null && obj.error.Length > 0)
                            {
                                if (obj.result == false)
                                    Logger.LogToConsole(string.Format(("Server error for {0}: {1}"), redirector.m_displayName, obj.error), redirector.m_endpoint, ConsoleColor.Red);
                            }
                            else
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole(ex2.ToString(), redirector.m_endpoint);
                                    Logger.LogToConsole("From Server3 <----<", redirector.m_endpoint);
                                    Logger.LogToConsole("ID: " + obj.id, redirector.m_endpoint);
                                    Logger.LogToConsole("Result: " + obj.result, redirector.m_endpoint);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint);
                                }
                            }
                        }
                        catch (Exception ex3)
                        {
                            if (Program.settings.debug)
                            {
                                lock (Logger.ConsoleBlockLock)
                                {
                                    Logger.LogToConsole(ex3.ToString(), redirector.m_endpoint);
                                    Logger.LogToConsole(Encoding.UTF8.GetString(buffer, 0, length), redirector.m_endpoint);
                                }
                            }
                        }
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

            if (Program.settings.log)
                Program._logMessages.Add(new LogMessage(Logger.logFileName + ".txt", DateTime.Now.ToLongTimeString() + " <----<\r\n" + Encoding.UTF8.GetString(buffer, 0, length)));

        }

    }
}
