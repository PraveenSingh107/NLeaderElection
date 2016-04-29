using NLeaderElection.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLeaderElection.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string nodes = "-Nodes '192.168.56.102'";
                args = new String[] { nodes };
                Logger.Log(string.Format("Adding {0} on the cluster.",nodes));
                Setup(args);
                Console.Read();
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
            Console.Read();
        }

        private static void Setup(String[] args)
        {
            try
            {
                Logger.Log("INFO :: Follower setup process started.");
                Follower follower = new Follower();
                NodeRegistryCache.GetInstance().RegisterCurrentNode(follower);
                Logger.Log(string.Format("Node added : {0}", follower.ToString()));
                RequestListener.StartListeningOnPorts();
                RegisterCluster(args);
                follower.StartUp();
                follower.StartTimouts();
            }
            catch (Exception exp)
            {
                Logger.Log(exp);
            }
        }

        private static void RegisterCluster(string[] args)
        {
            if (args == null)
                return;
            if (args.Length >= 0 && !string.IsNullOrEmpty(args[0]))
            {
                var switches = args[0].Split(new Char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var optionSwitch in switches)
                {
                     var switchValues = optionSwitch.Split(new char[]{' '},StringSplitOptions.RemoveEmptyEntries);
                     if (switchValues[0].Equals("Nodes"))
                     {
                         var nodes  = switchValues[1].Replace("'","").Split(new Char[]{','});
                         foreach (var node in nodes)
                         {
                             var ipParts = node.Split(new Char[]{'.'},StringSplitOptions.RemoveEmptyEntries);
                             byte[] ipPartsArr = ipParts.Select(s => Convert.ToByte(s, 10)).ToArray();
                             IPAddress ip = new IPAddress(ipPartsArr);
                             NodeRegistryCache.GetInstance().Register(new DummyFollowerNode(ip));
                         }
                     }
                }
            }
        }
    }
}
