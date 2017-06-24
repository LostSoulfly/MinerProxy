using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy
{
    class MinerStats
    {
        public long submittedShares { get; set; }
        public long acceptedShares { get; set; }
        public long rejectedShares { get; set; }
        public long numberOfConnects { get; set; }
        public readonly string rigName;
        public string lastEndpoint { get; set; }
        //string wallet { get; set; }
        internal Queue<double> hashrateAverage;
        public readonly DateTime firstConnectTime;
        public DateTime totalTimeConnected;
        private int queueLimit = 60; // last x number of hashes to average, large number for smoother average

        public MinerStats(string rigName)
        {
            this.rigName = rigName;
            this.firstConnectTime = DateTime.Now;   // this is readonly, so we set this at initialization
        }

        public void AddHashrate(double hashrate)
        {
            if (hashrateAverage.Count >= queueLimit)
                hashrateAverage.Dequeue();  // if we're at our queue limit, drop off the first queued hashrate

            this.hashrateAverage.Enqueue(hashrate); // add the new hashrate
        }

        public string GetAverageHashrate()
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
            this.totalTimeConnected = this.totalTimeConnected + ts;
        }

        public string GetStats()
        {
            return "miner stats";
        }
    }
}
