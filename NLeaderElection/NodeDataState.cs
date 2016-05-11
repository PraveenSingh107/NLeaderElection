﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLeaderElection.Messages;

namespace NLeaderElection
{
    public class NodeDataState
    {
        public long Term { get; set; }
        public bool Voted { get; set; }
        public string LeaderId { get; set; }
        public LogEntry LastLogEntry { get; set; }

        public NodeDataState()
        {
            this.Term = 0;
        }

        internal void SetTerm(long term)
        {
            this.Term = term;
        }
    }
}
