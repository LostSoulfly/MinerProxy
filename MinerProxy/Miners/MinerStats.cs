using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy.Miners
{

    class MinerStats
    {
        public long submittedShares { get; set; }
        public long acceptedShares { get; set; }
        public long rejectedShares { get; set; }
        public long hashrate { get; set; }
        public string rigName { get; set; }
        public string endPoint { get; set; }
        public string replacedWallet { get; set; }
        public string connectionName { get; set; }
        public string workerName { get; set; }
        public string displayName { get; set; }
        public bool connectionAlive { get; set; }
        public bool noRigName { get; set; }
        public DateTime connectionStartTime;
        public DateTime lastCalculatedTime;
    }

    class MinerStatsFull : MinerStats
    {
        // public List<string> minerWallets;    //keep a list of all the miner's wallets, useful for DevFee wallet collection
        public long numberOfConnects { get; set; }
        public TimeSpan totalTimeConnected;
        public readonly DateTime firstConnectTime;

        private  Queue<double> hashrateAverage = new Queue<double>();

        private int queueLimit = 60; // last x number of hashes to average, large number for smoother average

        public MinerStatsFull(string displayName)
        {
            if (!string.IsNullOrEmpty(this.displayName))    //don't try to initialize again
                return;

            this.displayName = displayName;
            this.firstConnectTime = DateTime.Now;   // this is readonly, so we set this at initialization
        }

        public void AddHashrate(double hashrate)
        {
            if (hashrateAverage.Count >= queueLimit)
                hashrateAverage.Dequeue();  // if we're at our queue limit, drop off the first queued hashrate

            this.hashrateAverage.Enqueue(hashrate); // add the new hashrate
        }

        internal string GetAverageHashrate()
        {
            try
            {
                double hashrate;
                hashrate = hashrateAverage.Average();
                return hashrate.ToString("#,##0,Mh/s").Replace(",", ".");
            } catch (Exception ex)
            {
                if (Program.settings.debug) Logging.Logger.LogToConsole(string.Format("Hashrate average error: {0}", ex.Message));
                return "0 MH/s";
            }
        }

        public void AddConnectedTime(TimeSpan ts)
        {
            this.totalTimeConnected.Add(totalTimeConnected);
            Console.WriteLine("totalTimeConnected: " + totalTimeConnected.TotalSeconds);
        }
    }
}
