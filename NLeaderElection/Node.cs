using NLeaderElection.Exceptions;
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
        public IPAddress IP { get; set; }

        public Node(string nodeId)
        {
            this.nodeId = nodeId;

            IPAddress[] IPs = Array.FindAll(
                Dns.GetHostEntry(string.Empty).AddressList,
                a => a.AddressFamily == AddressFamily.InterNetwork);

            if (IPs.Count() > 1)
                throw new MoreThanOneIPAddressConfigured("Critical :: More than one IP addresses are registered. Please bind single IP.");

            IP = IPs[0];
            //term = 1;
        }

        public Node(string nodeId, string IPAddress)
        {
            this.nodeId = nodeId;
            IP = null;

            IPAddress[] IPs = Array.FindAll(
                Dns.GetHostEntry(IPAddress).AddressList,
                a => a.AddressFamily == AddressFamily.InterNetwork);

            if (IPs.Count() > 1)
                throw new MoreThanOneIPAddressConfigured("Please bind single IP.");

            IP = IPs[0];
        }

        public Node(IPAddress IPAddress, string nodeId)
        {
            this.nodeId = nodeId;
            IP = IPAddress;
        }

        public abstract long GetTerm();

        public abstract void UpdateTerm(long term);

        //public abstract void IncrementTerm();

        public string GetNodeId()
        {
            return nodeId;
        }
    }
}
