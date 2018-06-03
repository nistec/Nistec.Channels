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
using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using Nistec.Runtime;
using Nistec.Generic;
using Nistec.IO;
using System.Security.Principal;
using Nistec.Logging;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Nistec.Channels
{

    /// <summary>
    /// Represent a pipe server listner
    /// </summary>
    public abstract class PipeServer<TRequest> where TRequest : ITransformMessage
    {
        #region membrs
        private int numThreads;
        private bool Listen;
        private bool Initilize = false;
        //private bool IsAsync = true;
        Thread[] servers;
        static object mlock = new object();
        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        #endregion

        #region settings
        /// <summary>
        /// Get or Set the host name.
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// Get or Set the pipe name.
        /// </summary>
        public string PipeName { get; set; }
        /// <summary>
        /// Get or Set the pipe direction
        /// </summary>
        public PipeDirection PipeDirection { get; set; }
        /// <summary>
        /// Get or Set the pipe options.
        /// </summary>
        public PipeOptions PipeOptions { get; set; }
        /// <summary>
        /// Get or Set the max server connections.
        /// </summary>
        public int MaxServerConnections { get; set; }
        /// <summary>
        /// Get or Set the max allowed server instances
        /// </summary>
        public int MaxAllowedServerInstances { get; set; }
        /// <summary>
        /// Get or Set the verify pipe for security.
        /// </summary>
        public string VerifyPipe { get; set; }
        /// <summary>
        /// Get or Set the connection timeout.
        /// </summary>
        public uint ConnectTimeout { get; set; }
        //public bool IsApi { get; set; }
        /// <summary>
        /// Get or Set the in buffer size in bytes.
        /// </summary>
        public int ReceiveBufferSize { get; set; }
        /// <summary>
        /// Get or Set the out buffer size in bytes.
        /// </summary>
        public int SendBufferSize { get; set; }
        /// <summary>
        /// Local Server name
        /// </summary>
        public const string ServerName = ".";
        /// <summary>
        /// Get the full pipe name.
        /// </summary>
        public string FullPipeName { get { return @"\\" + ServerName + @"\pipe\" + PipeName; } }
        /// <summary>
        ///  Get or Set Indicates that the channel can be used for asynchronous reading and writing..
        /// </summary>
        public bool IsAsync { get; set; }
        //public object QLogger { get; private set; }


        #endregion

        #region ctor

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="loadFromSettings"></param>
        protected PipeServer(string hostName, bool loadFromSettings)
        {
            
            //PipeName = name;
            //VerifyPipe = name;

            PipeSettings settings = new PipeSettings(hostName, true, loadFromSettings);
            this.HostName = settings.HostName;
            this.PipeName = settings.PipeName;
            this.ConnectTimeout = settings.ConnectTimeout;
            this.ReceiveBufferSize = settings.ReceiveBufferSize;
            this.SendBufferSize = settings.SendBufferSize;
            this.PipeDirection = settings.PipeDirection;
            this.PipeOptions = settings.PipeOptions;
            this.VerifyPipe = settings.VerifyPipe;
            this.MaxAllowedServerInstances = settings.MaxAllowedServerInstances;
            this.MaxServerConnections = settings.MaxServerConnections;
            this.IsAsync = settings.IsAsync;
        }

        /// <summary>
        /// Constractor with settings
        /// </summary>
        /// <param name="settings"></param>
        protected PipeServer(PipeSettings settings)
        {
            this.HostName = settings.HostName;
            this.PipeName = settings.PipeName;
            this.ConnectTimeout = settings.ConnectTimeout;
            this.ReceiveBufferSize = settings.ReceiveBufferSize;
            this.SendBufferSize = settings.SendBufferSize;
            this.PipeDirection = settings.PipeDirection;
            this.PipeOptions = settings.PipeOptions;
            this.VerifyPipe = settings.VerifyPipe;
            this.MaxAllowedServerInstances = settings.MaxAllowedServerInstances;
            this.MaxServerConnections = settings.MaxServerConnections;
            this.IsAsync = settings.IsAsync;

        }
        #endregion

        #region Initilize

        private void Init()
        {
            if (Initilize)
                return;
            numThreads = MaxServerConnections;
            servers = new Thread[numThreads];
            //IsAsync = true;
            for (int i = 0; i < numThreads; i++)
            {
                if (IsAsync)
                    servers[i] = new Thread(RunAsync);
                else
                    servers[i] = new Thread(Run);

                //servers[i] = new Thread(RunTask);
                servers[i].Name = "PipeServer_" + i.ToString();
                servers[i].IsBackground = true;
                servers[i].Start();
            }
            Initilize = true;
            OnLoad();

            Log.Info("Waiting for client connection...\n");

        }
        /// <summary>
        /// On load.
        /// </summary>
        protected virtual void OnLoad()
        {

        }
        /// <summary>
        /// On start.
        /// </summary>
        protected virtual void OnStart()
        {

        }
        /// <summary>
        /// On stop.
        /// </summary>
        protected virtual void OnStop()
        {

        }
        /// <summary>
        /// On error.
        /// </summary>
        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
        }

        /// <summary>
        /// Start pipe server listner.
        /// </summary>
        public void Start()
        {
            //IsAsync = isAsync;
            Listen = true;
            Init();
            OnStart();
        }
        /// <summary>
        /// Stop pipe server listner.
        /// </summary>
        public void Stop()
        {
            Listen = false;
            OnStop();
        }

        #endregion

        #region Read/Write
        /// <summary>
        /// Read request
        /// </summary>
        /// <param name="pipeServer"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(NamedPipeServerStream pipeServer);


        /// <summary>
        /// Write response
        /// </summary>
        /// <param name="pipeServer"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(NamedPipeServerStream pipeServer, TransStream bResponse)
        {

            if (bResponse == null)
            {
                return;
            }

            //BinaryWriter bw = new BinaryWriter(pipeServer);
            //bw.Write(bResponse.ToArray(), 0, cbResponse);
            //bw.Flush();

            var ns= bResponse.GetStream();
            if(ns==null)
            {
                return;
            }
            pipeServer.Write(ns.ToArray(), 0, ns.iLength);

            pipeServer.Flush();

        }

        /// <summary>
        /// Exec Requset
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TransStream ExecRequset(TRequest request);

        /// <summary>
        /// Occured when client is connected.
        /// </summary>
        protected virtual void OnClientConnected()
        {
            //Console.WriteLine("Debuger-OnClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
        }

        #endregion

        #region Run

        PipeSecurity pipeSecurity;

        PipeSecurity GetPipeSecurity()
        {
            if(pipeSecurity==null)
            {
                var ps = new PipeSecurity();

                PipeAccessRule aceClients = new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), // or some other group defining the allowed clients
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow);

                ps.AddAccessRule(aceClients);

                PipeAccessRule aceOwner = new PipeAccessRule(
                    WindowsIdentity.GetCurrent().Owner,
                    PipeAccessRights.FullControl,
                    AccessControlType.Allow);
                ps.AddAccessRule(aceOwner);


                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var rule = new PipeAccessRule(sid, PipeAccessRights.ReadWrite,
                                              AccessControlType.Allow);
                ps.AddAccessRule(rule);

                pipeSecurity = ps;
            }
            return pipeSecurity;
        }

        NamedPipeServerStream CreatePipeAccessControl()
        {

            // Fix up the DACL on the pipe before opening the listener instance
            // This won't disturb the SACL containing the mandatory integrity label
            NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                        PipeName,                                               // The unique pipe name.
                        PipeDirection,                                          // The pipe is duplex
                        NamedPipeServerStream.MaxAllowedServerInstances,        // MaxAllowedServerInstances
                        PipeTransmissionMode.Message,                           // Byte|Message-based communication
                        //PipeOptions.Asynchronous | PipeOptions.WriteThrough,  
                        PipeOptions,                                            // No additional parameters
                        ReceiveBufferSize,                                      // Input buffer size
                        SendBufferSize,                                         // Output buffer size
                        GetPipeSecurity(),                                      // Pipe security attributes
                        HandleInheritability.None,                              // Not inheritable
                        PipeAccessRights.ChangePermissions);

            return pipeServer;
        }

        /// <summary>
        /// Use the pipe classes in the System.IO.Pipes namespace to create the 
        /// named pipe. This solution is recommended.
        /// </summary>
        private void Run()
        {
            NamedPipeServerStream pipeServer = null;
            TRequest message = default(TRequest);
            //bool connected = false;
            Console.WriteLine("{0} Pipe server start listen Thread<{1}>", PipeName, Thread.CurrentThread.ManagedThreadId);

            while (Listen)
            {
                try
                {
                    lock (mlock)
                    {
                        pipeServer = CreatePipeAccessControl();
                    }

                    // Wait for the client to connect.
                    pipeServer.WaitForConnection();
                    //connected = true;

                    OnClientConnected();

                    using (message = ReadRequest(pipeServer))
                    {
                        TransStream res = ExecRequset(message);

                        if (message.IsDuplex)
                            WriteResponse(pipeServer, res);
                    }

                    // Flush the pipe to allow the client to read the pipe's contents 
                    // before disconnecting. Then disconnect the client's connection.
                    pipeServer.WaitForPipeDrain();
                    pipeServer.Disconnect();
                    //connected = false;
                }
                catch (Exception ex)
                {
                    OnFault("The pipe server throws the error: " , ex);
                }
                finally
                {
                    Close(pipeServer);
                }
                Thread.Sleep(10);
            }
            Console.WriteLine("{0} Pipe server stope listen Thread<{1}>", PipeName, Thread.CurrentThread.ManagedThreadId);

        }


        /// <summary>
        /// Use the pipe classes in the System.IO.Pipes namespace to create the 
        /// named pipe. This solution is recommended.
        /// </summary>
        private void RunTask()
        {
            NamedPipeServerStream pipeServer = null;
            //bool connected = false;
            
            //const string ResponseMessage = "Default response from server\0";
            Console.WriteLine("{0} Pipe server start listen Thread<{1}>", PipeName, Thread.CurrentThread.ManagedThreadId);

            while (Listen)
            {
                try
                {

                    pipeServer = CreatePipeAccessControl();

                    // Wait for the client to connect.
                    //Netlog.Info("Waiting for the client's connection...");
                    pipeServer.WaitForConnection();
                    //connected = true;

                    OnClientConnected();

                    int timeout = (int)this.ConnectTimeout;

                    Task task = Task.Factory.StartNew(() =>
                    {

                        try
                        {

                            using (var message = ReadRequest(pipeServer))
                            {
                                var res = ExecRequset(message);
                                if (message.IsDuplex)
                                    WriteResponse(pipeServer, res);
                            }

                            // Flush the pipe to allow the client to read the pipe's contents 
                            // before disconnecting. Then disconnect the client's connection.
                            pipeServer.WaitForPipeDrain();
                            pipeServer.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            OnFault("The pipe server throws the error: ", ex);
                        }
                        finally
                        {
                            Close(pipeServer);
                        }

                    });
                    {
                        task.Wait(timeout);
                    }
                    task.TryDispose();

                }
                catch (Exception ex)
                {
                    OnFault("The pipe server throws the error: ", ex);

                    Close(pipeServer);
                }

                Thread.Sleep(10);
            }
            Console.WriteLine("{0} Pipe server stope listen Thread<{1}>", PipeName, Thread.CurrentThread.ManagedThreadId);

        }


        void Close(NamedPipeServerStream pipeServer,TRequest message=default(TRequest))
        {
            try
            {
                if (pipeServer != null)
                {
                    if (pipeServer.IsConnected)
                    {
                        pipeServer.Disconnect();
                    }
                    pipeServer.Close();
                    pipeServer = null;
                }
                if (message != null)
                {
                    message.Dispose();
                    message = default(TRequest);
                }
            }
            catch(Exception ex)
            {
                OnFault("Close pipeServer error ", ex);
            }
        }
        void CloseMessage(TRequest message)
        {
            try
            {
                if (message != null)
                {
                    message.Dispose();
                    message = default(TRequest);
                }
            }
            catch (Exception ex)
            {
                OnFault("Close pipeServer error ", ex);
            }
        }


        #region ServerCom & AutoResetEvent

        private class ServerCom : IDisposable
        {
            public long Uid;
            public NamedPipeServerStream ServerStream;
            public AutoResetEvent ResetEvent;

            public ServerCom(NamedPipeServerStream serverStream)
            {
                Uid = UUID.UniqueId();
                ResetEvent = new AutoResetEvent(false);
                ServerStream = serverStream;
            }

            public void WaitOne()
            {
                ResetEvent.WaitOne();
            }
            public void Set()
            {
                ResetEvent.Set();
            }
            public void Dispose()
            {
                try
                {
                    if (ResetEvent != null)
                    {
                        ResetEvent.Close();
                        ResetEvent.Dispose();
                        ResetEvent = null;
                    }

                    if (ServerStream != null)
                    {
                        if (ServerStream.IsConnected)
                        {
                            ServerStream.Disconnect();
                        }
                        ServerStream.Close();
                        ServerStream = null;
                    }
                }
                catch
                {
                    //do nothing      
                }
            }
        }
        #endregion

        /// <summary>
        /// Use the pipe classes in the System.IO.Pipes namespace to create the 
        /// named pipe. This solution is recommended.
        /// </summary>
        private void RunAsync()
        {
            NamedPipeServerStream pipeServerAsync = null;
            //bool connected = false;
            Console.WriteLine("{0} Pipe server async start listen Thread<{1}>", PipeName, Thread.CurrentThread.ManagedThreadId);

            while (Listen)
            {
                ServerCom server = null;

                try
                {
                   pipeServerAsync = CreatePipeAccessControl();
                   server = new ServerCom(pipeServerAsync);

                    // Wait for the client to connect.
                    AsyncCallback myCallback = new AsyncCallback(WaitForConnectionAsyncCallback);
                    IAsyncResult asyncResult = pipeServerAsync.BeginWaitForConnection(myCallback, server);
                   
                    server.WaitOne();
                }
                catch (Exception ex)
                {
                    OnFault("The pipe sync server throws the error: " , ex);
                }
                finally
                {
                    if (server != null)
                    {
                        server.Dispose();
                    }
                }
                Thread.Sleep(10);
            }
            Console.WriteLine("{0} Pipe server async stop listen Thread<{1}>", PipeName, Thread.CurrentThread.ManagedThreadId);
        }


        private void WaitForConnectionAsyncCallback(IAsyncResult result)
        {
            //bool connected = false;
            ServerCom server = null;
            NamedPipeServerStream pipeServerAsync = null;
            TRequest message = default(TRequest);
            try
            {
                server = (ServerCom)result.AsyncState;

                if (server != null && server.ServerStream !=null)
                {

                    pipeServerAsync = server.ServerStream;

                    pipeServerAsync.EndWaitForConnection(result);

                    //connected = true;

                    OnClientConnected();

                    message = ReadRequest(pipeServerAsync);

                    TransStream res = ExecRequset(message);
                    if (message.IsDuplex)
                        WriteResponse(pipeServerAsync, res);

                    pipeServerAsync.WaitForPipeDrain();
                }
                //Console.WriteLine("Debuger-RunAsyncCallback. end: " + server.Uid.ToString()); 

            }
            catch (OperationCanceledException oex)
            {
                OnFault("Pipe server error, The pipe was canceled: " , oex);
            }
            catch (Exception ex)
            {
                OnFault("Pipe server error: " , ex);
            }
            finally
            {
                CloseMessage(message);
                
                if (server != null)
                {
                    server.Set();
                }
            }
            //Thread.Sleep(10);
        }

        #endregion
    }

    

}
