//licHeader
//===============================================================================================================
// System  : Nistec.Channels - Nistec.Channels Class Library
// Author  : Nissim Trujman  (nissim@nistec.net)
// Updated : 01/07/2015
// Note    : Copyright 2007-2015, Nissim Trujman, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that is part of nistec library.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: http://nistec.net/license/nistec.cache-license.txt.  
// This notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who      Comments
// ==============================================================================================================
// 10/01/2006  Nissim   Created the code
//===============================================================================================================
//licHeader|
using Nistec.Generic;
using Nistec.IO;
using Nistec.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCP = System.Net.Sockets;

namespace Nistec.Channels.Tcp
{

    public abstract class TcpServerPool<TRequest>
    {

        #region membrs
        private bool Listen;
        private bool Initilize = false;
        ClientConnectionPool Pool;
        Thread[] threadPool;

        #endregion

        #region settings
        private ChannelServiceState _State = ChannelServiceState.None;
        /// <summary>
        /// Get <see cref="ChannelServiceState"/> State.
        /// </summary>
        public ChannelServiceState ServiceState { get { return _State; } }

        /// <summary>
        /// Get current <see cref="TcpSettings"/> settings.
        /// </summary>
        public TcpSettings Settings { get; protected set; }
        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

        /// <summary>
        /// Get current <see cref="TcpSettings"/> settings.
        /// </summary>
        public bool IsReady { get; protected set; }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpServerPool()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServerPool(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServerPool(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServerPool(TcpSettings settings)
        {
            Settings = settings;
        }
        #endregion

        #region Initilize

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
        IPEndPoint endpoint;
        int sockeErrors = 0;
        int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
        private bool IsAsync = true;
        private Thread _listenerThread;
        TcpListener _listener;

        private void Init()
        {

            if (Initilize)
                return;
            IsReady = false;
            endpoint = Settings.GetEndpoint();
            MAX_SOCKET_ERRORS = Settings.MaxSocketError;
            IsAsync = Settings.IsAsync;
            Pool = new ClientConnectionPool();
            int numThreads = Math.Max(1, Settings.MaxServerConnections);
            threadPool = new Thread[numThreads];

            for (int i = 0; i < numThreads; i++)
            {
                threadPool[i] = new Thread(AsyncProcessPool);
                threadPool[i].IsBackground = true;
                threadPool[i].Start();
            }
            OnLoad();
            Log.Info("TcpServer Initilized...\n");
            IsReady = true;
        }


        protected virtual void OnLoad()
        {

        }

        protected virtual void OnStart()
        {

        }

        protected virtual void OnStop()
        {

        }

        protected virtual void OnPause()
        {

        }
        public void Start()
        {
            try
            {
                if (_State == ChannelServiceState.Paused)
                {
                    _State = ChannelServiceState.Started;
                    OnStart();
                    return;
                }

                if (_State == ChannelServiceState.Started)
                    return;

                Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                Log.Info("TcpServer started: {0}", Settings.HostName);
                StartInternal(IsAsync);
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = ChannelServiceState.None;
                Log.Exception("The tcp server on start throws the error: ", ex, true, true);
            }
        }

        void StartInternal(bool isAsync)
        {
            try
            {

                _listener = new TcpListener(endpoint);
                _listener.ExclusiveAddressUse = false;
                _listener.Start();

                if (isAsync)
                    _listenerThread = new Thread(RunAsync);
                else
                    _listenerThread = new Thread(Run);

                _listenerThread.IsBackground = true;
                _listenerThread.Start();

            }
            catch (Exception ex)
            {
                Log.Exception("The tcp server async listener on Start throws the error: ", ex, true, true);
                return;
            }

        }

        public void Stop()
        {
            Listen = false;
            for (int i = 0; i < threadPool.Length; i++)
            {
                if (threadPool[i] != null && threadPool[i].IsAlive)
                    threadPool[i].Join();
            }

            // Close all client connections
            while (Pool.Count > 0)
            {
                var client = Pool.Dequeue();
                client.Close();
            }
            _State = ChannelServiceState.Stoped;
            OnStop();
            Log.Info("TcpServer stoped: {0}", Settings.HostName);
        }

        public void Pause()
        {
            Listen = false;
            _State = ChannelServiceState.Paused;
            OnPause();
            Log.Debug("TcpServer paused: {0}", Settings.HostName);
        }
        #endregion

        #region Read/Write

        internal void ExecFault(TCP.TcpClient client, string reason)
        {
            Log.Error("Tcp listener fault error: " + reason);
            if (client != null)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    if (client != null)
                    {
                        var ack = FaultAck(reason);
                        WriteResponse(stream, ack);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Tcp listener ExecFault error: " + ex.Message);
                }
            }
        }
        /// <summary>
        /// Create fault ack.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected virtual NetStream FaultAck(string reason)
        {
            return new TcpMessage("Fault", "ack", reason, 0).ToStream();
        }

        protected abstract TRequest ReadRequest(NetworkStream stream);


        protected virtual void WriteResponse(NetworkStream stream, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            int cbResponse = bResponse.iLength;

            stream.Write(bResponse.ToArray(), 0, cbResponse);

        }

        /// <summary>
        /// Exec Requset
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract NetStream ExecRequset(TRequest request);

        #endregion

        #region Run

        private void Run()
        {

            while (Listen)
            {
                try
                {
                    if (_State == ChannelServiceState.Paused)
                    {
                        Log.Warn("The tcp server pool is in paused state, please wait for server to be ready.");
                        Thread.Sleep(10000);
                        continue;
                    }

                    TCP.TcpClient client = _listener.AcceptTcpClient();
                    if (IsReady == false)
                    {
                        ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                        Close(client);
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (client != null)
                    {
                        Pool.Enqueue(client);
                    }

                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    sockeErrors++;
                    Log.Exception("The tcp server throws SocketException: {0}", se);
                    if (sockeErrors > MAX_SOCKET_ERRORS)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception("The tcp server throws the error: ", ex, true, true);
                }
                Thread.Sleep(10);
            }

        }

        //begin runAsync
        private void RunAsync()
        {

            while (Listen)
            {

                try
                {
                    if (_State == ChannelServiceState.Paused)
                    {
                        Log.Warn("The tcp server pool is in paused state, please wait for server to be ready.");
                        Thread.Sleep(10000);
                        continue;
                    }

                    tcpClientConnected.Reset();
                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), _listener);
                    //connected = true;
                    tcpClientConnected.WaitOne();

                }
                catch (SocketException se)
                {
                    sockeErrors++;
                    Log.Exception("The tcp server async throws SocketException: {0}", se);
                    if (sockeErrors > MAX_SOCKET_ERRORS)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception("The tcp server async throws the error: ", ex, true, true);
                }
            }
        }

        void ProcessIncomingConnection(IAsyncResult ar)
        {
            TcpListener listener = null;
            TCP.TcpClient client = null;

            try
            {
                listener = (TcpListener)ar.AsyncState;
                client = listener.EndAcceptTcpClient(ar);
                if (IsReady == false)
                {
                    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    Close(client);
                    Thread.Sleep(1000);
                }
                else
                {
                    if (client != null)
                    {
                        Pool.Enqueue(client);
                    }

                }
            }
            catch (SocketException se)
            {
                int val = Interlocked.Increment(ref sockeErrors);
                if (val > MAX_SOCKET_ERRORS)
                {
                    Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                    listener.Stop();
                }
                Log.Exception("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);

            }
            catch (Exception ex)
            {
                Log.Exception("The tcp server async ProcessIncomingConnection throws the error: ", ex, true, true);
            }
            finally
            {
                tcpClientConnected.Set();
            }
        }
        //end runAsync


        void Close(TCP.TcpClient client)
        {
            if (client != null)
            {
                if (client.Connected)
                {
                    //client.EndConnect();
                }
                client.Close();
                client = null;
            }
        }


        #endregion

        private void AsyncProcessPool()
        {

            while (Listen)
            {
                try
                {

                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(10000);
                        continue;
                    }
                    lock (Pool.SyncRoot)
                    {
                        if (Pool.Count > 0)
                        {
                            TCP.TcpClient client = Pool.Dequeue();
                            if (client != null)
                            {
                                Task.Factory.StartNew(() => MessageStreamHandler(client));
                            }
                        }
                    }
                    Thread.Sleep(10);
                    sockeErrors = 0;
                }
                catch (Exception ex)
                {
                    Log.Exception("The tcp server throws the error: ", ex, true, true);
                }
            }

        }

        private void MessageStreamHandler(TCP.TcpClient client)
        {
            try
            {
                // Get a stream object for reading and writing
                using (NetworkStream stream = client.GetStream())
                {

                    TRequest req = ReadRequest(stream);

                    var res = ExecRequset(req);

                    WriteResponse(stream, res);
                }
            }
            catch (SocketException se)
            {
                int val = Interlocked.Increment(ref sockeErrors);
                if (val > MAX_SOCKET_ERRORS)
                {
                    Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                }
                Log.Exception("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);

            }
            catch (Exception ex)
            {
                Log.Exception("The tcp server async ProcessIncomingConnection throws the error: ", ex, true, true);
            }
            finally
            {
                Close(client);
            }
        }


        class ClientConnectionPool
        {
            // Creates a synchronized wrapper around the Queue.
            private Queue SyncdQ = Queue.Synchronized(new Queue());

            public void Enqueue(TCP.TcpClient client)
            {
                SyncdQ.Enqueue(client);
            }

            public TCP.TcpClient Dequeue()
            {
                return (TCP.TcpClient)(SyncdQ.Dequeue());
            }

            public int Count
            {
                get { return SyncdQ.Count; }
            }

            public object SyncRoot
            {
                get { return SyncdQ.SyncRoot; }
            }

        }


    }

    /// <summary>
    /// Endpoin Pool
    /// </summary>
    public class EndpoinPool
    {
        ConcurrentDictionary<int, IPEndPoint> endPointList = new ConcurrentDictionary<int, IPEndPoint>();
        int currentIndex = 0;
        int MaxIndex = 0;
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="array"></param>
        public EndpoinPool(IPEndPoint[] array)
        {
            if (array == null || array.Length == 0)
            {

                throw new ArgumentNullException("EndpoinPool");
            }
            int i = 0;
            foreach (var item in array)
            {
                endPointList[i] = item;
                i++;
            }
            MaxIndex = array.Length - 1;
        }
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="ports"></param>
        public EndpoinPool(IPAddress ip, int[] ports)
        {
            if (ip == null || ports == null || ports.Length == 0)
            {
                throw new ArgumentNullException("EndpoinPool");
            }
            int i = 0;
            foreach (var item in ports)
            {
                endPointList[i] = new IPEndPoint(ip, item);
                i++;
            }
            MaxIndex = ports.Length - 1;
        }
        /// <summary>
        /// Get Next IPEndPoint
        /// </summary>
        /// <returns></returns>
        public IPEndPoint Next()
        {
            if (0 != Interlocked.CompareExchange(ref currentIndex, MaxIndex, 0))
                Interlocked.Increment(ref currentIndex);
            else
                Interlocked.Exchange(ref currentIndex, 0);

            return endPointList[currentIndex]; ;
        }

    }
}
