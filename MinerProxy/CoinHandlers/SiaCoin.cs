using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinerProxy.Logging;
using MinerProxy.Network;

namespace MinerProxy.CoinHandlers
{
    class SiaCoin
    {
        internal void ChangeRigName(Redirector redirector)
        {
            redirector.m_rigName = "test";
        }
        
    }
}
