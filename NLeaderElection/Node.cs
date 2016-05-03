﻿using NLeaderElection.Exceptions;
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
        public IPAddress IP { get;set;}
        protected long term;

        public Node(string nodeId)
        {
            this.nodeId = nodeId;
            
            IPAddress[] IPs = Array.FindAll(
                Dns.GetHostEntry(string.Empty).AddressList,
                a => a.AddressFamily == AddressFamily.InterNetwork);
            
            if (IPs.Count() > 1)
                throw new MoreThanOneIPAddressConfigured("Critical :: More than one IP addresses are registered. Please bind single IP.");
           
            IP = IPs[0];
            term = 1;
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
            term = 1;
        }

        public Node(string nodeId, IPAddress IPAddress, long termPassed)
        {
            this.nodeId = nodeId;
            IP = IPAddress;
            term = termPassed;
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
