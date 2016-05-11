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
        
        public LogEntry(long term, long index)
        {
            Term = term;
            Index = index;
        }

        public override string ToString()
        {
            return string.Format("Term: {0} ## Index:{1}", Term, Index);
        }

        public override bool Equals(object obj)
        {
            LogEntry other = (obj as LogEntry);
            if(other == null)
                return false;

             return Index.Equals(other.Index) && Term.Equals(other.Term);
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
    }
}
