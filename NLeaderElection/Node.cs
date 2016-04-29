using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NLeaderElection
{
    public abstract class Node
    {
        protected string nodeId;
        public IPAddress IP { get;private set;}
        protected long term;

        public Node(string nodeId)
        {
            this.nodeId = nodeId;
            IP = null;
            //IP = //Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            IPAddress[] IPs = Array.FindAll(
                Dns.GetHostEntry(string.Empty).AddressList,
                a => a.AddressFamily == AddressFamily.InterNetwork);
            foreach (var ip in IPs)
            {
                if (ip.ToString().StartsWith("192"))
                {
                    IP = ip;
                }
            }
            if (IP == null)
            {
                IP = IPs[0]; 
            }
            term = 1;
        }

        public long GetTerm()
        {
            return this.term;
        }

        public void UpdateTerm(long term)
        {
            this.term = term;   
        }

        public void IncrementTerm()
        {
            term++;
        }

        public Node(string nodeId, IPAddress ipAddress)
        {
            this.nodeId = nodeId;
            IP = ipAddress;
        }

        public string GetNodeId()
        {
            return nodeId;
        }
    }
}
