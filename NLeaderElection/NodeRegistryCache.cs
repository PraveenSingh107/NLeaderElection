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
            throw new Exception("Warning ! Node not registered.");
        }
    
        public void PromoteFollowerToCandidate(Follower follower)
        {
            Candidate candidate = new Candidate(follower.GetNodeId(),follower.GetIP());
            if (candidate != null)
            {
                candidate.IP = follower.IP;
                candidate.UpdateTerm(follower.CurrentStateData.Term);
                candidate.IncrementTerm();
                this.CurrentNode = candidate;
                //follower.Dispose();
                Logger.Log(string.Format("INFO (FTC) :: {0} promoted to Candidate {1} .", follower, candidate));
                candidate.SendRequestVotesToFollowers();
            }
            else
            {
                Logger.Log("WARNING ! FOLLOWER IS ALREADY PROMOTED TO CANDIDATE. NODE IS NOT A FOLLOWER.");
            }
        }

        internal void PromoteCandidateToLeader(Candidate candidate)
        {
            Leader leader = new Leader(candidate.GetNodeId(),candidate.GetIP(),candidate.CurrentStateData.Term);
            if (leader != null)
            {
                candidate.IP = candidate.IP;
                candidate.UpdateTerm(candidate.CurrentStateData.Term);
                this.CurrentNode = leader;
                //candidate.Dispose();
                Logger.Log(string.Format("INFO (CTL) :: {0} promoted to Leader {1} .", candidate, leader));
            }
            else
            {
                Logger.Log("WARNING ! CANDIDATE ALREADY PROMOTED TO LEADER. NODE IS NOT A CANDIDATE.");
            }
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
            if(candidate != null)
            {
                Follower follower = new Follower(candidate.GetNodeId(), candidate.GetIP());
                follower.UpdateTerm(candidate.GetPreviousTerm());
                this.CurrentNode = follower;
                //candidate.Dispose();
                follower.StartHeartbeatTimouts();
                Logger.Log(string.Format("INFO (CTF) :: {0} demoted to Follower {1} .", candidate, follower));
            }
            else
            {
                Logger.Log("WARNING ! WRONG CALL TO DEMOTE CANDIDATE TO FOLLOWER. CANDIDATE ALREADY DEMOTED.");
            }
        }

        internal void DemoteLeaderToFollower()
        {
            Leader leader = (CurrentNode as Leader);
            if (leader != null)
            {
                Follower follower = new Follower(leader.GetNodeId(), leader.GetIP());
                follower.UpdateTerm(leader.CurrentStateData.Term);
                this.CurrentNode = follower;
                //leader.Dispose();
                follower.StartHeartbeatTimouts();
                Logger.Log(string.Format("INFO (LTF) :: {0} demoted to Follower {1} .", leader, follower));
            }
            else
            {
                Logger.Log("WARNING ! WRONG CALL TO DEMOTE LEADER TO FOLLOWER. LEADER ALREADY DEMOTED.");
            }
        }
    }
}
