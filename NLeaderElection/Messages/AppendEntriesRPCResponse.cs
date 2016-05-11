using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection.Messages
{
    public enum AppendEntriesRPCResponseType
    {
        WrittenToPersistentStorage,
        LastLogEntryOutOfSync
    }

    public class AppendEntriesRPCResponse
    {

        #region Properties
        public  LogEntry CurrentLogEntry { get;private set; }
        public  AppendEntriesRPCResponseType ResponseStatus { get; set; }
        public string FollowerNodeId { get; set; }
        # endregion

        # region constructor
        public AppendEntriesRPCResponse(LogEntry currentLogEntry, AppendEntriesRPCResponseType responseType)
        {
            this.CurrentLogEntry = currentLogEntry;
            this.ResponseStatus = responseType;
        }
        # endregion
    
        # region Method
        public override string ToString()
        {
            return string.Format("AppendEntryMessage###Response: {0} ## CurrentLogEntry: {1} ## FollowerNodeId: {2}",
                ResponseStatus.ToString(), CurrentLogEntry.ToString(),FollowerNodeId);
        }
        #endregion
    
    }
}
