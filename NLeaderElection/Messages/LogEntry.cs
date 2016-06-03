using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection.Messages
{
    public class LogEntry : IComparable<LogEntry>
    {
        public long Term { get; private set; }
        public long Index { get; private set; }
        public String Command { get; set; }
        public bool IsCommitted { get; set; }
        
        public LogEntry(long term, long index)
        {
            Term = term;
            Index = index;
        }

        private LogEntry() { }

        public override string ToString()
        {
            return string.Format("Term:{0}##Index:{1}##Command:{2}##Committed:{3}", 
                Term, Index, Command, IsCommitted);
        }

        public override bool Equals(object obj)
        {
            LogEntry other = (obj as LogEntry);
            if(other == null)
                return false;

             return Index.Equals(other.Index) && Term.Equals(other.Term) &&
                 IsCommitted.Equals(other.IsCommitted);
        }

        public override int GetHashCode()
        {
            return (int)Index;
        }

        public int CompareTo(LogEntry other)
        {
            var otherValue = other.Term * 100 + other.Index;
            var currentValue = this.Term * 100 + this.Index;
            if (currentValue > otherValue)
                return 1;
            else if (currentValue < otherValue)
                return -1;
            else
                return 0;
        }

        internal static LogEntry GetLogEntryFromString(string lastLogEntry)
        {
            if (string.IsNullOrEmpty(lastLogEntry))
                return null;
            else
            {
                var splittedValues = lastLogEntry.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedValues.Count() < 4)
                    return null;
                else
                {
                    LogEntry logEntry = new LogEntry();
                    foreach (var splittedValue in splittedValues)
                    {
                        var keyValuePair = splittedValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        switch (keyValuePair[0])
                        {
                            case "Term": logEntry.Term = long.Parse(keyValuePair[1]); break;
                            case "Index": logEntry.Index = long.Parse(keyValuePair[1]); break;
                            case "Command": logEntry.Command = keyValuePair[1]; break;
                            case "Committed": logEntry.IsCommitted = bool.Parse(keyValuePair[1]); break;
                        }
                    }
                    return logEntry;
                }
            }
        }
    }
}
