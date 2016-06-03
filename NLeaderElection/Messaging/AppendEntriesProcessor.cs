using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLeaderElection.Messaging
{
    public class AppendEntriesProcessor : IDisposable
    {
        # region Properties
        private long logIndex = 0;
        private static AppendEntriesProcessor _instance = new AppendEntriesProcessor();
        private volatile static Queue<LogEntry> _unprocessedLogs = new Queue<LogEntry>();
        private volatile static Dictionary<LogEntry, List<DummyFollowerNode>> _unrespondedLogNodes = new Dictionary<LogEntry, List<DummyFollowerNode>>();
        private volatile static Dictionary<LogEntry, List<DummyFollowerNode>> _respondedLogNodes = new Dictionary<LogEntry, List<DummyFollowerNode>>();
        private volatile static List<LogEntry> _committedLogs;
        private ManualResetEvent _unprocessedLogAdditionNotifier = new ManualResetEvent(false);
        private IPersistenceManager _persistenceManager;
        # endregion

        private AppendEntriesProcessor()
        {
            try
            {
                _committedLogs = new List<LogEntry>();
                _persistenceManager = new FilePersistenceManager();
                Task.Run(() => { AddUnrespondedLog(); });
                _committedLogs.AddRange(_persistenceManager.GetAllCommittedLogEntries());
                if (_committedLogs.Count > 0)
                    logIndex = _committedLogs.Last().Index;
                else
                    logIndex = 0;
            }
            catch (Exception exp)
            {
                throw;
            }
        }

        public long LogIndex()
        {
            return ++logIndex;
        }

        public static AppendEntriesProcessor GetInstance()
        {
            return _instance;
        }

        public void AddUnprocessedLog(LogEntry logEntry)
        {
            _persistenceManager.SaveUncommittedLogEntry(logEntry);
            _unprocessedLogs.Enqueue(logEntry);
            _unprocessedLogAdditionNotifier.Set();
        }

        private void AddUnrespondedLog()
        {
            _unprocessedLogAdditionNotifier.WaitOne();
            var dummyNodes = NodeRegistryCache.GetInstance().Get();

            // TO DO : Might need to send RPC on different threads. Otherwise client response will be delayed
            while (_unprocessedLogs.Count > 0)
            {
                LogEntry logEntry = _unprocessedLogs.Dequeue();
                foreach (var node in dummyNodes)
                {
                    if (_unrespondedLogNodes.ContainsKey(logEntry))
                        _unrespondedLogNodes[logEntry].Add(node);
                    else
                    {
                        var nodes = new List<DummyFollowerNode>();
                        nodes.Add(node);
                        _unrespondedLogNodes.Add(logEntry, nodes);
                    }
                    LogEntry previousLog = GetPreviousLogEntry(logEntry);
                    AppendEntriesRPCMessage requestMsg = new AppendEntriesRPCMessage(previousLog, logEntry);
                    requestMsg.RequestType = AppendEntriesRPCRequestType.AppendEntryUncommitMessage;
                    MessageBroker.GetInstance().LeaderSendAppendEntryAsync(node as Node, logEntry.ToString(),
                        AppendEntriesRPCRequestType.AppendEntryUncommitMessage);
                }
            }
            _unprocessedLogAdditionNotifier.Reset();
            AddUnrespondedLog();
        }

        public LogEntry GetPreviousLogEntry(LogEntry logEntry)
        {
            if (_committedLogs.Contains(logEntry))
            {
                var index = _committedLogs.IndexOf(logEntry);
                if (index >= 1)
                    return _committedLogs[index - 1];
            }
            return null;
        }

        internal void AddRespondedLogNodes(AppendEntriesRPCResponse response, DummyFollowerNode node)
        {
            if (_respondedLogNodes.ContainsKey(response.CurrentLogEntry))
            {
                _respondedLogNodes[response.CurrentLogEntry].Add(node);
            }
            else
            {
                var nodes = new List<DummyFollowerNode>();
                nodes.Add(node);
                _respondedLogNodes.Add(response.CurrentLogEntry, nodes);
            }
            if (_unrespondedLogNodes.ContainsKey(response.CurrentLogEntry))
            {
                var nodes = _unrespondedLogNodes[response.CurrentLogEntry];
                if (nodes.Contains(node))
                    nodes.Remove(node);
            }
            // TO DO :: Not sure. Need to research on it
            if (!_committedLogs.Contains(response.CurrentLogEntry) &&
                _respondedLogNodes[response.CurrentLogEntry].Count >= NodeRegistryCache.GetInstance().GetConsensusCount())
            {
                // persist the committed log entry as committed log.
                _persistenceManager.SaveCommittedLogEntry(response.CurrentLogEntry);
                _committedLogs.Add(response.CurrentLogEntry);
                NodeRegistryCache.GetInstance().NotifyClientOnCommandCommitted(response.CurrentLogEntry);
                SendoutCommitAppendEntry(response.CurrentLogEntry, node);
            }
            else if (_committedLogs.Contains(response.CurrentLogEntry))
            {
                SendoutCommitAppendEntry(response.CurrentLogEntry, node);
            }

            if (_unrespondedLogNodes.ContainsKey(response.CurrentLogEntry))
            {
                var nodes = _unrespondedLogNodes[response.CurrentLogEntry];
                if (nodes.Contains(node))
                    nodes.Remove(node);
                _unrespondedLogNodes[response.CurrentLogEntry] = nodes;
            }
        }

        private void SendoutCommitAppendEntry(LogEntry logEntry, DummyFollowerNode node)
        {
            foreach (var nodes in _respondedLogNodes[logEntry])
            {
                MessageBroker.GetInstance().LeaderSendAppendEntryRPCAsync(logEntry, node);
            }
        }

        /// <summary>
        /// To be called from response listener on receiving the commit append entry smessage response.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="node"></param>

        internal void RemoveAppendEntryRespondedNode(LogEntry log, DummyFollowerNode node)
        {
            if (_respondedLogNodes.ContainsKey(log))
            {
                var nodes = _respondedLogNodes[log];
                if (nodes.Contains(node))
                {
                    nodes.Remove(node);
                    _respondedLogNodes[log] = nodes;
                }
            }
        }

        # region Disposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_unprocessedLogAdditionNotifier != null) _unprocessedLogAdditionNotifier.Dispose();
            }
        }

        ~AppendEntriesProcessor()
        {
            Dispose(false);
        }

        #endregion Disposable

    }
}
