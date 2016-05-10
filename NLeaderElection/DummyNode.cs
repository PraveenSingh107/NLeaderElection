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
        public long term;
        public DummyFollowerNode(IPAddress address)
            : base(address, DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {

        }

        
        public override string ToString()
        {
            return IP.ToString();
        }

        public override long GetTerm()
        {
            return term;
        }

        public override void UpdateTerm(long term)
        {
            this.term = term;
        }

        public void IncrementTerm()
        {
            this.term++;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            DummyFollowerNode otherNode = (obj as DummyFollowerNode);
            return IP.Equals(otherNode.IP);
        }

        public override int GetHashCode()
        {
            return this.IP.GetHashCode();
        }
    }
}
