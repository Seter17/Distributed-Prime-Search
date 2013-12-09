using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;


namespace PrimeServer
{
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    class Server
    {
        #region Fields

        private static volatile Server instance;

        private static readonly object syncRoot = new Object();
        private static readonly ManualResetEvent allDone = new ManualResetEvent(false);

        private Socket mainSocket;

        private PrimeLoader loader;
        private PrimeGenerator generator;

        private System.Timers.Timer timeoutCheckTimer;
        private TimeSpan timeout = new TimeSpan(0,0,0,300);

        #endregion

        private Server()
        {
            loader = new PrimeLoader("","");

            BigInteger start;
            int range;

            var pendingValues = loader.LoadPendingValues(out start, out range);
            generator = new PrimeGenerator(start, timeout, range);

            generator.AddPendingValue(pendingValues);

            LaunchTimer();
        }

        #region Events & Handlers

        public static event EventHandler<string> Started;

        private void StartedEventHandler()
        {
            if (Started == null) return;
            Started.Invoke(this, "Launch complete");
        }

        public static event EventHandler<string> Listening;

        private void ListeningEventHandler()
        {
            if (Listening == null) return;
            Listening.Invoke(this, "Waiting for a client");
        }

        public static event EventHandler<string> DataRecieved;

        private void DataRecievedEventHandler(string data)
        {
            if (DataRecieved == null) return;
            DataRecieved.Invoke(this, data);
        }


        public static event EventHandler<Exception> Exception;
        private void ExceptionEventHandler(Exception e)
        {
            if (Exception == null) return;
            Exception.Invoke(this, e);
        }
        #endregion

        #region Control & Access

        public static Server Instance
        {
            get
            {
                if (instance != null) return instance;
                lock (syncRoot)
                {
                    if (instance == null)
                        instance = new Server();
                }

                return instance;
            }
        }

        public void Launch()
        {
            var bytes = new byte[1024];
            var ipHostInfo = Dns.GetHostEntry("localhost");

            //Validate IP address
            var endPoint = new IPEndPoint(ipHostInfo.AddressList[1], 11000);

            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                mainSocket.Bind(endPoint);
                mainSocket.Listen(100);

                this.StartedEventHandler();

                var listeningThread = new Thread(StartListeningCycle);
                listeningThread.Start();

            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }

        }

        public void Stop()
        {
            try
            {
                allDone.Reset();
                if (mainSocket != null)
                    mainSocket.Close();
            }
            catch (Exception ex)
            {
                ExceptionEventHandler(ex);
            }

        }

        public void LaunchTimer()
        {
            timeoutCheckTimer = new System.Timers.Timer {AutoReset = false, Interval = timeout.TotalMilliseconds};
            timeoutCheckTimer.Elapsed += CheckTimeout;
        }

        private void CheckTimeout(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            generator.CheckTimeout();
        }

        private void StartListeningCycle()
        {
            try
            {
                while (mainSocket.Connected())
                {
                    allDone.Reset();

                    this.ListeningEventHandler();

                    mainSocket.BeginAccept(AcceptCallback, mainSocket);

                    allDone.WaitOne();
                }
            }
            catch (ObjectDisposedException ex)
            {
                
            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }
        }

        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            var byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                SendCallback, handler);
        }

        #endregion

        #region Callbacks

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                var socket = (Socket) ar.AsyncState;
                var handler = socket.EndAccept(ar);

                var state = new StateObject {workSocket = handler};
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    ReadCallback, state);
            }
            catch (ObjectDisposedException e)
            {
                //Occurs when main socket was closed
            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }

            allDone.Set();
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;

            var bytesRead = state.workSocket.EndReceive(ar);

            if (bytesRead <= 0) return;
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

            var content = state.sb.ToString();

            if (content.IndexOf("<EOF>", System.StringComparison.Ordinal) > -1)
            {
                //Everything is done. Store value, send another;
                DataRecievedEventHandler(content.Substring(0, content.IndexOf("<EOF>", System.StringComparison.Ordinal)));
                Send(state.workSocket, "This is a test<EOF>");
            }
            else
            {
                state.workSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    ReadCallback, state);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                var bytesSent = handler.EndSend(ar);

                //Send completed

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }
        }

        #endregion
    }
}
