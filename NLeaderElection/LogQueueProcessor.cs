using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    internal class LogQueueProcessor : ILogProcessor
    {
        private static LogQueueProcessor instance = new LogQueueProcessor();
        volatile Queue<LogEntry> _qLogEntries = new Queue<LogEntry>();
        private Object _lockerObject = new object();

        public void Add(LogEntry logEntry)
        {
            _qLogEntries.Enqueue(logEntry);
        }

        public Messages.LogEntry GetNext()
        {
            if (_qLogEntries.Count > 0)
            {
                return _qLogEntries.Dequeue();
            }
            return null;
        }

        public bool IsEmpty()
        {
            if (_qLogEntries.Count > 0)
                return false;
            else 
                return true;
        }

    }
}
