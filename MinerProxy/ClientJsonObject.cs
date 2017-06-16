using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy
{
    public class ClientRootObject
    {
        public string worker { get; set; }
        public string jsonrpc { get; set; }
        public List<string> @params { get; set; }
        public int id { get; set; }
        public string method { get; set; }
    }
}
