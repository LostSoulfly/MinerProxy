using System.Collections.Generic;

namespace MinerProxy
{
    public class ClientRootObject
    {
        public string worker { get; set; }
        public string jsonrpc { get; set; }
        public List<string> @params { get; set; }
        public int? id { get; set; }
        public string method { get; set; }
    }
}
