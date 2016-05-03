using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection.Messages
{
    public class RequestVoteRPCMessage
    {
        private long term;

        public RequestVoteRPCMessage(long currentTerm)
        {
            this.term = currentTerm;
        }

        public long GetTerm()
        {
            return term;
        }

        public bool HasMoreRecentTerm(long followerTerm)
        {
            if (this.term >= followerTerm)
                return true;
            else
                return false;
        }
    }
}
