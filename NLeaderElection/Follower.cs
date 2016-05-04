using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLeaderElection.Messages;
using System.Timers;
using System.Net;
using NLeaderElection.Messaging;

namespace NLeaderElection
{
    public class Follower : Node , IDisposable
    {
        private static Timer NetworkDiscoveryTimeout;
        private static Timer HeartBeatTimeout;
        public NodeDataState CurrentStateData { get; set; }

        public Follower()
            : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {
            SetupTimeouts();
            CurrentStateData = new NodeDataState(1);
        }

        public Follower(IPAddress address)
            : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"), address)
        {
            SetupTimeouts();
            CurrentStateData = new NodeDataState(1);
        }

        public Follower(string nodeId,IPAddress address,long term)
            : base(nodeId, address,term)
        {
            SetupTimeouts();
            CurrentStateData = new NodeDataState(term);
        }

        private void SetupTimeouts()
        {
            NetworkDiscoveryTimeout = new Timer(8000);
            HeartBeatTimeout = new Timer(500);
            NetworkDiscoveryTimeout.Elapsed += NetworkBootStrapTimeElapsed;
            HeartBeatTimeout.Elapsed += HeartBeatTimeout_Elapsed;
            NetworkDiscoveryTimeout.Start();
            //HeartBeatTimeout.Start();
        }

        public void StartTimouts()
        {
            HeartBeatTimeout.Start();
        }

        private void HeartBeatTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
            NodeRegistryCache.GetInstance().PromoteFollowerToCandidate(this);
            HeartBeatTimeout.Stop();
            HeartBeatTimeout.Close();
        }

        private void NetworkBootStrapTimeElapsed(object sender, ElapsedEventArgs e)
        {
            NetworkDiscoveryTimeout.Stop();
            NetworkDiscoveryTimeout.Elapsed -= NetworkBootStrapTimeElapsed;
            NetworkDiscoveryTimeout.Close();
            Logger.Log(string.Format("Network bootstrap timed out."));
            StartTimouts();
            Logger.Log(string.Format("Heartbeat timeout started."));
        }

        private void HeartBeatTimeout_Reset()
        {
            HeartBeatTimeout.Stop();
            HeartBeatTimeout.Start();
        }

        private void ElectionTimeout_Reset()
        {
            NetworkDiscoveryTimeout.Stop();
            NetworkDiscoveryTimeout.Start();
            Logger.Log(string.Format("{0}'s bootstrap timed out.", this.ToString()));
        }

        /// <summary>
        /// Respond to request votes from candidate. This method will have following scenarios
        /// 1.  Follower has aleady voted for current term
        /// 2.  Follower has not already voted for current term and request term is latest from candidate. Respond positive
        /// 3.  Follower has not already voted for current term but request term is older than the term of the follower.
        /// 
        /// </summary>
        /// <param name="requestVote"></param>
        /// <returns></returns>
        public RequestVoteRPCResponse RespondToRequestVoteFromCandidate(RequestVoteRPCMessage requestVote)
        {
            //Restart heartbeat as there is already a candidate. Might sujbect to check the term to verify
            //that this message is not from old candidate
            HeartBeatTimeout_Reset();
            Logger.Log(string.Format("INFO :: Received Request RPC from candidate for term {0} .",requestVote.GetTerm()));
            RequestVoteRPCResponse response;
            
            if (HasAleadyVoted())
            {
                response = new RequestVoteRPCResponse(nodeId, RequestVoteResponseType.AlreadyVotedForCurrentTerm);
            }
            else
            {
                if (requestVote.HasMoreRecentTerm(CurrentStateData.Term))
                {
                    response = new RequestVoteRPCResponse(nodeId, RequestVoteResponseType.StaleRequestVoteMessage);
                }
                else
                {
                    response = new RequestVoteRPCResponse(nodeId, RequestVoteResponseType.PositiveVote);
                }
            }
            Logger.Log(string.Format("INFO :: Sending response to Request RPC from candidate for term {0} .",requestVote.GetTerm()));
            return response;
        }

        private bool HasAleadyVoted()
        {
            return CurrentStateData.Voted;
        }

        // TO DO
        public void AppendEntryRPC()
        {

        }

        public virtual void HeartBeatSignalReceivedFromLeader(long term)
        {
<<<<<<< HEAD
            Logger.Log(string.Format("INFO :: Received the heartbeat signal from leader for term {0} .", term));
=======
            Logger.Log(string.Format("INFO :: Received the heartbeat signal from leader for term {0} .",term));
>>>>>>> 6bf46844a52162f84f1fc0547d0d0afbd24d03c2
            if (IsServingCurrentTerm(term))
            {
                HeartBeatTimeout_Reset();
            }
            else if (IsWorkingOnStaleTerm(term))
            {
                UpdateNodeLogEntries();
            }
            else
            {
                RequestLeaderToStepDown();
            }
        }

        // TO DO
        private void RequestLeaderToStepDown()
        {
         //   throw new NotImplementedException();
        }

        // TO DO
        private void UpdateNodeLogEntries()
        {
         //   throw new NotImplementedException();
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
                if (NetworkDiscoveryTimeout != null) NetworkDiscoveryTimeout.Dispose();
                if (HeartBeatTimeout != null) HeartBeatTimeout.Dispose();
            }
        }

        ~Follower()
        {
            Dispose(false);
        }

        #endregion Disposable

        public void StartUp()
        {
            List<DummyFollowerNode> nodes = NodeRegistryCache.GetInstance().Get();

            foreach (var node in nodes)
            {
                MessageBroker.GetInstance().SendNodeStartupNotification(node); 
            }
        }

        public override string ToString()
        {
            return "Follower :: IP : " + this.IP.ToString() + ", ID : " + this.nodeId.ToString();
        }

        internal IPAddress GetIP()
        {
            return IP;
        }
    }
}
