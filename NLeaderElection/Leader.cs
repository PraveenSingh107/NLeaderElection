﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        public Leader() : base(DateTime.Now.ToString("yyyyMMddHHmmssffff"))
        {
            
            Followers = new List<Follower>();
            heartBeatTimeout = new Timer(150);
            heartBeatTimeout.Elapsed += HeartBeatTimeout_Elapsed;
            heartBeatTimeout.Start();

            // not sure
            CurrentStateData = new NodeDataState(1);
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
            foreach (var follower in Followers)
            {
                try
                {
                    follower.HeartBeatSignalFromLeader(CurrentStateData.Term);
                }
                catch (Exception exp)
                {
                    Logger.Log(exp);
                }
            }

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
    }
}
