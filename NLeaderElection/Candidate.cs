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
        private object lockerObject = new object();

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
                electionTimeout.Stop();
                electionTimeout.Close();
            }
        }

        private void electionTimeoutElapsed(object sender, ElapsedEventArgs e)
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
                Logger.Log("INFO (C) :: Quorum won.");
                lock (lockerObject)
                {
                    if (NodeRegistryCache.GetInstance().CurrentNode != null && NodeRegistryCache.GetInstance().CurrentNode is Candidate)
                    {
                        DetachEventListerners();
                        NodeRegistryCache.GetInstance().PromoteCandidateToLeader(this);
                        electionTimeout.Stop();
                    }
                }
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
            catch (Exception exp)
            {
                Logger.Log(exp);
            }
        }

        private void RestartElectionTimeout()
        {
            if (electionTimeout != null)
            {
                Logger.Log("INFO (C) :: Election timed out. Restarting the elections.");
                electionTimeout.Stop();
                electionTimeout.Start();
                totalResponseReceivedForCurrentTerm = 0;
                positiveVotes = new Dictionary<string, long>();
            }
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
                if (!positiveVotes.ContainsKey(response.FollowerId) && this.CurrentStateData.Term == response.Term 
                    && response.ResponseType == RequestVoteResponseType.PositiveVote)
                {
                    positiveVotes.Add(response.FollowerId, response.Term);
                    totalResponseReceivedForCurrentTerm++;
                }
                else if (response.ResponseType == RequestVoteResponseType.RequestedVoteToLeader && this.CurrentStateData.Term == response.Term)
                {
                    Logger.Log("INFO (C) :: One leader already exist. Stepping down.");
                    DetachEventListerners();
                    DecrementTermOnDemotion();
                    NodeRegistryCache.GetInstance().DemoteCandidateToFollower();
                }
                else if (response.ResponseType == RequestVoteResponseType.RequestedVoteToCandidate && this.CurrentStateData.Term == response.Term)
                {
                    totalResponseReceivedForCurrentTerm++;
                }
                else if (this.CurrentStateData.Term == response.Term)
                {
                    //positiveVotes[response.FollowerId] = response.Term;
                    totalResponseReceivedForCurrentTerm++;
                }
            }
        }

        internal void ResponseCallbackFromFollower(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                var responseParts = content.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                if (responseParts != null && responseParts.Count() >= 3)
                {
                    RequestVoteRPCResponse response = new RequestVoteRPCResponse(responseParts[2], GetRequestRPCResponse(responseParts[0]),
                        Convert.ToInt64(responseParts[1]));
                    ResponseCallbackFromFollower(response);
                }
                else
                {
                    // TO DO wrong response from follower.
                }
             }
        }

        private RequestVoteResponseType GetRequestRPCResponse(string type)
        {
            if (type.Equals("PositiveVote"))
                return RequestVoteResponseType.PositiveVote;
            else if (type.Equals("AlreadyVotedForCurrentTerm"))
                return RequestVoteResponseType.AlreadyVotedForCurrentTerm;
            else if (type.Equals("StaleRequestVoteMessage"))
                return RequestVoteResponseType.StaleRequestVoteMessage;
            else
                return RequestVoteResponseType.RequestedVoteToLeader;
        }

        internal IPAddress GetIP()
        {
            return IP;
        }

        internal int HeartBeatSignalReceivedFromLeader(long termedPassed)
        {
            if (IsServingCurrentTerm(termedPassed))
            {
                lock(lockerObject)
                if (NodeRegistryCache.GetInstance().CurrentNode != null && NodeRegistryCache.GetInstance().CurrentNode is Candidate)
                {
                    DetachEventListerners();
                    DecrementTermOnDemotion();
                    NodeRegistryCache.GetInstance().DemoteCandidateToFollower();
                    return 0;
                }
                else return -1;
            }
            else if (IsWorkingOnStaleTerm(termedPassed))
            {
                UpdateNodeLogEntries(termedPassed);
                return 0;
            }
            else
            {
                return -1;
               // RequestLeaderToStepDown();
            }
        }

        private void DecrementTermOnDemotion()
        {
            if (this.CurrentStateData != null)
                -- this.CurrentStateData.Term;
        }

        private void UpdateNodeLogEntries(long term)
        {
            this.CurrentStateData.Term = term;
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

        public void IncrementTerm()
        {
            this.CurrentStateData.Term++;
        }

        internal long GetPreviousTerm()
        {
            return this.CurrentStateData.Term--;
        }
    }
}
