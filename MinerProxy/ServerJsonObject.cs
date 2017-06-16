using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy
{
    public class ServerRootObject
    {
        public int id { get; set; }
        public string jsonrpc { get; set; }
        public List<string> result { get; set; }
    }

    public class ServerRootObjectBool
    {
        public int id { get; set; }
        public string jsonrpc { get; set; }
        public bool result { get; set; }
        public string error { get; set; }
    }
}
