﻿using System;
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
        private static Timer ElectionTimeout;
        private static Timer HeartBeatTimeout;
        public NodeDataState CurrentStateData { get; set; }
        
        public Follower() : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {
            SetupTimeouts();
        }

        public Follower(IPAddress address)
            : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"),address)
        {
            SetupTimeouts();
        }

        private void SetupTimeouts()
        {
            ElectionTimeout = new Timer(200);
            HeartBeatTimeout = new Timer(200);
            ElectionTimeout.Elapsed += ElectionTimeout_Elapsed;
            HeartBeatTimeout.Elapsed += HeartBeatTimeout_Elapsed;
            ElectionTimeout.Start();
            HeartBeatTimeout.Start();
        }

        public void StartTimouts()
        {
            ElectionTimeout.Start();
            HeartBeatTimeout.Start();
        }

        private void HeartBeatTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
            NodeRegistryCache.GetInstance().PromoteFollowerToCandidate(this);
            HeartBeatTimeout.Stop();
            ElectionTimeout.Stop();
            Dispose();
        }

        private void ElectionTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
        //        throw new NotImplementedException();
        }

        private void HeartBeatTimeout_Reset()
        {
            HeartBeatTimeout.Stop();
            HeartBeatTimeout.Start();
        }

        private void ElectionTimeout_Reset()
        {
            ElectionTimeout.Stop();
            ElectionTimeout.Start();
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

        public virtual void HeartBeatSignalFromLeader(long term)
        {
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
                if (ElectionTimeout != null) ElectionTimeout.Dispose();
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
