using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection.Messages
{

    public enum RequestVoteResponseType
    {
        PositiveVote,
        AlreadyVotedForCurrentTerm,
        StaleRequestVoteMessage,
        RequestedVoteToLeader,
        RequestedVoteToCandidate
    }

    public class RequestVoteRPCResponse
    {
        public string FollowerId { get;private set; }
        public RequestVoteResponseType ResponseType { get; set; }
        public long Term { get; private set; }
        public RequestVoteRPCResponse(string followerId, RequestVoteResponseType responseType, long returnedTerm)
        {
            this.FollowerId = followerId;
            this.ResponseType = responseType;
            this.Term = returnedTerm;
        }
    }
}
