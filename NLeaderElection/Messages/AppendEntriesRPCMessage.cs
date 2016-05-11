using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection.Messages
{
    /// <summary>
    /// Immutable Append Entry message
    /// </summary>
    public class AppendEntriesRPCMessage
    {

        # region Properties
        public LogEntry LastLogEntry { get; private set; }
        public LogEntry CurrentLogEntry { get; private set; }
        # endregion

        # region Constructor
        public AppendEntriesRPCMessage(LogEntry lastLog, LogEntry currentLog)
        {
            this.LastLogEntry = lastLog;
            this.CurrentLogEntry = currentLog;
        }
        # endregion

        # region Methods
        public override string ToString()
        {
            return string.Format("AppendEntryMessage###LastLogEntry: {} ## CurrentLogEntry: {}", this.LastLogEntry.ToString(),
                this.CurrentLogEntry.ToString());
        }
        # endregion Methods

    }
}
