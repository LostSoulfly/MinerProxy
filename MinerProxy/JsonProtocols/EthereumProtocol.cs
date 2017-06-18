using System.Collections.Generic;

namespace MinerProxy.JsonProtocols
{
    public class EthServerRootObject
    {
        public int id { get; set; }
        public string jsonrpc { get; set; }
        public List<string> result { get; set; }
    }

    public class EthError
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    public class EthServerRootObjectBool
    {
        public int? id { get; set; }
        public string jsonrpc { get; set; }
        public bool? result { get; set; }
        public EthError error { get; set; }
    }

    public class EthServerRootObjectError
    {
        public int? id { get; set; }
        public string jsonrpc { get; set; }
        public bool? result { get; set; }
        public string error { get; set; }
    }

    public class EthClientRootObject
    {
        public string worker { get; set; }
        public string jsonrpc { get; set; }
        public List<string> @params { get; set; }
        public int? id { get; set; }
        public string method { get; set; }
    }
}
