using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy.JsonProtocols
{
    class SiaProtocol
    {
        public class SiaServerJsonObject
        {
            public int id { get; set; }
            public string method { get; set; }
            public List<string> @params { get; set; }
        }
    }
}
