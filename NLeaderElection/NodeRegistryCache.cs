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
            if(!nodes.Contains(node))
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
            Candidate candidate = new Candidate(follower.GetNodeId(),follower.GetIP());
            candidate.IP =  follower.IP;
            candidate.UpdateTerm(follower.CurrentStateData.Term);
            candidate.IncrementTerm();
            this.CurrentNode = candidate;
            //follower.Dispose();
            Logger.Log(string.Format("{0} promoted to Candidate {1} .",follower,candidate));
            candidate.SendRequestVotesToFollowers();
        }

        internal void PromoteCandidateToLeader(Candidate candidate)
        {
            Leader leader = new Leader(candidate.GetNodeId(),candidate.GetIP(),candidate.CurrentStateData.Term);
            candidate.IP = candidate.IP;
            candidate.UpdateTerm(candidate.CurrentStateData.Term);
            this.CurrentNode = leader;
            //candidate.Dispose();
            Logger.Log(string.Format("{0} promoted to Leader {1} .", candidate, leader));
        }

        internal void NofifyHeartbeatReceived(string content)
        {
            var receivedMsg = content.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
            if (CurrentNode is Follower)
            {
                (CurrentNode as Follower).HeartBeatSignalReceivedFromLeader(Convert.ToInt64(receivedMsg[0]));
            }
            else if (CurrentNode is Candidate)
            {
                (CurrentNode as Candidate).HeartBeatSignalReceivedFromLeader(Convert.ToInt64(receivedMsg[0]));
            }
            else
            {
                (CurrentNode as Leader).HeartBeatSignalReceivedFromLeader(Convert.ToInt64(receivedMsg[0]));
            }
        }

        internal void DemoteCandidateToFollower()
        {
            Candidate candidate = (CurrentNode as Candidate);
            Follower follower = new Follower(candidate.GetNodeId(), candidate.GetIP());
            follower.UpdateTerm(candidate.GetPreviousTerm());
            this.CurrentNode = follower;
            //candidate.Dispose();
            follower.StartHeartbeatTimouts();
            Logger.Log(string.Format("{0} demoted to Follower {1} .", candidate, follower));
        }

        internal void DemoteLeaderToFollower()
        {
            Leader leader = (CurrentNode as Leader);
            Follower follower = new Follower(leader.GetNodeId(), leader.GetIP());
            follower.UpdateTerm(leader.CurrentStateData.Term);
            this.CurrentNode = follower;
            //leader.Dispose();
            follower.StartHeartbeatTimouts();
            Logger.Log(string.Format("{0} demoted to Follower {1} .", leader, follower));
        }
    }
}
