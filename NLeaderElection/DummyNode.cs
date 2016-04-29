using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    public class DummyFollowerNode : Node
    {
        public DummyFollowerNode(IPAddress address)
            : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"), address)
        {

        }

        
        public override string ToString()
        {
            return IP.ToString();
        }
    }
}
