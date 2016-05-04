﻿using NLeaderElection.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NLeaderElection
{
    public class Leader : Node, IDisposable
    {
        public NodeDataState CurrentStateData { get; set; }
        public List<Follower> Followers { get; private set; }
        private Timer heartBeatTimeout;

        public Leader() : this(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {}

        public Leader(string nodeId)
            : base(nodeId)
        {
            Followers = new List<Follower>();
            heartBeatTimeout = new Timer(200);
            heartBeatTimeout.Elapsed += HeartBeatTimeout_Elapsed;
            heartBeatTimeout.Start();

            // not sure
            CurrentStateData = new NodeDataState(1);
        }

        public Leader(string nodeId, IPAddress ip, long term)
            : base(nodeId, ip, term)
        {
            Followers = new List<Follower>();
            heartBeatTimeout = new Timer(200);
            heartBeatTimeout.Elapsed += HeartBeatTimeout_Elapsed;
            heartBeatTimeout.Start();
            CurrentStateData = new NodeDataState(term);
        }
        

        private void HeartBeatTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendHeartBeatMessage();
            heartBeatTimeout.Stop();
            heartBeatTimeout.Start();
        }

        public void RegisterFollower(Follower follower)
        {
            Followers.Add(follower);
        }

        public void SendHeartBeatMessage()
        {
            Logger.Log("INFO :: Initiating sending heartbeat signals.");
            var dummyFollowers = NodeRegistryCache.GetInstance().Get();
            foreach (var follower in dummyFollowers)
            {
                try
                {
                    MessageBroker.GetInstance().LeaderSendHeartbeatAsync(follower,this.CurrentStateData.Term);
                }
                catch (Exception exp)
                {
                    Logger.Log(exp);
                }
            }
            if (dummyFollowers == null || dummyFollowers.Count == 0)
                Logger.Log("INFO :: No followers registered to the cluster Or Leader is not aware of any follower.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (heartBeatTimeout != null) heartBeatTimeout.Dispose();
            }
        }

        ~Leader()
        {
            Dispose(false);
        }

        internal IPAddress GetIP()
        {
            return IP;
        }

        internal void HeartBeatSignalReceivedFromLeader(long p)
        {
            Logger.Log(string.Format("INFO :: Received heartbeat message from leader."));
            NodeRegistryCache.GetInstance().DemoteLeaderToFollower();
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
