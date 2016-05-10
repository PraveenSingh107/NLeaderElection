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
        private readonly static Int32 HEARTBEAT_PORT_NUMBER = 11004;
        private string response = string.Empty;

        private ManualResetEvent followerRVResponseConnectDone = new ManualResetEvent(false);
        private bool isFollowerRVResponseConnectDone = false;
        private ManualResetEvent followerRVResponseSendDone = new ManualResetEvent(false);
        private bool isFollowerRVResponseSendDone = false;

        private ManualResetEvent candidateConnectDone = new ManualResetEvent(false);
        private bool isCandidateConnectDone = false;
        private ManualResetEvent candidateSendDone = new ManualResetEvent(false);
        private bool isCandidateSendDone = false;
        
        private ManualResetEvent leaderConnectDone = new ManualResetEvent(false);
        private bool isLeaderConnectDone = false;
        private ManualResetEvent leaderSendDone = new ManualResetEvent(false);
        private bool isLeaderSendDone = false;
        
        private ManualResetEvent startupRequestResponseReceiveDone = new ManualResetEvent(false);
        private bool isStartupRequestResponseReceiveDone = false;

        #endregion Properties

        private MessageBroker() { }

        public static MessageBroker GetInstance()
        {
            return new MessageBroker();
        }

        #region Leader Send Methods

        internal void LeaderSendHeartbeatAsync(Node node, long term)
        {
            Socket senderSocket = null;
            // open a tcp connection to the node's socket.
            try
            {
                Logger.Log(string.Format("INFO :: Sending heartbeat signal to {0} .", node.ToString()));
                IPAddress ipAddress = node.IP;
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, HEARTBEAT_PORT_NUMBER);

                // Create a TCP/IP socket.
                senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                senderSocket.BeginConnect(remoteEP, new AsyncCallback(HeartbeatConnectCallback), senderSocket);
                leaderConnectDone.WaitOne();

                if (isLeaderConnectDone)
                {
                    // Send test data to the remote device.
                    SendHeartbeatSignal(senderSocket, term + "##<EOF>");
                    leaderSendDone.WaitOne();
                    if (isLeaderSendDone)
                    {
                        // Write the response to the console.
                        Logger.Log(string.Format("INFO :: Sent heartbeat signal to {0} .", node.ToString()));
                    }
                }
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
            finally
            {
                // Release the socket.
                if (senderSocket != null && senderSocket.Connected)
                {
                    senderSocket.Shutdown(SocketShutdown.Both);
                    senderSocket.Close();
                }
                else if (senderSocket != null)
                {
                    senderSocket.Close();
                }
            }
        }

        private void SendHeartbeatSignal(Socket client, String data)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(HeartbeatSendCallback), client);
                 
            }
            catch (SocketException scExp)
            {
                isLeaderConnectDone = false;
                Logger.Log(scExp);
                leaderSendDone.Set();
            }
            catch (Exception e)
            {
                isLeaderConnectDone = false;
                Logger.Log(e);
                leaderSendDone.Set();
            }
        }

        private void HeartbeatSendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                isLeaderSendDone = true;
            }
            catch (SocketException scExp)
            {
                isLeaderSendDone = false;
                Logger.Log(scExp);
            }
            catch (Exception e)
            {
                isLeaderSendDone = false;
                Logger.Log(e);
            }
            finally
            {
                leaderSendDone.Set();
            }
        }

        private void HeartbeatConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);
                isLeaderConnectDone = true;
            }
            catch (System.Net.Sockets.SocketException socketExp)
            {
                isLeaderConnectDone = false;
                Logger.Log(socketExp);
            }
            catch (Exception e)
            {
                isLeaderConnectDone = false;
                Logger.Log(e);
            }
            finally
            {
                // Signal that the connection has been made.
                leaderConnectDone.Set();
            }
        }

        #endregion Leader Send Methods

        # region Candidate Send Methods

        internal void CandidateSendRequestVoteAsync(Node node, long term)
        {
            Socket candidateSocket = null;
            // open a tcp connection to the node's socket.
            try
            {
                Logger.Log(string.Format("Sending Request Vote RPC to {0} .", node.ToString()));
                IPAddress ipAddress = node.IP;
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, FOLLOWER_PORT_NUMBER);

                // Create a TCP/IP socket.
                candidateSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                candidateSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), candidateSocket);
                candidateConnectDone.WaitOne();

                // Send test data to the remote device.
                Send(candidateSocket, term.ToString() + "##<EOF>");
                candidateSendDone.WaitOne();

                // Write the response to the console.
                Logger.Log(string.Format("Sent Request Vote RPC to {0} .", node.ToString()));

            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
            finally
            {
                // Release the socket.
                if(candidateSocket != null && candidateSocket.Connected)
                {
                    candidateSocket.Shutdown(SocketShutdown.Both);
                    candidateSocket.Close();
                }
                else if (candidateSocket != null)
                {
                    candidateSocket.Close();
                }
            }
        }

        private void Send(Socket client, String data)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }
            catch (SocketException scExp)
            {
                isCandidateSendDone = false;
                Logger.Log(scExp);
                candidateSendDone.Set();
            }
            catch (Exception exp)
            {
                isCandidateSendDone = false;
                Logger.Log(exp);
                candidateSendDone.Set();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                
                // Signal that all bytes have been sent.
                isCandidateSendDone = true;
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isCandidateSendDone = false;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isCandidateSendDone = false;
            }
            finally
            {
                // Signal that all bytes have been sent.
                candidateSendDone.Set();
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

                Console.WriteLine("INFO :: Socket connected to {0}", client.RemoteEndPoint.ToString());
                isCandidateConnectDone = true;
            }
            catch (SocketException scExp)
            {
                isCandidateConnectDone = false;
                Logger.Log(scExp);
            }
            catch (Exception e)
            {
                isCandidateConnectDone = false;
                Logger.Log(e);
            }
            finally
            {
                // Signal that the connection has been made.
                candidateConnectDone.Set();
            }
        }

        # endregion

        # region Follower Send Methods

        internal void FollowerSendRVResponseAsync(IPAddress destinationIP, string data)
        {
            Socket followerRVResponseSendingSocket = null;
            // open a tcp connection to the node's socket.
            try
            {
                Logger.Log(string.Format("INFO :: Sending REQUEST VOTE RPC (RES) to Candidate : {0} .", destinationIP));
                IPEndPoint remoteEP = new IPEndPoint(destinationIP, CANDIDATE_PORT_NUMBER);

                // Create a TCP/IP socket.
                followerRVResponseSendingSocket = new Socket(destinationIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                followerRVResponseSendingSocket.BeginConnect(remoteEP, new AsyncCallback(FollowerSendRequestRPCResponseConnectCallback), followerRVResponseSendingSocket);
                followerRVResponseConnectDone.WaitOne();

                if (isFollowerRVResponseConnectDone)
                {
                    // Send test data to the remote device.
                    FollowerSendRequestRVResponse(followerRVResponseSendingSocket, data);
                    followerRVResponseSendDone.WaitOne();
                    if (isFollowerRVResponseSendDone)
                    {
                        // Write the response to the console.
                        Logger.Log(string.Format("INFO :: Sent REQUEST VOTE RPC Response to {0} .", destinationIP.ToString()));
                    }
                }
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
            finally
            {
                // Release the socket.
                if (followerRVResponseSendingSocket != null && followerRVResponseSendingSocket.Connected)
                {
                    followerRVResponseSendingSocket.Shutdown(SocketShutdown.Both);
                    followerRVResponseSendingSocket.Close();
                }
                else if (followerRVResponseSendingSocket != null)
                {
                    followerRVResponseSendingSocket.Close();
                }
            }
        }

        private void FollowerSendRequestRPCResponseConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);
                isFollowerRVResponseConnectDone = true;
            }
            catch (System.Net.Sockets.SocketException socketExp)
            {
                isFollowerRVResponseConnectDone = false;
                Logger.Log(socketExp);
            }
            catch (Exception e)
            {
                isFollowerRVResponseConnectDone = false;
                Logger.Log(e);
            }
            finally
            {
                // Signal that the connection has been made.
                followerRVResponseConnectDone.Set();
            }
        }

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

        private void FollowerSendRequestRVResponse(Socket handler, String data)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(FollowerSendToCandidateRVResposeCallback), handler);
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isFollowerRVResponseSendDone = false;
                followerRVResponseSendDone.Set();
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isFollowerRVResponseSendDone = false;
                followerRVResponseSendDone.Set();
            }
        }

        private void FollowerSendToCandidateRVResposeCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                isFollowerRVResponseSendDone = true;
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isFollowerRVResponseSendDone = false;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isFollowerRVResponseSendDone = false;
            }
            finally
            {
                followerRVResponseSendDone.Set();
            }
        }
        
        # endregion

        #region Node Startup Methods

        public void SendNodeStartupNotification(Node node)
        {
            Socket candidateSocket = null;
            // open a tcp connection to the node's socket.
            try
            {
                Logger.Log(string.Format("INFO :: Sending the startup notification to {0}",node.IP.ToString()));
                IPAddress ipAddress = node.IP;
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, STARTUP_PORT_NUMBER);

                // Create a TCP/IP socket.
                candidateSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                candidateSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), candidateSocket);
                candidateConnectDone.WaitOne();

                if (isCandidateConnectDone)
                {
                    // Send test data to the remote device.
                    string startUpMsg = NodeRegistryCache.GetInstance().IP.ToString() + "##<EOF>";
                    SendStartupNotification(candidateSocket, startUpMsg);
                    candidateSendDone.WaitOne();

                    if (isCandidateSendDone)
                    {
                        // Receive the response from the remote device.
                        ReceiveStartupResposeAsync(candidateSocket);
                        startupRequestResponseReceiveDone.WaitOne();
                        // Write the response to the console.
                        // After letting the cluster know that a new node has been added. We can start the heartbeat timeout on the follower node.
                        if (isStartupRequestResponseReceiveDone)
                        {
                            StartFollowersHeartBeatTimeout();
                            Logger.Log(string.Format("INFO :: Node startup notification response received : {0}", response));
                        }
                    }
                }

            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
            finally
            {
                // Release the socket.
                if (candidateSocket != null && candidateSocket.Connected)
                {
                    candidateSocket.Shutdown(SocketShutdown.Both);
                    candidateSocket.Close();
                }
                else if (candidateSocket != null)
                {
                    candidateSocket.Close();
                }
            }
        }

        private void StartFollowersHeartBeatTimeout()
        {
            var follower = NodeRegistryCache.GetInstance().CurrentNode as Follower;
            if (follower != null)
            {
                follower.StartHeartbeatTimouts();
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
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isStartupRequestResponseReceiveDone = false;
                startupRequestResponseReceiveDone.Set();
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isStartupRequestResponseReceiveDone = false;
                startupRequestResponseReceiveDone.Set();
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
                    var responseEntries = response.Split(new String[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                    if (responseEntries != null && responseEntries.Count() >= 2)
                    {
                        long defaultTerm = NodeRegistryCache.GetInstance().CurrentNode.GetTerm();
                        long termSentOverWire = Convert.ToInt64(responseEntries[1]);
                        NodeRegistryCache.GetInstance().CurrentNode.UpdateTerm(termSentOverWire > defaultTerm ? termSentOverWire : defaultTerm);
                    }
                    Logger.Log(string.Format("StartUp request's response from Follower: {0}, Response: {1}.", responseEntries[0],
                            responseEntries[1]));
                    isStartupRequestResponseReceiveDone = true;
                }
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isStartupRequestResponseReceiveDone = false;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isStartupRequestResponseReceiveDone = false;
            }
            finally
            {
                startupRequestResponseReceiveDone.Set();
            }
        }

        private void SendStartupNotification(Socket client, String data)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendStartupNotificationCallback), client);
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isCandidateSendDone = false;
                candidateSendDone.Set();
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isCandidateSendDone = false;
                candidateSendDone.Set();
            }
        }

        private void SendStartupNotificationCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent new node notification to other nodes.", bytesSent);
                isCandidateSendDone = true;
            }
            catch (SocketException scExp)
            {
                Logger.Log(scExp);
                isCandidateSendDone = false;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                isCandidateSendDone = false;
            }
            finally
            {
                // Signal that all bytes have been sent.
                candidateSendDone.Set();
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
                if (leaderConnectDone != null) leaderConnectDone.Dispose();
                if (leaderSendDone != null) leaderSendDone.Dispose();
                if (startupRequestResponseReceiveDone != null) startupRequestResponseReceiveDone.Dispose();
                if (followerRVResponseConnectDone != null) followerRVResponseConnectDone.Dispose();
                if (followerRVResponseSendDone != null) followerRVResponseSendDone.Dispose();
            }
        }

        ~MessageBroker()
        {
            Dispose(false);
        }

        #endregion Disposable

        
    }
}
