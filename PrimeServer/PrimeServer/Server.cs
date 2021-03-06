﻿using System;
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
using System.Windows.Documents;
using PrimeLibrary;


namespace PrimeServer
{
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
        
        private TimeSpan timeout = new TimeSpan(0,0,0, 300);

        private string primeDataFilePath, pendingValuesFilePath;

        IPAddress ip;
        private int port;

        #endregion

        private Server()
        {
            primeDataFilePath = "primes.txt";
            pendingValuesFilePath = "pendings.xml";
        }

        /// <summary>
        /// Set up server configuration. You need to relaunch server after this method invoked to apply options
        /// </summary>
        public void Configurate(string pendingFilePath, string primeFilePath, int port = 11000, string ip = "any")
        {
            if (String.IsNullOrEmpty(ip) || ip.Equals("any"))
                this.ip = IPAddress.Any;
            else
            {
                this.ip = IPAddress.Parse(ip);    
            }
            
            this.port = port;
            this.pendingValuesFilePath = pendingFilePath;
            this.primeDataFilePath = primeFilePath;
        }

        private void LoadLastState()
        {

            loader = new PrimeLoader(primeDataFilePath, pendingValuesFilePath);

            BigInteger start;
            int range;

            var pendingValues = loader.LoadPendingValues(out start, out range);
            generator = new PrimeGenerator(start, timeout, range);

            generator.AddPendingValue(pendingValues);

            LaunchTimer();

            //We could put here an event =(
        }

        #region Events & Handlers

        public static event EventHandler<TEventArgs<string>> Started;
        private void StartedEventHandler()
        {
            if (Started == null) return;
            var args = new TEventArgs<string>() {data = "Launch complete"};
            Started.Invoke(this, args);
        }

        public static event EventHandler<TEventArgs<string>> Listening;
        private void ListeningEventHandler()
        {
            if (Listening == null) return;
            var args = new TEventArgs<string>() { data = "Waiting for a client" };
            Listening.Invoke(this, args);
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

        public static event EventHandler TimeoutCheckBegan;
        private void TimeoutCheckBeganEventHandler()
        {
            if (TimeoutCheckBegan == null) return;
            TimeoutCheckBegan.Invoke(this, null);
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
            LoadLastState();

            var bytes = new byte[1024];

            //Validate IP address
            var endPoint = new IPEndPoint(ip, port);

            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                mainSocket.Bind(endPoint);
                mainSocket.Listen(100);

                this.StartedEventHandler();

                Task.Factory.StartNew(StartListeningCycle);

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

                loader.SavePendingValues(generator.GetPendingValues(), generator.StartValue, generator.Range);
                loader.Close();
            }
            catch (Exception ex)
            {
                ExceptionEventHandler(ex);
            }

        }

        private void LaunchTimer()
        {
            timeoutCheckTimer = new System.Timers.Timer {AutoReset = true, Interval = timeout.TotalMilliseconds};
            timeoutCheckTimer.Elapsed += CheckTimeout;
            timeoutCheckTimer.Start();
        }

        private void CheckTimeout(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            TimeoutCheckBeganEventHandler();
            generator.CheckTimeout();
        }

        private void StartListeningCycle()
        {
            try
            {
                while (true)
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
            var byteData = Encoding.ASCII.GetBytes(String.Format("{0}<EOF>",data));

            var state = new StateObject { workSocket = handler};
            state.sb.Append(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                SendCallback, state);
        }

        private void ParseMessage(StateObject state)
        {
            var content = state.sb.ToString().EraseEnding();

            if (content.Equals("Gimme"))
            {
                Send(state.workSocket, generator.GenerateNewValueMessage());
            }
            if (content.Contains("Gotcha"))
            {
                Send(state.workSocket, "Thanks!");
                var data = content.Split(' ');

                if (data.Length > 2 && !String.IsNullOrEmpty(data[2]))
                {
                    var list = Array.ConvertAll(data[2].Split(','), BigInteger.Parse).ToList();
                    loader.SavePrimeData(list);
                }

                var packetId = Guid.Parse(data[1]);
                generator.RemoveFromPending(packetId);
            }

            DataRecievedEventHandler(content);
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
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }

            allDone.Set();
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;

            var bytesRead = 0;

            try
            {
                bytesRead = state.workSocket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                ExceptionEventHandler(ex);
                bytesRead = 0;
            }
           

            if (bytesRead <= 0) return;
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

            var content = state.sb.ToString();

            if (content.IndexOf("<EOF>", System.StringComparison.Ordinal) > -1)
            {
                //Everything is done. Now time to parse value;
                ParseMessage(state);
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
                var state = (StateObject)ar.AsyncState;

                // Complete sending the data to the remote device.
                state.workSocket.EndSend(ar);

                state.workSocket.Shutdown(SocketShutdown.Both);
                state.workSocket.Close();

                //Send complete event
                SentEventHandler(state.sb.ToString().EraseEnding());

            }
            catch (Exception e)
            {
                ExceptionEventHandler(e);
            }
        }

        #endregion
    }
}
