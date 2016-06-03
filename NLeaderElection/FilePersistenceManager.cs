using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    public class FilePersistenceManager : IPersistenceManager
    {

        private string directoryName;
        private string committedLogFileName = "commitLogs.txt";
        private string uncommittedLogFileName = "uncommitLogs.txt";

        private StreamWriter _committedWriter;
        private StreamWriter _uncommittedWriter;
        
        public FilePersistenceManager()
        {
            try
            {
                directoryName = Directory.GetCurrentDirectory() + "\\..\\..\\";
                Directory.CreateDirectory(directoryName + "Logs\\");
                directoryName += "Logs\\";
                if (!File.Exists(directoryName + committedLogFileName))
                   using (File.Create(directoryName + committedLogFileName)) { }
                
                if (!File.Exists(directoryName + uncommittedLogFileName))
                    using (File.Create(directoryName + uncommittedLogFileName)) { }
            }
            catch (Exception exp)
            {
                Logger.Log(exp.Message);
            }
        }

        public static IPersistenceManager GetInstance()
        {
            return new FilePersistenceManager();
        }

        public void SaveUncommittedLogEntry(LogEntry logEntry)
        {
            if (_uncommittedWriter == null)
            {
                _uncommittedWriter = new StreamWriter(File.Open(directoryName + uncommittedLogFileName,FileMode.Append));
                _uncommittedWriter.AutoFlush = true;
            }

            _uncommittedWriter.WriteLine(logEntry.ToString());
        }

        public void SaveCommittedLogEntry(LogEntry logEntry)
        {
            if (_committedWriter == null)
            {
                _committedWriter = new StreamWriter(File.Open(directoryName + committedLogFileName,FileMode.Append));
                _committedWriter.AutoFlush = true;
            }

            _committedWriter.WriteLine(logEntry.ToString());
        }

        public Messages.LogEntry GetLastUncommittedLogEntry()
        {
           string lastLogEntry = File.ReadAllLines(directoryName + uncommittedLogFileName).Last();
           return LogEntry.GetLogEntryFromString(lastLogEntry);
        }

        public List<Messages.LogEntry> GetAllUncommittedLogEntries()
        {
            return ReadAllLogs(uncommittedLogFileName);
        }

        private List<LogEntry> ReadAllLogs(string logFileName)
        {
            try
            {
                var logStrings = File.ReadAllLines(directoryName + logFileName);
                List<LogEntry> logs = new List<LogEntry>();
                foreach (var log in logStrings)
                {
                    logs.Add(LogEntry.GetLogEntryFromString(log));
                }
                return logs;
            }
            catch (Exception exp)
            {
                throw;
            }
        }

        public Messages.LogEntry GetLastCommittedLogEntry()
        {
            string lastLogEntry = File.ReadAllLines(directoryName + committedLogFileName).Last();
            return LogEntry.GetLogEntryFromString(lastLogEntry);
        }

        public List<Messages.LogEntry> GetAllCommittedLogEntries()
        {
            return ReadAllLogs(committedLogFileName);
        }
    }
}
