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
            Setup();
        }

        public Candidate(string nodeId,IPAddress ipAddress)
            : base(ipAddress, nodeId)
        {
            Setup();
        }

        private void Setup()
        {
            CurrentStateData = new NodeDataState();
            electionTimeout = new Timer(3000);
            electionTimeout.Elapsed += electionTimeoutElapsed;
            electionTimeout.Start();
        }

        public void DetachEventListerners()
        {
            if (electionTimeout != null)
            {
                electionTimeout.Elapsed -= electionTimeoutElapsed;
            }
        }

        void electionTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            if (!TryGettingConsensus())
            {
                RestartElectionTimeout();
                // Election timed out without reaching at consensus. Trying re election
                Console.WriteLine("Election timed out without reaching at consensus. Trying re election");
                SendRequestVotesToFollowers();
            }
            else
            {
                NodeRegistryCache.GetInstance().PromoteCandidateToLeader(this);
                electionTimeout.Stop();
                //Dispose();
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
                totalResponseReceivedForCurrentTerm++;
                positiveVotes = new Dictionary<String, long>();
                positiveVotes.Add(this.GetNodeId(), this.GetTerm());
                var nodeRegistry = NodeRegistryCache.GetInstance();
                foreach (Node node in nodeRegistry.Get())
                {
                    if (!node.GetNodeId().Equals(this.nodeId))
                    {
                        MessageBroker.GetInstance().CandidateSendRequestVoteAsync(node, CurrentStateData.Term);
                    }
                }
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
                totalResponseReceivedForCurrentTerm = 0;
                positiveVotes = new Dictionary<string, long>();
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
                if (!positiveVotes.ContainsKey(response.FollowerId) && this.CurrentStateData.Term == response.Term)
                {
                    positiveVotes.Add(response.FollowerId, response.Term);
                    totalResponseReceivedForCurrentTerm++;
                }
                else if (this.CurrentStateData.Term == response.Term)
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

        internal int HeartBeatSignalReceivedFromLeader(long p)
        {
            if (IsServingCurrentTerm(this.CurrentStateData.Term))
            {
                NodeRegistryCache.GetInstance().DemoteCandidateToFollower();
                return 0;
            }
            else if (IsWorkingOnStaleTerm(this.CurrentStateData.Term))
            {
                UpdateNodeLogEntries();
                return 0;
            }
            else
            {
                return -1;
               // RequestLeaderToStepDown();
            }
        }

        private void UpdateNodeLogEntries()
        {
            throw new NotImplementedException();
        }

        private bool IsServingCurrentTerm(long term)
        {
            return term.Equals(CurrentStateData.Term);
        }

        private bool IsWorkingOnStaleTerm(long term)
        {
            if (CurrentStateData.Term < term)
                return true;
            return false;
        }

        public override long GetTerm()
        {
            return this.CurrentStateData.Term;
        }

        public override void UpdateTerm(long term)
        {
            this.CurrentStateData.Term = term;
        }

        public override void IncrementTerm()
        {
            this.CurrentStateData.Term++;
        }
    }
}
