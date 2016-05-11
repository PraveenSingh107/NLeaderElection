using NLeaderElection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLeaderElection.Messaging
{
    public class AppendEntriesProcessor
    {
        # region Properties
        private static AppendEntriesProcessor _instance = new AppendEntriesProcessor();
        private volatile static Queue<LogEntry> _unprocessedLogs = new Queue<LogEntry>();
        private volatile static Dictionary<LogEntry, List<DummyFollowerNode>> _unrespondedLogNodes = new Dictionary<LogEntry, List<DummyFollowerNode>>();
        private volatile static Dictionary<LogEntry, List<DummyFollowerNode>> _respondedLogNodes = new Dictionary<LogEntry, List<DummyFollowerNode>>();
        private volatile static List<LogEntry> _committedLogs = new List<LogEntry>();

        private ManualResetEvent _unprocessedLogAdditionNotifier = new ManualResetEvent(false);
        # endregion 

        private AppendEntriesProcessor() {
            Task.Run(() => { AddUnrespondedLog(); });
        }

        public static AppendEntriesProcessor GetInstance()
        {
            return _instance;
        }

        public void AddUnprocessedLog(LogEntry logEntry)
        {
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
                var logEntry = _unprocessedLogs.Dequeue();
                foreach(var node in dummyNodes)
                {
                    if (_unrespondedLogNodes.ContainsKey(logEntry))
                        _unrespondedLogNodes[logEntry].Add(node);
                    else
                    {
                        var nodes = new List<DummyFollowerNode>();
                        nodes.Add(node);
                        _unrespondedLogNodes.Add(logEntry, nodes);
                    }
                    MessageBroker.GetInstance().LeaderSendAppendEntryRPCAsync(logEntry, node);
                }
            }
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
                NodeRegistryCache.GetInstance().UpdateCommitLogToCommittedState(response);
                _committedLogs.Add(response.CurrentLogEntry);
                NodeRegistryCache.GetInstance().NotifyClientOnCommandCommitted(response.CurrentLogEntry);
                SendoutCommitAppendEntry(response.CurrentLogEntry,node);
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

        private void SendoutCommitAppendEntry(LogEntry logEntry,DummyFollowerNode node)
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
    }
}
