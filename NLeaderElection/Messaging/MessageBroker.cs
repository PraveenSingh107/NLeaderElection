using NLeaderElection.Messages;
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
    /// <summary>
    /// Responsible for synchronization and communication between all nodes.
    /// </summary>
    public class MessageBroker : IDisposable
    {
        
        #region Properties
        
        private readonly static Int32 FOLLOWER_PORT_NUMBER = 11000;
        private readonly static Int32 CANDIDATE_PORT_NUMBER = 11001;
        private readonly static Int32 STARTUP_PORT_NUMBER = 11002;
        private string response = string.Empty;
        private ManualResetEvent candidateConnectDone = new ManualResetEvent(false);
        private ManualResetEvent candidateSendDone = new ManualResetEvent(false);
        private ManualResetEvent startupRequestResponseReceiveDone = new ManualResetEvent(false);
        
        #endregion Properties

        private MessageBroker(){}

        public static MessageBroker GetInstance()
        {
            return new MessageBroker();
        }

        # region Candidate Send Methods

        internal  void CandidateSendRequestVoteAsync(Node node,long term)
        {
            // open a tcp connection to the node's socket.
            try
            {
                IPAddress ipAddress = node.IP;
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, CANDIDATE_PORT_NUMBER);

                // Create a TCP/IP socket.
                Socket candidate = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                candidate.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), candidate);
                candidateConnectDone.WaitOne();

                // Send test data to the remote device.
                Send(candidate, term + "##<EOF>");
                candidateSendDone.WaitOne();

                // Write the response to the console.
                Logger.Log(string.Format("Response received : {0}", response));

                // Release the socket.
                candidate.Shutdown(SocketShutdown.Both);
                candidate.Close();

            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
        }

        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                candidateSendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",  client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                candidateConnectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        # endregion

        # region Follower Send Methods
        
        internal  void UpdateCandidateWithRequestVoteResposeAsync()
        {
            // update the respective candidate with response.
        }

        //internal  void FollowerSendRequestVoteResponseAsync(string nodeId,long term)
        //{
        //    Follower follower = (NodeRegistry.GetInstance().Get(nodeId) as Follower);
        //    if (follower != null)
        //    {
        //        RequestVoteRPCMessage requestVoteRPCMessage = new RequestVoteRPCMessage(term);
        //        var response = follower.RespondToRequestVoteFromCandidate(requestVoteRPCMessage);
        //        // open a tcp connection to candidate node to response back.
        //        try
        //        {
        //            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        //            IPAddress ipAddress = ipHostInfo.AddressList[0];
        //            IPEndPoint remoteEP = new IPEndPoint(ipAddress, FOLLOWER_PORT_NUMBER);

        //            // Create a TCP/IP socket.
        //            Socket candidate = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //            // Connect to the remote endpoint.
        //            candidate.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), candidate);
        //            candidateConnectDone.WaitOne();

        //            // Send test data to the remote device.
        //            Send(candidate, nodeId +"##" + term + "##<EOF>");
        //            candidaetSendDone.WaitOne();

        //            // Write the response to the console.
        //            Logger.Log(string.Format("Response received : {0}", response));

        //            // Release the socket.
        //            candidate.Shutdown(SocketShutdown.Both);
        //            candidate.Close();

        //        }
        //        catch (Exception e)
        //        {
        //            Logger.Log(e.ToString());
        //        }
        //    }
        //}

        public string FollowerProcessIncomingDataFromCandidate(string content)
        {
            var tokens = content.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
            Follower currentFollower = (NodeRegistryCache.GetInstance().CurrentNode as Follower);
            if (currentFollower != null && tokens != null)
            {
                var response = currentFollower.RespondToRequestVoteFromCandidate(new RequestVoteRPCMessage(
                    Convert.ToInt64(tokens[0])));

                return response.ResponseType.ToString() + "##" + response.Term.ToString()
                    + "##" + response.FollowerId.ToString() + "<EOF>";
            }
            else if (tokens.Count() <= 0)
            {
                return "Exception: Wrong input sent.<EOF>";
            }
            else
            {
                return "Eception: Symentic exception. Case not handled.<EOF>";
            }
        }

        public void FollowerSendRequestRPCResponse(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(FollowerSendToCandidateCallback), handler);
        }

        private  void FollowerSendToCandidateCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
       
        # endregion

        #region Node Startup Methods

        public void SendNodeStartupNotification(Node node)
        {
            // open a tcp connection to the node's socket.
            try
            {
                IPAddress ipAddress = node.IP;
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, STARTUP_PORT_NUMBER);

                // Create a TCP/IP socket.
                Socket candidate = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                candidate.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), candidate);
                candidateConnectDone.WaitOne();

                // Send test data to the remote device.
                string startUpMsg = ipAddress.ToString() + "##<EOF>";
                SendStartupNotification(candidate, startUpMsg);
                candidateSendDone.WaitOne();

                // Receive the response from the remote device.
                ReceiveStartupResposeAsync(candidate);
                startupRequestResponseReceiveDone.WaitOne();
                // Write the response to the console.
                Logger.Log(string.Format("Response received : {0}", response));

                // Release the socket.
                candidate.Shutdown(SocketShutdown.Both);
                candidate.Close();

            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
        }

        private void ReceiveStartupResposeAsync(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveStartupResposeAsyncCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveStartupResposeAsyncCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveStartupResposeAsyncCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    var responseEntries  = response.Split(new String[] {"##"},StringSplitOptions.RemoveEmptyEntries);
                    if (responseEntries != null && responseEntries.Count() >= 2)
                    {
                        long defaultTerm = NodeRegistryCache.GetInstance().CurrentNode.GetTerm();
                        long termSentOverWire = Convert.ToInt64(responseEntries[1]);
                        NodeRegistryCache.GetInstance().CurrentNode.UpdateTerm(termSentOverWire > defaultTerm ? termSentOverWire : defaultTerm);

                    }
                    Logger.Log(string.Format("StartUp request's response from Follower: {0}, Response: {1}.", responseEntries[0],
                            responseEntries[1]));
                    startupRequestResponseReceiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendStartupNotification(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendStartupNotificationCallback), client);
        }

        private void SendStartupNotificationCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                candidateSendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
       
        #endregion

        # region Disposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (candidateConnectDone != null) candidateConnectDone.Dispose();
                if (candidateSendDone != null) candidateSendDone.Dispose();
            }
        }

        ~MessageBroker()
        {
            Dispose(false);
        }

        #endregion Disposable
    }
}
