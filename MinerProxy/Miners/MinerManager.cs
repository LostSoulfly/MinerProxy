using System;
using System.Collections.Generic;

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
                if (minerIndex < 0) return "NO MINER FOUND";
                return minerList[minerIndex].GetAverageHashrate();
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
