using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy
{
    public class Settings
    {
        public int localPort { get; set; }
        public string remoteHost { get; set; }
        public int remotePort { get; set; }
        public bool log { get; set; }
        public bool debug { get; set; }
        public bool identifyDevFee { get; set; }
        public string walletAddress { get; set; }
        public List<string> allowedAddresses = new List<string>();
    }
}
