using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
namespace MinerProxy
{
    class LogMessage
    {
        public string Filepath;
        public string Text;

        public LogMessage(string v1, string v2)
        {
            this.Filepath = v1;
            this.Text = v2;
        }

    }


}
