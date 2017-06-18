using System.Collections.Generic;

namespace MinerProxy
{
    public class ServerRootObject
    {
        public int id { get; set; }
        public string jsonrpc { get; set; }
        public List<string> result { get; set; }
    }

    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    public class ServerRootObjectBool
    {
        public int? id { get; set; }
        public string jsonrpc { get; set; }
        public bool? result { get; set; }
        public Error error { get; set; }
    }

    public class ServerRootObjectError
    {
        public int? id { get; set; }
        public string jsonrpc { get; set; }
        public bool? result { get; set; }
        public string error { get; set; }
    }
}
