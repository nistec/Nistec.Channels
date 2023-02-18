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
using Nistec.IO;
using Nistec.Logging;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Represent a json tcp server listner.
    /// </summary>
    public abstract class TcpJsonServer : TcpServer<StringMessage,TransStream>
    {

        //public Func<string, NetStream> ActionRequset { get; set; }

        #region override
        /// <summary>
        /// OnStart
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
        }
        /// <summary>
        /// OnStop
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();
        }
        /// <summary>
        /// OnLoad
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();
        }
        #endregion

        #region ctor


        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        public TcpJsonServer(string hostName)
            : base(hostName)
        {
            //LoadRemoteCache();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="settings"></param>
        public TcpJsonServer(TcpSettings settings)
            : base(settings)
        {
            //LoadRemoteCache();
        }

        #endregion

        #region abstract methods

        ///// <summary>
        ///// Execute client request and return response as stream.
        ///// </summary>
        ///// <param name="message"></param>
        ///// <returns></returns>
        //protected override NetStream ExecRequset(StringMessage message)
        //{
        //    if (ActionRequset != null)
        //        return ActionRequset(message.Message);
        //    else
        //    {
        //        string response=null;
        //        DoActionRequset(message.Message, ref response);
        //        if (response == null)
        //            return null;
        //        var nstream = new NetStream();
        //        StringMessage.WriteString(response, nstream);
        //        return nstream;
        //    }
        //}

        /// <summary>
        /// ReadRequest
        /// </summary>
        /// <param name="networkStream"></param>
        /// <returns></returns>
        protected override StringMessage ReadRequest(NetworkStream networkStream)
        {
            //StringMessage.ReadString(pipeServer);


            StringMessage message = null;
            using (var ntStream = new NetStream())
            {
                ntStream.CopyFrom(networkStream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

                message = new StringMessage(ntStream);
            }
            return message;

            //return new StringMessage(networkStream);
        }

        #endregion
    }

    /// <summary>
    /// Represent a json tcp server listner.
    /// </summary>
    public abstract class TcpFlexServer : TcpServer<MessageFlex, MessageFlex>
    {
        //public Func<string, NetStream> ActionRequset { get; set; }

        #region override
        /// <summary>
        /// OnStart
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
        }
        /// <summary>
        /// OnStop
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();
        }
        /// <summary>
        /// OnLoad
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();
        }
        #endregion

        #region ctor

        
        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        public TcpFlexServer(string hostName)
            : base(hostName)
        {
            //LoadRemoteCache();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="settings"></param>
        public TcpFlexServer(TcpSettings settings)
            : base(settings)
        {
            //LoadRemoteCache();
        }

        #endregion

        #region abstract methods

        ///// <summary>
        ///// Execute client request and return response as stream.
        ///// </summary>
        ///// <param name="message"></param>
        ///// <returns></returns>
        //protected override NetStream ExecRequset(StringMessage message)
        //{
        //    if (ActionRequset != null)
        //        return ActionRequset(message.Message);
        //    else
        //    {
        //        string response=null;
        //        DoActionRequset(message.Message, ref response);
        //        if (response == null)
        //            return null;
        //        var nstream = new NetStream();
        //        StringMessage.WriteString(response, nstream);
        //        return nstream;
        //    }
        //}

        /// <summary>
        /// ReadRequest
        /// </summary>
        /// <param name="networkStream"></param>
        /// <returns></returns>
        protected override MessageFlex ReadRequest(NetworkStream networkStream)
        {
            //StringMessage.ReadString(pipeServer);

            MessageFlex message = null;
            using (var ntStream = new NetStream())
            {
                ntStream.CopyFrom(networkStream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

                message = MessageFlex.Parse(ntStream);
            }
            return message;

            //return new StringMessage(networkStream);
        }

        #endregion
    }


    ///// <summary>
    ///// Represent a base class for tcp server listner.
    ///// </summary>
    ///// <typeparam name="TRequest"></typeparam>
    //public abstract class TcpJsonServer<TRequest> where TRequest : ITransformMessage
    //{

    //    #region membrs
    //    volatile bool Listen;
    //    private bool Initilized = false;
    //    private bool IsAsync = true;
    //    #endregion

    //    #region settings

    //    private ChannelServiceState _State = ChannelServiceState.None;
    //    /// <summary>
    //    /// Get <see cref="ChannelServiceState"/> State.
    //    /// </summary>
    //    public ChannelServiceState ServiceState { get { return _State; } }
    //    /// <summary>
    //    /// Get current <see cref="TcpSettings"/> settings.
    //    /// </summary>
    //    public TcpSettings Settings { get; protected set; }
    //    ILogger _Logger = Logger.Instance;
    //    /// <summary>
    //    /// Get or Set Logger that implements <see cref="ILogger"/> interface.
    //    /// </summary>
    //    public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

    //    /// <summary>
    //    /// Get current <see cref="TcpSettings"/> settings.
    //    /// </summary>
    //    public bool IsReady { get; protected set; }

    //    #endregion

    //    #region ctor

    //    /// <summary>
    //    /// Constractor default
    //    /// </summary>
    //    protected TcpJsonServer()
    //    {
    //        Settings = new TcpSettings();
    //    }

    //    /// <summary>
    //    /// Constractor using hostAddress and port.
    //    /// </summary>
    //    /// <param name="hostAddress"></param>
    //    /// <param name="port"></param>
    //    protected TcpJsonServer(string hostAddress, int port)
    //    {
    //        Settings = new TcpSettings(hostAddress, port);
    //    }

    //    /// <summary>
    //    /// Constractor using host configuration.
    //    /// </summary>
    //    /// <param name="configHost"></param>
    //    protected TcpJsonServer(string configHost)
    //    {
    //        Settings = new TcpSettings(configHost, true);
    //    }

    //    /// <summary>
    //    /// Constractor using settings.
    //    /// </summary>
    //    /// <param name="settings"></param>
    //    protected TcpJsonServer(TcpSettings settings)
    //    {
    //        Settings = settings;
    //        Log = settings.Log;
    //    }
    //    #endregion

    //    #region Initilize

    //    ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
    //    IPEndPoint endpoint;
    //    //bool connected = false;
    //    int sockeErrors = 0;
    //    int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
    //    private Thread _listenerThread;
    //    TcpListener _listener;
    //    private void Init()
    //    {

    //        if (Initilized)
    //            return;
    //        IsReady = false;
    //        endpoint = Settings.GetEndpoint();
    //        MAX_SOCKET_ERRORS = Settings.MaxSocketError;
    //        IsAsync = Settings.IsAsync;
    //        OnLoad();
    //        Log.Info("TcpServer Initilized...\n");
    //        IsReady = true;
    //    }

    //    protected virtual void OnLoad()
    //    {

    //    }

    //    protected virtual void OnStart()
    //    {

    //    }

    //    protected virtual void OnStop()
    //    {

    //    }

    //    protected virtual void OnPause()
    //    {

    //    }


    //    protected virtual void OnFault(string message, Exception ex)
    //    {
    //        Log.Exception(message, ex, true);
    //    }


    //    public void Start()
    //    {
    //        try
    //        {
    //            if (_State == ChannelServiceState.Paused)
    //            {
    //                if (Initilized)
    //                {
    //                    _State = ChannelServiceState.Started;
    //                    OnStart();
    //                    return;
    //                }
    //            }
    //            if (_State == ChannelServiceState.Started)
    //                return;

    //            Listen = true;
    //            Init();
    //            _State = ChannelServiceState.Started;
    //            OnStart();
    //            StartInternal(IsAsync);
    //        }
    //        catch (Exception ex)
    //        {
    //            Listen = false;
    //            _State = ChannelServiceState.None;
    //            OnFault("The tcp server on start throws the error: ", ex);
    //        }
    //    }

    //    void StartInternal(bool isAsync)
    //    {
    //        try
    //        {

    //            _listener = new TcpListener(endpoint);
    //            _listener.ExclusiveAddressUse = false;
    //            _listener.Start();

    //            if (isAsync)
    //                _listenerThread = new Thread(RunAsync);
    //            else
    //                _listenerThread = new Thread(Run);

    //            _listenerThread.IsBackground = true;
    //            _listenerThread.Start();
    //            Initilized = true;

    //        }
    //        catch (Exception ex)
    //        {
    //            OnFault("The tcp server async listener on Start throws the error: ", ex);
    //            return;
    //        }
    //    }

    //    void StopInternal()
    //    {
    //        try
    //        {
    //            Log.Info("The tcp server listener Stoping...");
    //            Thread.Sleep(3000);
    //            if (_listener != null)
    //            {
    //                _listener.Stop();
    //                //_listenerThread.Interrupt();
    //                //_listenerThread.Join(5000);
    //            }
    //            Initilized = false;
    //        }
    //        catch (ThreadInterruptedException ex)
    //        {
    //            /* Clean up. */
    //            OnFault("The tcp server on Stop throws ThreadInterruptedException: ", ex);
    //        }
    //        catch (Exception ex)
    //        {
    //            OnFault("The tcp server listener on Stop throws the error: ", ex);
    //        }
    //    }

    //    public void Stop()
    //    {
    //        Listen = false;
    //        StopInternal();
    //        Initilized = false;
    //        _State = ChannelServiceState.Stoped;
    //        OnStop();
    //        Log.Info("TcpServer stoped: {0}", Settings.HostName);
    //    }

    //    public void Pause()
    //    {
    //        Listen = false;
    //        _State = ChannelServiceState.Paused;
    //        OnPause();
    //        Log.Debug("TcpServer paused: {0}", Settings.HostName);
    //    }
    //    #endregion

    //    #region Read/Write

    //    internal void ExecFault(TCP.TcpClient client, string reason)
    //    {
    //        Log.Error("Tcp listener fault error: " + reason);
    //        if (client != null && client.Connected)
    //        {
    //            try
    //            {
    //                NetworkStream stream = client.GetStream();
    //                if (client != null)
    //                {
    //                    var ack = FaultAck(reason);
    //                    WriteResponse(stream, ack);
    //                    Close(client);
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                OnFault("Tcp listener ExecFault error: ", ex);
    //            }
    //            finally
    //            {
    //                Close(client);
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// Create fault ack.
    //    /// </summary>
    //    /// <param name="reason"></param>
    //    /// <returns></returns>
    //    protected virtual StringMessage FaultAck(string reason)
    //    {
    //        return StringMessage.WriteState(-1, reason);//.ToStream();
    //        //return TransStream.Write(new TcpMessage("Fault", "ack", reason, 0), TransType.Object);//.ToStream();
    //    }
    //    /// <summary>
    //    /// Read Request from client.
    //    /// </summary>
    //    /// <param name="stream"></param>
    //    /// <returns></returns>
    //    protected abstract TRequest ReadRequest(NetworkStream stream);

    //    /// <summary>
    //    /// Exec client requset.
    //    /// </summary>
    //    /// <param name="request"></param>
    //    /// <returns></returns>
    //    protected abstract string ExecRequset(TRequest request);

    //    /// <summary>
    //    /// Write response to client.
    //    /// </summary>
    //    /// <param name="stream"></param>
    //    /// <param name="bResponse"></param>
    //    protected virtual void WriteResponse(NetworkStream stream, StringMessage bResponse)
    //    {
    //        if (bResponse == null)
    //        {
    //            return;
    //        }
            
    //        var ns = bResponse.GetBytes();
    //        if (ns == null)
    //        {
    //            return;
    //        }
    //        int lenth = ns.Length;
    //        //stream.WriteValue(cbResponse);
    //        stream.Write(ns, 0, lenth);
    //    }


    //    #endregion

    //    #region Run

    //    /// <summary>
    //    /// Occured when client is connected.
    //    /// </summary>
    //    protected virtual void OnClientConnected()
    //    {
    //        //Console.WriteLine("Debuger-OnTcpClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
    //    }

    //    private void Run()
    //    {
    //        //bool hasFault = false;

    //        while (Listen)
    //        {
    //            TCP.TcpClient client = null;
    //            try
    //            {
    //                //hasFault = false;
    //                if (_State == ChannelServiceState.Paused)
    //                {
    //                    Thread.Sleep(5000);
    //                    continue;
    //                }

    //                client = _listener.AcceptTcpClient();

    //                if (IsReady == false)
    //                {
    //                    //hasFault = true;
    //                    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
    //                    Thread.Sleep(1000);
    //                    continue;
    //                }
    //                //connected = true;
    //                OnClientConnected();

    //                //using (NetworkStream stream = client.GetStream())
    //                //{
    //                //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

    //                //    var res = ExecRequset(req);
    //                //    if (req.IsDuplex)
    //                //        WriteResponse(stream, res);
    //                //}
    //                //sockeErrors = 0;

    //                //ProcessIncomingData(client,readtimeout, ReceiveBufferSize,true);

    //                Task task = Task.Factory.StartNew(() => ProcessIncomingData(client, false));
    //                {
    //                    task.Wait();
    //                    //if (task.IsCompleted)
    //                    //{
    //                    //    Console.WriteLine("ProcessIncomingData completed");
    //                    //}
    //                }
    //                task.TryDispose();

    //                //connected = false;
    //                sockeErrors = 0;
    //            }
    //            catch (SocketException se)
    //            {
    //                //hasFault = true;
    //                sockeErrors++;
    //                OnFault("The tcp server throws SocketException: ", se);
    //                //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
    //                if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
    //                {
    //                    Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
    //                    _listener.Stop();
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                //hasFault = true;
    //                OnFault("The tcp server throws the error: ", ex);
    //                //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
    //            }
    //            finally
    //            {
    //                //Close(client);
    //            }
    //        }

    //    }

    //    private class ServerCom : IDisposable
    //    {
    //        public long Uid;
    //        public TCP.TcpClient client;
    //        public ManualResetEvent ManualReset;

    //        public void Dispose()
    //        {
    //            if (ManualReset != null)
    //            {
    //                ManualReset.Close();
    //                ManualReset.Dispose();
    //                ManualReset = null;
    //            }

    //            if (client != null)
    //            {
    //                if (client.Connected)
    //                {
    //                    client.Close();
    //                }
    //                //client.Close();
    //                client = null;
    //            }
    //        }
    //    }

    //    private void RunAsync()
    //    {

    //        while (Listen)
    //        {

    //            try
    //            {

    //                tcpClientConnected.Reset();
    //                _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), _listener);
    //                //connected = true;
    //                //OnClientConnected();
    //                tcpClientConnected.WaitOne();

    //            }
    //            catch (SocketException se)
    //            {
    //                sockeErrors++;
    //                OnFault("The tcp server async throws SocketException: {0}", se);
    //                if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
    //                {
    //                    Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
    //                    _listener.Stop();
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                OnFault("The tcp server async throws the error: ", ex);
    //            }
    //        }
    //    }

    //    void ProcessIncomingConnection(IAsyncResult ar)
    //    {
    //        TcpListener listener = null;
    //        TCP.TcpClient client = null;

    //        try
    //        {
    //            if (Listen == false)
    //            {
    //                Log.Warn("The tcp server async ProcessIncomingConnection not lisenning... ");
    //                Thread.Sleep(1000);
    //                return;
    //            }

    //            listener = (TcpListener)ar.AsyncState;
    //            client = listener.EndAcceptTcpClient(ar);

    //            if (IsReady == false)
    //            {
    //                //hasFault = true;
    //                ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
    //                Thread.Sleep(1000);
    //                return;
    //            }
    //            OnClientConnected();

    //            //using (NetworkStream stream = client.GetStream())
    //            //{
    //            //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

    //            //    var res = ExecRequset(req);
    //            //    if (req.IsDuplex)
    //            //        WriteResponse(stream, res);
    //            //}
    //            //sockeErrors = 0;

    //            //ProcessIncomingData(client, readtimeout, ReceiveBufferSize,true);

    //            Task task = Task.Factory.StartNew(() => ProcessIncomingData(client, false));
    //            {
    //                task.Wait();
    //                //if(task.IsCompleted)
    //                //{
    //                //    Console.WriteLine("ProcessIncomingConnection completed");
    //                //}
    //            }
    //            task.TryDispose();
    //        }
    //        catch (SocketException se)
    //        {
    //            int val = Interlocked.Increment(ref sockeErrors);
    //            OnFault("The tcp server async throws SocketException: {0}", se);
    //            ExecFault(client, "The tcp server throws Exception: " + se.Message);
    //            if (val > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
    //            {
    //                Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
    //                listener.Stop();
    //            }
    //            OnFault("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);
    //        }
    //        catch (Exception ex)
    //        {
    //            ExecFault(client, "The tcp server throws Exception: " + ex.Message);
    //            OnFault("The tcp server async ProcessIncomingConnection throws the error: ", ex);
    //        }
    //        finally
    //        {
    //            //Close(client);
    //            tcpClientConnected.Set();
    //        }
    //    }


    //    void ProcessIncomingData(TCP.TcpClient client, bool enableException)
    //    {
    //        try
    //        {
    //            if (Listen == false)
    //            {
    //                Log.Warn("The tcp server async ProcessIncomingData not lisenning... ");
    //                Thread.Sleep(1000);
    //                return;
    //            }
    //            using (NetworkStream stream = client.GetStream())
    //            {
    //                TRequest req = ReadRequest(stream);

    //                var res = ExecRequset(req);
    //                if (req.IsDuplex)
    //                    WriteResponse(stream, res);
    //            }
    //            sockeErrors = 0;
    //            //connected = false;
    //        }
    //        catch (Exception ex)
    //        {
    //            if (enableException)
    //                throw ex;
    //            else
    //                OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
    //        }
    //        finally
    //        {
    //            Close(client);
    //        }
    //    }

    //    void Close(TCP.TcpClient client)
    //    {
    //        try
    //        {
    //            if (client != null)
    //            {
    //                if (client.Connected)
    //                {
    //                    //client.EndConnect();
    //                }
    //                client.Close();
    //                client = null;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            OnFault("Close TcpClient error ", ex);
    //        }
    //    }
    //    #endregion
    //}
}
