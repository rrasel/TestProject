using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TCPIPListenerServer.Common;
using System.Net.Sockets;
using System.Net;

namespace TCPIPListenerServer.SocketListener
{
    public class AsyncSocketListener : IAsyncSocketListener
    {
        private static readonly IAsyncSocketListener instance = new AsyncSocketListener();

        private readonly ManualResetEvent mre = new ManualResetEvent(false);
        private readonly IDictionary<int, IStateObject> clients = new Dictionary<int, IStateObject>();

        public event MessageReceivedHandler MessageReceived;
        public event MessageSubmittedHandler MessageSubmitted;

        private AsyncSocketListener()
        {}

        public static IAsyncSocketListener Instance { get { return instance; } }

        public void StartListening(IPEndPoint localEndPoint, CancellationTokenSource cancel)
        {
            byte[] bytes = new Byte[1024];

            // Create a TCP/IP socket.  
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (!cancel.IsCancellationRequested)
                {
                    // Set the event to nonsignaled state.  
                    mre.Reset();
                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(OnClientConnect), listener);

                    // Wait until a connection is made before continuing.  
                    mre.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.ReadLine();
        }

        private IStateObject GetClient(int id)
        {
            IStateObject state;
            return this.clients.TryGetValue(id, out state) ? state : null;
        }

        public bool IsConnected(int id)
        {
            var state = this.GetClient(id);
            return !(state.Listener.Poll(1000, SelectMode.SelectRead) && state.Listener.Available == 0);
        }

        public void OnClientConnect(IAsyncResult asyncResult)
        {
            // Signal the main thread to continue.  
            this.mre.Set();
            try
            {
                IStateObject clientState;
                lock (this.clients)
                {
                    var id = !this.clients.Any() ? 1 : this.clients.Keys.Max() + 1;
                    Socket clientSocket = (Socket)asyncResult.AsyncState;
                    clientState = new StateObject(clientSocket.EndAccept(asyncResult), id);
                    this.clients.Add(id, clientState);
                    Console.WriteLine("Client connected. Get Id " + id);
                }
                clientState.Listener.BeginReceive(clientState.Buffer, 0, clientState.BufferSize, SocketFlags.None, this.ReceiveCallback, clientState);
            }
            catch (SocketException)
            { }
        }

        public void ReceiveCallback(IAsyncResult asyncResult)
        {
            var receiveState = (IStateObject)asyncResult.AsyncState;

            try
            {
                var receive = 0;
                if (receiveState.Listener.Connected)
                    receive = receiveState.Listener.EndReceive(asyncResult);

                if (receive > 0)
                {
                    receiveState.Append(Encoding.UTF8.GetString(receiveState.Buffer, 0, receive));

                    //this.MessageReceived?.Invoke(receiveState.Id, receiveState.Text);
                    if(this.MessageReceived !=null)
                        this.MessageReceived(receiveState.Id, receiveState.Text);

                    if (!receiveState.Text.Contains("EOT"))
                    {
                        receiveState.Reset();
                        receiveState.Listener.BeginReceive(receiveState.Buffer, 0, receiveState.BufferSize, SocketFlags.None, this.ReceiveCallback, receiveState);
                    }
                }
            }
            catch (SocketException)
            {

            }
        }

        #region Send data
        public void Send(int id, string msg, bool close)
        {
            var state = this.GetClient(id);

            if (state == null)
            {
                throw new Exception("Client does not exist.");
            }

            if (!this.IsConnected(state.Id))
            {
                throw new Exception("Destination socket is not connected.");
            }

            try
            {
                var send = Encoding.UTF8.GetBytes(msg);
                state.Close = close;
                state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, this.SendCallback, state);
            }
            catch (SocketException)
            {
                // TODO:
            }
            catch (ArgumentException)
            {
                // TODO:
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            var state = (IStateObject)result.AsyncState;

            try
            {
                state.Listener.EndSend(result);
            }
            catch (SocketException)
            { }
            catch (ObjectDisposedException)
            { }
            finally
            {
                //MessageSubmitted?.Invoke(state.Id, state.Close);
                if (MessageSubmitted != null)
                    MessageSubmitted(state.Id, state.Close);
            }
        }
        #endregion

        public void Close(int id)
        {
            var state = this.GetClient(id);

            if (state == null)
            {
                throw new Exception("Client does not exist.");
            }

            try
            {
                state.Listener.Shutdown(SocketShutdown.Both);
                state.Listener.Close();
            }
            catch (SocketException)
            {
                // TODO:
            }
            finally
            {
                lock (this.clients)
                {
                    this.clients.Remove(state.Id);
                    Console.WriteLine("Client disconnected with Id {0}", state.Id);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                lock (this.clients)
                {
                    foreach (var id in this.clients.Keys.ToArray())
                    {
                        this.Close(id);
                    }
                }

                this.mre.Dispose();
            }
            catch { }
        }

    }
}
