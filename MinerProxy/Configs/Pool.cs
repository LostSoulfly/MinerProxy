using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy.Configs
{

    public class PoolItem
    {
        public string poolAddress { get; set; }
        public int poolPort { get; set; }

        public PoolItem(string host, int port)
        {
            this.poolAddress = host;
            this.poolPort = port;
        }

        
    }
}
