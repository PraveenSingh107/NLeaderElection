using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    public interface IPersistenceManager
    {
        void SaveUncommittedLogEntry(LogEntry log);
        LogEntry GetLastUncommittedLogEntry();
        
        List<LogEntry> GetAllUncommittedLogEntries();
        void SaveCommittedLogEntry(LogEntry log);

        LogEntry GetLastCommittedLogEntry();
        List<LogEntry> GetAllCommittedLogEntries();
    }
}
