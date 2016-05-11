using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    internal interface ILogProcessor
    {
        void Add(LogEntry logEntry);
        LogEntry GetNext();
        bool IsEmpty();
    }
}
