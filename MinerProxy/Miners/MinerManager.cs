using System;
using System.Collections.Generic;
using System.Linq;

namespace MinerProxy.Miners
{
    class MinerManager
    {
        public static readonly object MinerManagerLock = new object();

        public static void SetRigName(string displayName, string rigName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex <= 0) return;
            }
        }

        public static void AddNewMiner(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex >= 0) return;    //miner already exists in the list, so we can ignore it. Probably a reconnection.
                minerList.Add(new MinerStatsFull(displayName));
            }
        }

        public static void SetWorkerName(string displayName, string workerName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return;
            }
        }

        public static void SetConnectionName(string displayName, string connectionName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return;
                minerList[minerIndex].connectionName = connectionName;
            }
        }

        public static void AddHashrate(string displayName, double hashrate, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return;
                minerList[minerIndex].AddHashrate(hashrate);
            }
        }

        public static string GetAverageHashrate(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return null;
                return minerList[minerIndex].GetAverageHashrate();
            }
        }

        public static DateTime GetTotalTimeConnected(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return DateTime.MinValue;
                return minerList[minerIndex].totalTimeConnected;
            }
        }

        public static int GetNumberOfSessions(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return 0;
                return minerList[minerIndex].numberOfSessions;
            }
        }

        public static DateTime GetFirstTimeConnected(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return DateTime.MinValue;
                return minerList[minerIndex].firstConnectTime;
            }
        }

        public static bool IsConnectionAlive(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return false;
                return minerList[minerIndex].connectionAlive;
            }
        }

        public static string GetWorkerName(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return null;
                return minerList[minerIndex].workerName;
            }
        }

        public static string GetLastHashrate(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return "0 MH/s";
                return minerList[minerIndex].hashrate.ToString("#,##0,Mh/s").Replace(",", ".");
            }
        }

        public static long GetAcceptedShares(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return 0;
                return minerList[minerIndex].acceptedShares;
            }
        }

        public static long GetRejectedShares(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return 0;
                return minerList[minerIndex].submittedShares;
            }
        }

        public static long GetSubmittedShares(string displayName, List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                int minerIndex = GetMinerIndex(displayName, minerList);
                if (minerIndex < 0) return 0;
                return minerList[minerIndex].submittedShares;
            }
        }


        public static List<string> GetMiners(List<MinerStatsFull> minerList)
        {
            lock (MinerManagerLock)
            {
                List<string> miners = minerList.Select(m => m.displayName).ToList();
                return miners;
            }
        }

        private static int GetMinerIndex(string displayName, List<MinerStatsFull> minerList)
        {
            try
            {
                int index = minerList.FindIndex(MinerStatsFull => MinerStatsFull.displayName.Equals(displayName, StringComparison.Ordinal));
                return index;
            } catch
            {
                return -1;
            }
        }
    }
}
