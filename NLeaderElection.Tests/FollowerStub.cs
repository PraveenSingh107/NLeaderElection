using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection.Tests
{
    class FollowerStub : Follower
    {
        private int heartBeatTimeoutCounts;

        public FollowerStub()
        {
            heartBeatTimeoutCounts = 0;
        }

        public int GetHeartbeatSingalCounts()
        {
            return heartBeatTimeoutCounts;
        }

        public override void HeartBeatSignalFromLeader(long term)
        {
            heartBeatTimeoutCounts++;
        }
 
    }
}
