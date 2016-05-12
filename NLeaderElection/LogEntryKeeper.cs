using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    public class LogEntryKeeper
    {
        # region properties
        private readonly static LogEntryKeeper _instance = new LogEntryKeeper();
        private List<LogEntry> _logs = new List<LogEntry>();
        # endregion properties

        # region constructors
        private LogEntryKeeper() { }
        # endregion constructors

        # region Methods

        public static LogEntryKeeper GetInstance()
        {
            return _instance;
        }

        public void AddCommittedLogEntry(LogEntry logEntry)
        {
            if (!_logs.Contains(logEntry))
                _logs.Add(logEntry);
        }

        public LogEntry GetPreviousLogEntry(LogEntry logEntry)
        {
            if (_logs.Contains(logEntry))
            {
                var index = _logs.IndexOf(logEntry);
                if (index >= 1)
                    return _logs[index - 1];
            }
            return null;
        }
       
        # endregion
    }
}
