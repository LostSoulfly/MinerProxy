using System;
using System.Collections.Generic;
using System.Linq;

namespace MinerProxy.Miners
{
    class MinerManager
    {
        public static readonly object MinerManagerLock = new object();
        private object test = Program._minerStats;

        public static void AddNewMiner(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex >= 0) return;    //miner already exists in the list, so we can ignore it. Probably a reconnection.
                Program._minerStats.Add(new MinerStatsFull(displayName));
            }
        }

        public static void SetRigName(string displayName, string rigName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex <= 0) return;
            }
        }
        
        public static void SetWorkerName(string displayName, string workerName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
            } 
        }

        public static void SetConnectionName(string displayName, string connectionName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].connectionName = connectionName;
            }
        }

        public static void SetEndpoint(string displayName, string endpoint)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].endPoint = endpoint;
            }
        }

        public static void AddMinerWallet(string displayName, string wallet)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;

                if (!Program._minerStats[minerIndex].minerWallets.Contains(wallet))
                    Program._minerStats[minerIndex].minerWallets.Add(wallet);
            }
        }

        public static List<string> GetMinerWallets(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return null;

                return Program._minerStats[minerIndex].minerWallets.ToList();
            }
        }

        public static void AddHashrate(string displayName, long hashrate, bool setHashrate = true)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].AddHashrate(hashrate);
                if (setHashrate) Program._minerStats[minerIndex].hashrate = hashrate;
            }
        }

        public static void SetHashrate(string displayName, long hashrate)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].hashrate = hashrate;
            }
        }

        public static string GetAverageHashrate(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return null;
                return Program._minerStats[minerIndex].GetAverageHashrate();
            }
        }

        public static long GetNumberOfSessions(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return 0;
                return Program._minerStats[minerIndex].numberOfConnects;
            }
        }

        public static DateTime GetFirstTimeConnected(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return DateTime.MinValue;
                return Program._minerStats[minerIndex].firstConnectTime;
            }
        }

        public static void SetConnectionStartTime(string displayName, DateTime d)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                 Program._minerStats[minerIndex].connectionStartTime = d;
            }
        }


        public static TimeSpan GetTotalTimeConnected(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return TimeSpan.MinValue;
                return Program._minerStats[minerIndex].totalTimeConnected;
            }
        }

        public static void AddConnectionCount(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].numberOfConnects++;
            }
        }

        public static void AddConnectedTime(string displayName, TimeSpan t)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].AddConnectedTime(t);
            }
        }

        public static bool IsConnectionAlive(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return false;
                return Program._minerStats[minerIndex].connectionAlive;
            }
        }

        public static void SetConnectionAlive(string displayName, bool alive)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].connectionAlive = alive;
            }
        }

        public static string GetWorkerName(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return null;
                return Program._minerStats[minerIndex].workerName;
            }
        }

        public static string GetLastHashrate(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return "0 MH/s";
                return Program._minerStats[minerIndex].hashrate.ToString("#,##0,Mh/s").Replace(",", ".");
            }
        }

        public static long GetAcceptedShares(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return 0;
                return Program._minerStats[minerIndex].acceptedShares;
            }
        }

        public static long GetRejectedShares(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return 0;
                return Program._minerStats[minerIndex].rejectedShares;
            }
        }

        public static long GetSubmittedShares(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return 0;
                return Program._minerStats[minerIndex].submittedShares;
            }
        }

        public static void AddAcceptedShare(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].acceptedShares++;
            }
        }

        public static void AddRejectedShare(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].rejectedShares++;
            }
        }

        public static void AddSubmittedShare(string displayName)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName);
                if (minerIndex < 0) return;
                Program._minerStats[minerIndex].submittedShares++;
            }
        }


        public static List<string> GetMiners()    // simply returns a list of all miner's displayNames
        {
            lock (MinerManagerLock)
            {
                List<string> miners = Program._minerStats.Select(m => m.displayName).ToList();
                return miners;
            }
        }

        private static int GetMinerIndex(string displayName)
        {
            try
            {
                int index = Program._minerStats.FindIndex(MinerStatsFull => MinerStatsFull.displayName.Equals(displayName, StringComparison.Ordinal));
                return index;
            } catch
            {
                return -1;
            }
        }
    }
}
