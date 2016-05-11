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
    public class Leader : Node, IDisposable
    {

        #region Properties
        public NodeDataState CurrentStateData { get; set; }
        public List<Follower> Followers { get; private set; }
        private object lockerObject = new Object();
        private Timer heartBeatTimeout;
        #endregion Properties

        # region Constructors
        
        public Leader()
            : this(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        { }

        public Leader(string nodeId)
            : base(nodeId)
        {
            Followers = new List<Follower>();
            heartBeatTimeout = new Timer(1000);
            heartBeatTimeout.Elapsed += HeartBeatTimeoutElapsed;
            heartBeatTimeout.Start();
            CurrentStateData = new NodeDataState();
        }

        public Leader(string nodeId, IPAddress ip, long term)
            : base(ip, nodeId)
        {
            Followers = new List<Follower>();
            heartBeatTimeout = new Timer(1000);
            heartBeatTimeout.Elapsed += HeartBeatTimeoutElapsed;
            heartBeatTimeout.Start();
            CurrentStateData = new NodeDataState();
            CurrentStateData.SetTerm(term);
        }
        
        # endregion Constructors

        # region Methods

        private void HeartBeatTimeoutElapsed(object sender, ElapsedEventArgs e)
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
            Logger.Log(string.Format("INFO (L) :: Initiating sending heartbeat signals for term {0}.",GetTerm()));
            var dummyFollowers = NodeRegistryCache.GetInstance().Get();
                foreach (var follower in dummyFollowers)
                {
                    try
                    {
                        Task.Run(() => { MessageBroker.GetInstance().LeaderSendHeartbeatAsync(follower, this.CurrentStateData.Term); });
                    }
                    catch (Exception exp)
                    {
                        Logger.Log(exp);
                    }
                }
            if (dummyFollowers == null || dummyFollowers.Count == 0)
                Logger.Log("INFO (L) :: No followers registered to the cluster Or Leader is not aware of any follower.");
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
            Logger.Log(string.Format("INFO (L) :: HB SIGNAL (REC) from leader."));
            lock (lockerObject)
            {
                if (NodeRegistryCache.GetInstance().CurrentNode != null && NodeRegistryCache.GetInstance().CurrentNode is Leader)
                DetachEventListerners();
                NodeRegistryCache.GetInstance().DemoteLeaderToFollower();
            }
        }

        public override long GetTerm()
        {
            return this.CurrentStateData.Term;
        }

        public override void UpdateTerm(long term)
        {
            this.CurrentStateData.Term = term;
        }

        //public override void IncrementTerm()
        //{
        //    this.CurrentStateData.Term++;
        //}

        internal void DetachEventListerners()
        {
            if (heartBeatTimeout != null)
            {
                heartBeatTimeout.Elapsed -= HeartBeatTimeoutElapsed;
                heartBeatTimeout.Stop();
                heartBeatTimeout.Close();
            }
        }

        internal long GetPreviousTerm()
        {
            return --this.CurrentStateData.Term;
        }

        #endregion Methods
    
    }
}
