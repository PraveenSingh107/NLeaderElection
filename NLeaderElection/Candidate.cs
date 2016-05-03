using NLeaderElection.Messages;
using NLeaderElection.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NLeaderElection
{
    public class Candidate : Node, IDisposable
    {
        private Timer electionTimeout;
        public NodeDataState CurrentStateData { get;  set; }
        private Dictionary<string, long> positiveVotes;
        private int totalResponseReceivedForCurrentTerm = 0;

        public Candidate() : this(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {}

        public Candidate(string nodeId)
            : base(nodeId)
        {
            electionTimeout = new Timer(200);
            electionTimeout.Elapsed += electionTimeout_Elapsed;
            electionTimeout.Start();
        }

        public Candidate(string nodeId,IPAddress ipAddress,long termPassed)
            : base(nodeId,ipAddress,termPassed)
        {
            electionTimeout = new Timer(200);
            electionTimeout.Elapsed += electionTimeout_Elapsed;
            electionTimeout.Start();
        }

        void electionTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Election timed out without reaching at consensus. Trying re election
            if (!TryGettingConsensus())
            {
                electionTimeout.Stop();
                electionTimeout.Start();
                SendRequestVotesToFollowers();
                totalResponseReceivedForCurrentTerm = 0;
            }
            else
            {
                NodeRegistryCache.GetInstance().PromoteCandidateToLeader(this);
                electionTimeout.Stop();
                Dispose();
            }
        }

        private bool TryGettingConsensus()
        {
            if (totalResponseReceivedForCurrentTerm != 0 && positiveVotes != null)
            {
                if ((positiveVotes.Count * 100) / totalResponseReceivedForCurrentTerm >= 50)
                    return true;
            }
            return false;
        }

        public void SendRequestVotesToFollowers()
        {
            try
            {
                positiveVotes = new Dictionary<String, long>();
                var nodeRegistry = NodeRegistryCache.GetInstance();
                foreach (Node node in nodeRegistry.Get())
                {
                    if (!node.GetNodeId().Equals(this.nodeId))
                    {
                        MessageBroker.GetInstance().CandidateSendRequestVoteAsync(node, CurrentStateData.Term);
                    }
                }
                RestartElectionTimeout();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void RestartElectionTimeout()
        {
            if (electionTimeout != null)
            {
                electionTimeout.Stop();
                electionTimeout.Start();
            }
        }

        public Leader BecomeALeader()
        {
            return new Leader();
        }

        # region Disposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (electionTimeout != null) electionTimeout.Dispose();
            }
        }

        ~Candidate()
        {
            Dispose(false);
        }

        #endregion Disposable

        public void ResponseCallbackFromFollower(RequestVoteRPCResponse response)
        {
            if (positiveVotes != null)
            {
                if (!positiveVotes.ContainsKey(response.FollowerId) && this.term == response.Term)
                {
                    positiveVotes.Add(response.FollowerId, response.Term);
                    totalResponseReceivedForCurrentTerm++;
                }
                else if (this.term == response.Term)
                {
                    positiveVotes[response.FollowerId] = response.Term;
                    totalResponseReceivedForCurrentTerm++;
                }
            }
        }

        internal void ResponseCallbackFromFollower(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                var responseParts = content.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                if (responseParts != null && responseParts.Count() >= 2)
                {
                    RequestVoteRPCResponse response = new RequestVoteRPCResponse(responseParts[0], getRequestRPCResponse(responseParts[1]));
                }
                else
                {
                    // TO DO wrong response from follower.
                }
             }
        }

        private RequestVoteResponseType getRequestRPCResponse(string type)
        {
            if (type.Equals("PositiveVote"))
                return RequestVoteResponseType.PositiveVote;
            else if (type.Equals("AlreadyVotedForCurrentTerm"))
                return RequestVoteResponseType.AlreadyVotedForCurrentTerm;
            else
                return RequestVoteResponseType.StaleRequestVoteMessage;
        }

        internal IPAddress GetIP()
        {
            return IP;
        }
    }
}
