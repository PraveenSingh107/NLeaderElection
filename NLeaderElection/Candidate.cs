using NLeaderElection.Messages;
using NLeaderElection.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NLeaderElection
{
    public class Candidate : Node, IDisposable
    {
        public static string Id { get; private set; }
        private Timer electionTimeout;
        public List<Follower> Followers { get; private set; }
        public NodeDataState CurrentData { get; private set; }
        private Dictionary<string, long> positiveVotes;

        public Candidate()
            : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {
            electionTimeout = new Timer(200);
            electionTimeout.Elapsed += electionTimeout_Elapsed;
            Followers = new List<Follower>();
        }

        void electionTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Election timed out without reaching at consensus. Trying re election
            if (!TryGettingConsensus())
            {
                electionTimeout.Stop();
                electionTimeout.Start();
                SendRequestVotesToFollowers();
            }
        }

        // TO DO
        private bool TryGettingConsensus()
        {
            return true;
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
                        MessageBroker.GetInstance().CandidateSendRequestVoteAsync(node, CurrentData.Term);
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
                if (!positiveVotes.ContainsKey(response.FollowerId))
                    positiveVotes.Add(response.FollowerId, response.Term);
                else
                    positiveVotes[response.FollowerId] = response.Term;
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
    }
}
