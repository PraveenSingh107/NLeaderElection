using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLeaderElection.Messaging
{
    public class RequestListener
    {
        private readonly static Int32 FOLLOWER_PORT_NUMBER = 11000;
        private readonly static Int32 CANDIDATE_PORT_NUMBER = 11001;
        private readonly static Int32 STARTUP_PORT_NUMBER = 11002;
        public static ManualResetEvent followerAsyncHandler = new ManualResetEvent(false);
        public static ManualResetEvent candidateAsyncHandler = new ManualResetEvent(false);
        public static ManualResetEvent startupAsyncHandler = new ManualResetEvent(false);
        public static ManualResetEvent candidatePortListener = new ManualResetEvent(false);
        public static ManualResetEvent followerPortListener = new ManualResetEvent(false);

        public static void StartListeningOnPorts()
        {

            Task.Run(() => { WaitForRequestVotesFromCandidateAsync(); });
            Task.Run(() => { WaitForStartupMessageFromFollowerAsync(); });
            candidatePortListener.WaitOne();
            followerPortListener.WaitOne();
        }

        public static void StopListeningOnPorts()
        {
        }

        private static void WaitForRequestVotesFromCandidateAsync()
        {
            byte[] incomingData = new byte[1024];
            byte[] outGoingData = new byte[1024];

            IPEndPoint followerEndPoint = new IPEndPoint(NodeRegistryCache.GetInstance().IP, FOLLOWER_PORT_NUMBER);
            IPEndPoint candidateEndPoint = new IPEndPoint(NodeRegistryCache.GetInstance().IP, CANDIDATE_PORT_NUMBER);

            Socket followerListener = new Socket(followerEndPoint.AddressFamily,SocketType.Stream, ProtocolType.Tcp);
            Socket candidateListener = new Socket(candidateEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                followerListener.Bind(followerEndPoint);
                candidateListener.Bind(candidateEndPoint);
                Logger.Log("Follower started Listening on port " + FOLLOWER_PORT_NUMBER + " .");
                Logger.Log("Candidate started Listening on port " + CANDIDATE_PORT_NUMBER + " .");
                followerListener.Listen(1000);
                candidateListener.Listen(1000);
                while (true)
                {
                    followerAsyncHandler.Reset();
                    candidateAsyncHandler.Reset();
                    followerListener.BeginAccept(FollowerAcceptCallback, followerListener);
                    candidateListener.BeginAccept(CandidateAcceptCallback, candidateListener);
                    candidatePortListener.Set();
                    followerAsyncHandler.WaitOne();
                    candidateAsyncHandler.WaitOne();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void FollowerAcceptCallback(IAsyncResult ar)
        {
            followerAsyncHandler.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(FollowerReadCallback), state);
        }

        public static void FollowerReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    
                    string outputContect = MessageBroker.GetInstance().FollowerProcessIncomingDataFromCandidate(content);
                    // Echo the data back to the client.
                    MessageBroker.GetInstance().FollowerSendRequestRPCResponse(handler, outputContect);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(FollowerReadCallback), state);
                }
            }
        }

        public static void CandidateAcceptCallback(IAsyncResult ar)
        {
            followerAsyncHandler.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(CandidateReadCallback), state);
        }

        public static void CandidateReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Candidate candidate = (NodeRegistryCache.GetInstance().CurrentNode as Candidate);
                    if (candidate != null)
                    {
                        candidate.ResponseCallbackFromFollower(content);
                    }
                    else
                    {
                        // TO DO . exception response should be passed to candidate node. Log exception
                    }
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(FollowerReadCallback), state);
                }
            }
        }

        public static void WaitForStartupMessageFromFollowerAsync()
        {
            byte[] incomingData = new byte[1024];
            IPEndPoint startupFollowerEndPoint = new IPEndPoint(NodeRegistryCache.GetInstance().IP, STARTUP_PORT_NUMBER);
            Socket startupFollowerListener = new Socket(startupFollowerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                startupFollowerListener.Bind(startupFollowerEndPoint);
                Logger.Log(NodeRegistryCache.GetInstance().CurrentNode.ToString() + " started Listening on port " + STARTUP_PORT_NUMBER + " for new joining followers.");
                startupFollowerListener.Listen(1000);
                
                while (true)
                {
                    startupAsyncHandler.Reset();
                    startupFollowerListener.BeginAccept(StartupFollowerAcceptCallback, startupFollowerListener);
                    followerPortListener.Set();
                    startupAsyncHandler.WaitOne();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void StartupFollowerAcceptCallback(IAsyncResult ar)
        {
            startupAsyncHandler.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(StartupFollowerReadCallback), state);
        }

        public static void StartupFollowerReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    var ipAddressArr = content.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                    if (ipAddressArr != null && ipAddressArr.Count() >= 1)
                    {
                        var ipParts = ipAddressArr[0].Split(new Char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                        byte[] ipPartsArr = ipParts.Select(s => Convert.ToByte(s, 10)).ToArray();
                        IPAddress ip = new IPAddress(ipPartsArr);
                        DummyFollowerNode node = new DummyFollowerNode(ip);
                        NodeRegistryCache.GetInstance().Register(node);
                        Logger.Log("New node "+ node.ToString() + " successfully added in the cluster.");
                        
                        // send term as the output
                        string responseMsg = NodeRegistryCache.GetInstance().CurrentNode.GetNodeId() + "##" 
                            + NodeRegistryCache.GetInstance().CurrentNode.GetTerm() + "##<EOF>";

                        SendStartupMessageResponse(handler, responseMsg);
                    }
                    else
                    {
                        Logger.Log("Wrong message content on startup follower notification message.");
                    }
                    // Echo the data back to the client.
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(StartupFollowerReadCallback), state);
                }
            }
        }

        private static void SendStartupMessageResponse(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendStartupMessageResponseCallback), handler);
        }

        private static void SendStartupMessageResponseCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Logger.Log(string.Format("Sent new node found notification's response to source node.", bytesSent));

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
