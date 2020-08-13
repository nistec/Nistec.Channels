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
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TCP = System.Net.Sockets;

namespace Nistec.Channels.Http
{
   /// <summary>
   /// Represent a base class for tcp server listner.
   /// </summary>
   /// <typeparam name="TRequest"></typeparam>
    public abstract class HttpServer<TRequest>
    {

        #region membrs
        private bool Initilized = false;
        #endregion

        #region settings

        private ChannelServiceState _State = ChannelServiceState.None;
        /// <summary>
        /// Get <see cref="ChannelServiceState"/> State.
        /// </summary>
        public ChannelServiceState ServiceState { get { return _State; } }
        /// <summary>
        /// Get current <see cref="HttpSettings"/> settings.
        /// </summary>
        public HttpSettings Settings { get; protected set; }
        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        /// <summary>
        /// Get current <see cref="HttpSettings"/> settings.
        /// </summary>
        public bool IsReady { get; protected set; }

        #endregion

        #region ctor

 
        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected HttpServer(string configHost)
            : this(new HttpSettings(configHost, true))
        {
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected HttpServer(HttpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            //_workers = new Thread[maxThreads];
        }

        #endregion

        #region Initilize
        
        int httpErrors = 0;
        int MAX_ERRORS = HttpSettings.DefaultMaxErrors;


        private readonly HttpListener _listener;
        private Thread _listenerThread;
        private Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;
        int _maxThread;
        int _timeout;
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;
            _maxThread = Settings.MaxThreads;
            if (_maxThread <= 0)
                _maxThread = 1;

            //if (Settings.HostAddress == null || Settings.HostAddress.Length == 0)
            //    throw new ArgumentException("Invalid Host Address");

            //var prefixes = { "http://localhost:8080/app/root", "https://localhost:8443/app/root" };

            _listener.Prefixes.Clear();
            if (Settings.IsValidHostAddress())
                _listener.Prefixes.Add(Settings.HostAddress);
            if (Settings.IsValidSslHostAddress())
                _listener.Prefixes.Add(Settings.SslHostAddress);

            if (_listener.Prefixes.Count == 0)
            {
                throw new ArgumentException("Invalid Host Address");
            }

            //if (!_listener.Prefixes.Contains(Settings.HostAddress))
            //{
            //    _listener.Prefixes.Add(Settings.HostAddress);
            //}
            _timeout = Settings.ConnectTimeout;
            Initilized = true;

            MAX_ERRORS = Settings.MaxErrors;
            OnLoad();
            Log.Info("HttpServer Initilized...\n");
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


        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true, true);
        }

        
        public void Start()
        {
            try
            {
                if (_State == ChannelServiceState.Paused)
                {
                    if (Initilized)
                    {
                        _State = ChannelServiceState.Started;
                        OnStart();
                        return;
                    }
                }
                if (_State == ChannelServiceState.Started)
                    return;
                //Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                Run();
            }
            catch (Exception ex)
            {
                //Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The http server on start throws the error: ", ex);
            }
        }

        void StopInternal()
        {
            try
            {
                Log.Info("The http server listener Stoping...");
                Thread.Sleep(3000);
                if (_listener != null)
                {
                    _listener.Stop();
                    //_listenerThread.Interrupt();
                    //_listenerThread.Join(5000);
                    //if (_workers != null)
                    //{
                    //    for (int i = 0; i < _workers.Length; i++)
                    //    {
                    //        _workers[i].Interrupt();
                    //        _workers[i].Join(5000);
                    //    }
                    //}
                }
                Initilized = false;

            }
            catch (ThreadInterruptedException ex)
            {
                /* Clean up. */
                OnFault("The http server on Stop throws ThreadInterruptedException: ", ex);
            }
            catch (Exception ex)
            {
                OnFault("The http server async listener on Stop throws the error: ", ex);
            }
        }

        public void Stop()
        {
            StopInternal();
            Initilized = false;
            //Listen = false;
            _State = ChannelServiceState.Stoped;
            OnStop();
            Log.Info("HttpServer stoped: {0}", Settings.HostName);
        }

        public void Pause()
        {
            //Listen = false;
            _State = ChannelServiceState.Paused;
            OnPause();
            Log.Debug("HttpServer paused: {0}", Settings.HostName);
        }
        #endregion

        #region Read/Write

        internal void ExecFault(HttpListenerContext context, string reason)
        {
            Log.Error("Http listener fault error: " + reason);
            if (context != null)
            {
                try
                {
                    //NetworkStream stream = client.GetStream();
                    if (context != null)
                    {
                        var ack = FaultAck(reason);
                        WriteResponse(context, ack);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Http listener ExecFault error: " + ex.Message);
                }
            }
        }
        /// <summary>
        /// Create fault ack.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected virtual TransStream FaultAck(string reason)
        {
            return TransStream.Write(new HttpMessage("Fault", "ack", reason, 0), TransType.Object);
            //return new HttpMessage("Fault", "ack", reason, 0).ToStream();
        }
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(HttpRequestInfo request);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TransStream ExecTransStream(TRequest request);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract string ExecString(TRequest request);

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(HttpListenerContext context, TransStream bResponse)
        {
            var response = context.Response;
            if (context.Request.HttpMethod == "OPTIONS")
            {
                if (Settings.Allow_Headers)
                    response.AddHeader("Access-Control-Allow-Headers", Settings.AllowAccessHeaders);
            }
            if (Settings.Allow_Origin)
                response.AppendHeader("Access-Control-Allow-Origin", Settings.AllowAccessOrigin);

            if (bResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.StatusDescription = "No response";
                return;
            }
            byte[] buffer = null;

            if (bResponse.PeekTransType() == TransType.Json)
            {
                var json = bResponse.ReadJson();
                buffer=Encoding.UTF8.GetBytes(json);
            }
            else
            {
                var ns = bResponse.GetStream();
                int cbResponse = ns.iLength;
                buffer = ns.ToArray();
            }

            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="strResponse"></param>
        protected virtual void WriteResponse(HttpListenerContext context, string strResponse)
        {
            var response = context.Response;
            if (context.Request.HttpMethod == "OPTIONS")
            {
                if (Settings.Allow_Headers)
                    response.AddHeader("Access-Control-Allow-Headers", Settings.AllowAccessHeaders);
            }
            if (Settings.Allow_Origin)
                response.AppendHeader("Access-Control-Allow-Origin", Settings.AllowAccessOrigin);

            if (strResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.StatusDescription = "No response";
                return;
            }

            response.ContentType = "text/plain";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strResponse);
            int cbResponse = buffer.Length;

            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        #endregion

        #region Run

        private void HandleRequests()
        {

            while (_listener.IsListening)
            {
                HttpListenerContext context = null;
                try
                {
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(10000);
                        continue;
                    }

                    var contextState = _listener.BeginGetContext(ContextCallback, null);
                    context = (HttpListenerContext)contextState.AsyncState;
                    //connected = true;
                    if (IsReady == false)
                    {
                        ExecFault(context, "The http server is not ready to accept client requests, please wait for server to be ready.");
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (0 == WaitHandle.WaitAny(new[] { _stop, contextState.AsyncWaitHandle }))
                    {
                        return;
                    }
                    //connected = false;
                    httpErrors = 0;
                }
                catch (HttpException se)
                {
                    httpErrors++;
                    //OnFault("The http server throws SocketException: ", se);
                    ExecFault(context, "The http server throws SocketException: " + se.Message);
                    if (httpErrors > MAX_ERRORS)
                    {
                        Log.Error("The http server shutdown after {0} errors ", MAX_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //OnFault("The http server throws the error: ", ex);
                    ExecFault(context, "The http server throws Exception: " + ex.Message);
                }
            }

        }

        private void ContextCallback(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try
                {
                    //ProcessRequest(context);
                    Task task = Task.Factory.StartNew(() => ProcessRequest(context));
                    {
                        task.Wait(_timeout);
                        if (task.IsCompleted)
                        {
                            //
                        }
                    }
                    task.TryDispose();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        private void Run()
        {
            try
            {
                _workers = new Thread[_maxThread];
                _queue = new Queue<HttpListenerContext>();
                _listenerThread = new Thread(HandleRequests);

                _listener.Start();

                _listenerThread.Start();

                for (int i = 0; i < _workers.Length; i++)
                {
                    _workers[i] = new Thread(Worker);
                    _workers[i].Start();
                }

                Log.Info("HttpServer started: {0}", Settings.HostName);

            }
            catch (Exception ex)
            {
                OnFault("The http server listener on Start throws the error: ", ex);
                return;
            }

        }

        void ProcessRequest(HttpListenerContext context)
        {
            try
            {

                HttpRequestInfo requestInfo = HttpRequestInfo.Read(context.Request);

                TRequest req = ReadRequest(requestInfo);

                if (requestInfo.BodyType == HttpBodyType.Body)
                {
                    var res = ExecTransStream(req);
                    WriteResponse(context, res);
                }
                else
                {
                    var response = ExecString(req);
                    WriteResponse(context, response);
                }
                httpErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                ExecFault(context, "The http server throws Exception: " + ex.Message);
                //OnFault("The http server async ProcessIncomingData throws the error: ", ex);
            }
        }

        #endregion
    }

  
/*
    /// <summary>
    /// Represent a http server listner.
    /// </summary>
    public abstract class HttpServer : HttpServer<HttpMessage>
    {
         #region ctor


        /// <summary>
        /// Initialize a new instance of <see cref="HttpServer"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected HttpServer(string configHost)
            : base(configHost)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="HttpServer"/> with given <see cref="HttpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected HttpServer(HttpSettings settings)
            : base(settings)
        {

        }
        #endregion

        #region abstract methods

        /// <summary>
        /// Read Request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected override HttpMessage ReadRequest(HttpRequestInfo request)
        {
            return HttpMessage.ReadRequest(request);
        }

        #endregion

    }
 */  
}
