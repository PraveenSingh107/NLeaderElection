using NLeaderElection.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLeaderElection.Exceptions;

namespace NLeaderElection.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string nodes = "-Bind '192.168.56.102' -Nodes '192.168.56.101'";
                args = new String[] { nodes };
                Logger.Log(string.Format("INFO :: User Command : {0}.", nodes));
                Setup(args);
                Console.Read();
            }
            catch (MoreThanOneIPAddressConfigured exp)
            {
                Console.Write(exp.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
            Console.Read();
        }

        private static void Setup(String[] args)
        {
                Logger.Log("INFO :: Follower setup process started.");
                string ip = GetBindedIpAddress(args);
                Follower follower = null;
                if (!String.IsNullOrEmpty(ip))
                {
                    var ipPartsArr = ip.Split(new Char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    byte[] ipParts = ipPartsArr.Select(s => Convert.ToByte(s, 10)).ToArray();
                    IPAddress ipAddress = new IPAddress(ipParts);
                    follower = new Follower(ipAddress);
                }
                else
                {
                    follower = new Follower();
                }
                NodeRegistryCache.GetInstance().RegisterCurrentNode(follower);
                Logger.Log(string.Format("Node added : {0}", follower.ToString()));
                RequestListener.StartListeningOnPorts();
                RegisterCluster(args);
                follower.StartUp();
                follower.StartTimouts();
        }

        private static string GetBindedIpAddress(string[] args)
        {
            string ip = string.Empty;
            if (args != null && args[0].Count() > 0 && args[0].ToUpper().Contains("BIND"))
            {
                var switchOptions = args[0].Split(new Char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var optionSwitch in switchOptions)
                {
                    var switchValues = optionSwitch.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (switchValues[0].ToUpper().Equals("BIND"))
                    {
                        ip = switchValues[1].Replace("'", "");
                        break;
                    }
                }
            }
            return ip;
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
