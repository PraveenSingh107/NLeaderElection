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

        public Follower() : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {
            SetupTimeouts();
            CurrentStateData.Term  = 0;
        }

        public Follower(IPAddress address) : base(address,DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {
            SetupTimeouts();
            CurrentStateData.Term = 0;
        }

        public Follower(string nodeId, IPAddress address)
            : base(address,nodeId)
        {
            SetupTimeouts();
        }

        private void SetupTimeouts()
        {
            HeartBeatTimeout = new Timer(2000);
            HeartBeatTimeout.Elapsed += HeartBeatTimeoutElapsed;
            CurrentStateData = new NodeDataState();
        }

        public void DetachEventListeners()
        {
            if (HeartBeatTimeout != null)
                HeartBeatTimeout.Elapsed -= HeartBeatTimeoutElapsed;
        }

        public void StartNetworkBootstrap()
        {
            NetworkDiscoveryTimeout = new Timer(8000);
            NetworkDiscoveryTimeout.Elapsed += NetworkBootStrapTimeElapsed;
            NetworkDiscoveryTimeout.Start();
        }

        public void StartHeartbeatTimouts()
        {
            HeartBeatTimeout.Start();
        }

        private void HeartBeatTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            DetachEventListeners();
            NodeRegistryCache.GetInstance().PromoteFollowerToCandidate(this);
            HeartBeatTimeout.Stop();
            HeartBeatTimeout.Close();
        }

        private void NetworkBootStrapTimeElapsed(object sender, ElapsedEventArgs e)
        {
            NetworkDiscoveryTimeout.Stop();
            NetworkDiscoveryTimeout.Elapsed -= NetworkBootStrapTimeElapsed;
            NetworkDiscoveryTimeout.Close();
            NetworkDiscoveryTimeout = null;
            Logger.Log(string.Format("INFO :: Network bootstrap timed out."));
            StartHeartbeatTimouts();
            Logger.Log(string.Format("INFO :: Heartbeat timeout started."));
        }

        private void HeartBeatTimeoutReset()
        {
            HeartBeatTimeout.Stop();
            HeartBeatTimeout.Start();
            Logger.Log("INFO :: Reset heartbeat timeout.");
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
            HeartBeatTimeoutReset();
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
            Logger.Log(string.Format("INFO :: Received the heartbeat signal from leader for term {0} .", term));

            if (term.Equals(CurrentStateData.Term))
            {
                HeartBeatTimeoutReset();
            }
            else if (term > CurrentStateData.Term)
            {
                Logger.Log("WARN ! Follower has an older term. Update the log entries to sync with leader.");
                HeartBeatTimeoutReset();
            }
            else
            {
                Logger.Log("Warning! Getting signals from old(term) Leader. Let leader's know to step down.");
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
            if (term.Equals(CurrentStateData.Term))
            {
                return true;
            }
            else if(term > CurrentStateData.Term)
            {
                Logger.Log("WARN ! Follower has an older term. Update the log entries to sync with leader.");
                return true;
            }
            else
                return false;

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
                if (NetworkDiscoveryTimeout != null)
                {
                    NetworkDiscoveryTimeout.Dispose();
                }
                if (HeartBeatTimeout != null)
                {
                    HeartBeatTimeout.Dispose();
                }
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
