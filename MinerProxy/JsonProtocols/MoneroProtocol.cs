using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinerProxy.JsonProtocols
{

    public class MoneroClientParams
    {
        public string login { get; set; }
        public string pass { get; set; }
        public string agent { get; set; }
        public string id { get; set; }
        public string job_id { get; set; }
        public string nonce { get; set; }
        public string result { get; set; }
    }

    public class MoneroClientRootObject
    {
        public string method { get; set; }
        public MoneroClientParams @params { get; set; }
        public int id { get; set; }
    }

    public class MoneroServerJob
    {
        public string blob { get; set; }
        public string job_id { get; set; }
        public string target { get; set; }
    }

    public class MoneroServerResult
    {
        public string id { get; set; }
        public MoneroServerJob job { get; set; }
        public string status { get; set; }
    }

    public class MoneroServerRootObject
    {
        public int id { get; set; }
        public string jsonrpc { get; set; }
        public object error { get; set; }
        public string method { get; set; }
        public MoneroServerResult result { get; set; }
    }
}
