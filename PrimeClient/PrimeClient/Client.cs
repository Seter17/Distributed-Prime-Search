using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using PrimeLibrary;
using PrimeServer;

namespace PrimeClient
{
    class Client
    {
        enum ConnectType
        {
            Request,
            Store
        }

        #region Fields

        private static volatile Client instance;
        private static readonly object lockObject = new object();

        private bool firstRequest = true;

        private int port = 11000;
        private string ip = "127.0.0.1";

        private Socket client;

        private Guid id;
        private List<BigInteger> primes;

	    #endregion

        private Client()
        {
            primes = new List<BigInteger>();
        }

        #region Event & Handlers
        public static event EventHandler<TEventArgs<string>> Connected;
        private void ConnectedEventHandler()
        {
            if (Connected == null) return;
            var args = new TEventArgs<string>() { data = "Connection established" };
            Connected.Invoke(this, args);
        }

        public static event EventHandler<TEventArgs<string>> DataRecieved;
        private void DataRecievedEventHandler(string _data)
        {
            if (DataRecieved == null) return;
            var args = new TEventArgs<string>() { data = _data };
            DataRecieved.Invoke(this, args);
        }

        public static event EventHandler<TEventArgs<string>> Sent;
        private void SentEventHandler(string s)
        {
            if (Sent == null) return;
            var args = new TEventArgs<string>() { data = s };
            Sent.Invoke(this, args);
        }

        public static event EventHandler<TEventArgs<Exception>> Exception;
        private void ExceptionEventHandler(Exception e)
        {
            if (Exception == null) return;
            var args = new TEventArgs<Exception>() { data = e };
            Exception.Invoke(this, args);
        }

	    #endregion

        #region Control & Access

        public static Client Instance
        {
            get
            {
                if (instance != null) return instance;
                lock (lockObject)
                {
                    if(instance == null)
                        instance = new Client();
                }
                return instance;
            }
        }

        public void SetConnectionDetails(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        #region Connection methods

        public void RequestNumbers()
        {
            Connect(ConnectType.Request);
        }

        public void StorePrimes()
        {
            Connect(ConnectType.Store);
        }

        public void Disconnect()
        {
            if (client == null || !client.Connected) return;

            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private void Connect(ConnectType type)
        {
            Disconnect();

            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.IP);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP, ConnectCallback, type);
            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }
        }
        
        #endregion

        #region Send methods

        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            var byteData = Encoding.ASCII.GetBytes(data);

            var state = new StateObject { workSocket = client };
            state.sb.Append(data);
            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, state);
        }

        private void SendStoreMessage()
        {
            var message = String.Format("Gotcha {0} {1}<EOF>", id, String.Join(",", primes));
            Send(client, message);
        }

        private void SendRequestMessage()
        {
            //prepare for the next iteration
            primes.Clear();
            Send(client, "Gimme<EOF>");
        }
        
        #endregion

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                var state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    ReceiveCallback, state);
            }
            catch (Exception e)
            {
                
            }
        }

        private void ParseMessage(string message)
        {
            if (message.Equals("Thanks!"))
            {
                RequestNumbers();
                return;
            }

            var data = message.Split(' ');
            //poor check, I know, better to have id for every message but this demands time and since we have only one message incomimg so we can skip it for later
            if (data.Length != 3) return;

            try
            {
                id = Guid.Parse(data[0]);
                var startValue = BigInteger.Parse(data[1]);
                var range = Int32.Parse(data[2]);

                PrimeChecker.CheckRange(startValue, range, ref primes);

                StorePrimes();
            }
            catch (Exception ex)
            {
                ExceptionEventHandler(ex);
            }
        }

	    #endregion

        #region Callbacks
        
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                client.EndConnect(ar);

                ConnectedEventHandler();

                var type = (ConnectType)ar.AsyncState;
                switch (type)
                {
                    case  ConnectType.Request:
                        SendRequestMessage();
                        break;
                    case ConnectType.Store:
                        SendStoreMessage();
                        break;
                }
            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }
        }

        private  void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                var state = (StateObject)ar.AsyncState;
                var socket = state.workSocket;

                // Read data from the remote device.
                var bytesRead = socket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        ReceiveCallback, state);
                }
                else
                {
                    // All the data has arrived
                    if (state.sb.Length <= 1) return;

                    Disconnect();

                    DataRecievedEventHandler(state.sb.ToString().EraseEnding());

                    ParseMessage(state.sb.ToString().EraseEnding());
                }
            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }
        }

        private  void SendCallback(IAsyncResult ar)
        {
            try
            {
                var state = (StateObject)ar.AsyncState;

                state.workSocket.EndSend(ar);

                SentEventHandler(state.sb.ToString().EraseEnding());

                Receive(state.workSocket);

            }
            catch (Exception e)
            {
               ExceptionEventHandler(e);
            }
        }

        #endregion

    }
}
