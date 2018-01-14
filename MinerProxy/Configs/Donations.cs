using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy
{
    class Donations
    {
        public class DonateList
        {
            public string donateWallet;
            public string donatePoolAddress;
            public string developer;
            public int donatePoolPort;
            public bool isEmpty;

            public DonateList(string poolAddress, int poolPort, string walletAddress, string developer)
            {
                this.donatePoolAddress = poolAddress;
                this.donatePoolPort = poolPort;
                this.donateWallet = walletAddress;
                this.developer = developer;
                isEmpty = false;
            }

            public DonateList() { isEmpty = true; }
        }

        public static List<DonateList> ethDonateList = new List<DonateList>();
        public static List<DonateList> xmrDonateList = new List<DonateList>();
        public static List<DonateList> etcDonateList = new List<DonateList>();
        public static List<DonateList> ubqDonateList = new List<DonateList>();
        public static List<DonateList> expDonateList = new List<DonateList>();
        public static List<DonateList> zecDonateList = new List<DonateList>();

        public static void SetUpDonateLists()
        {
            //LostSoulfly
            ethDonateList.Add(new DonateList("us1.ethermine.org", 4444, "0x3Ff3CF71689C7f2f8F5c1b7Fc41e030009ff7332.Donation", "LostSoulfly"));
            etcDonateList.Add(new DonateList("us1-etc.ethermine.org", 4444, "0x83d557a1e88c9e3bbae51dfa7bd12cf523b28b84.Donation", "LostSoulfly"));
            xmrDonateList.Add(new DonateList("pool.usxmrpool.com", 3333, "43tVLRGvcaadfw4HrkUcpEKmZd9Y841rGKvsLZW8XvEVSBX1GrGezWvQYDdoNwNHAwTqSyK7iqyyqMSpDoUVKQmM43nzT72", "LostSoulfly"));
            ubqDonateList.Add(new DonateList("ubq.pool.sexy", 9009, "0x0c0ff71b06413865fe9fE9a4C40396c136a62980", "LostSoulfly"));
            expDonateList.Add(new DonateList("exp.digger.ws", 7008, "0x4412f6f92616fB20B9c4E57414F20e5357E2d776.Donation", "LostSoulfly"));
            //zecDonateList.Add(new DonateList("mining.miningspeed.com", 3092, "t1ZHrvmtgd3129iYEcFm21XMv5ojdh2xmsf.Donation", "LostSoulFly"));

            //Samut
            ethDonateList.Add(new DonateList("us1.ethermine.org", 4444, "0xcddb36acb8c9fba074bf824edfede05d3a3ec221.Donation", "samut3"));
            xmrDonateList.Add(new DonateList("pool.usxmrpool.com", 3333, "41p63nnxZyJCbu7m7Nj1uAhRGj9KdsK2hikGMgtxgMAf7AcaX4Me8cnMfPAR3rYqc5WEnZ2KYYM8J6QGKnLkKgwxU4KCGd9", "samut3"));
            ubqDonateList.Add(new DonateList("ubq.pool.sexy", 9009, "0xF22743C0488fdc6722210714c3Ad1ACceA159B73", "samut3"));
        }
        
        private static bool CalculateDonate()
        {
            int percent = Program.settings.percentToDonate;
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            double chance = rnd.Next(0, 100);

            if (chance <= percent)
            {
                if (Program.settings.debug) Logging.Logger.LogToConsole(string.Format("MinerProxy donate roll success: {0}. Needed: <={1}.", chance, percent), "Donation");
                return true;
            }
            else
            {
                if (Program.settings.debug) Logging.Logger.LogToConsole(string.Format("MinerProxy donate roll failed: {0}. Needed: <={1}.", chance, percent), "Donation");
                return false;
            }
        }

        public static bool CheckForDonation(out DonateList donation, string coin = "")
        {
            donation = new DonateList();
            if (!Program.settings.donateDevFee | !CalculateDonate())  //If not donating or % chance wasn't in favor of donation, just return false
                return false;

            if (coin.Length == 0)
                coin = Program.settings.minedCoin;

            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            switch (coin)
            {
                case "ETC":
                    donation = etcDonateList[rnd.Next(etcDonateList.Count)];
                    break;

                case "ETH":
                    donation = ethDonateList[rnd.Next(ethDonateList.Count)];
                    break;

                case "EXP":
                    donation = expDonateList[rnd.Next(expDonateList.Count)];
                    break;

                case "UBIQ":
                case "UBQ":
                    donation = ubqDonateList[rnd.Next(ubqDonateList.Count)];
                    break;
                /*
                case "XMR":
                    donation = xmrDonateList[rnd.Next(xmrDonateList.Count)];
                    break;
                */
                default:
                    return false;
            }

            return true;

        }

    }
}
