using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace NLeaderElection
{
    public class NodeRegistryCache
    {
        private static NodeRegistryCache _singleton = new NodeRegistryCache();
        private List<DummyFollowerNode> nodes = new List<DummyFollowerNode>();
        private List<DummyFollowerNode> activeNodes = new List<DummyFollowerNode>();
        public Node CurrentNode { get;private set; }

        public void RegisterCurrentNode(Node node)
        {
            CurrentNode = node;
            this.IP = node.IP;
        }

        private NodeRegistryCache() {}
        public static NodeRegistryCache GetInstance()
        {
            return _singleton;
        }
        public IPAddress IP { get; private set; }

        public void Register(DummyFollowerNode node)
        {
            nodes.Add(node);
        }

        public List<DummyFollowerNode> Get()
        {
            return nodes;
        }

        public DummyFollowerNode Get(string nodeId)
        {
            foreach(var node in nodes)
            {
                if (node.GetNodeId().Equals(nodeId))
                    return node;
            }
            throw new Exception("Node not registered.");
        }
    
        public void PromoteFollowerToCandidate(Follower follower)
        {
            Candidate candidate = new Candidate(follower.GetNodeId(),follower.GetIP(),follower.GetTerm());
            candidate.IP =  follower.IP;
            candidate.UpdateTerm(follower.GetTerm());
            this.CurrentNode = candidate;
            Logger.Log(string.Format("{0} promoted to Candidate {1} .",follower,candidate));
            candidate.SendRequestVotesToFollowers();
        }

        internal void PromoteCandidateToLeader(Candidate candidate)
        {
            Leader leader = new Leader(candidate.GetNodeId(),candidate.GetIP(),candidate.GetTerm());
            candidate.IP = candidate.IP;
            candidate.UpdateTerm(candidate.GetTerm());
            this.CurrentNode = leader;
            Logger.Log(string.Format("{0} promoted to Leader {1} .", candidate, leader));
            leader.SendHeartBeatMessage();
        }
    }
}
