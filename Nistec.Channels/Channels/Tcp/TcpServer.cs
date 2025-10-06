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
using Nistec.Runtime;
using System;
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
#pragma warning disable CS1591
namespace Nistec.Channels.Tcp
{

    public class TcpThreadSoketServer
    {
        #region members/ctor

        private Socket _serverSocket;
        protected bool _listen = false;
        protected bool Initilized = false;
        protected bool IsAsync = true;
        //static readonly ConcurrentDictionary<string, Socket> Clients = new ConcurrentDictionary<string, Socket>();


        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpThreadSoketServer()
        {
            Settings = new TcpSettings();
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="allowedIps"></param>
        protected TcpThreadSoketServer(string hostAddress, int port, string allowedIps)
        {
            Settings = new TcpSettings(hostAddress, port);
            Settings.AllowedIp = allowedIps;
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpThreadSoketServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpThreadSoketServer(TcpSettings settings)
        {
            Settings = settings;
            IsAsync = Settings.IsAsync;
            Log = settings.Log;
        }
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

        #region Tcp Server
        private bool IsAllowedIP(string ip)
        {
            return Array.Exists(Settings.AllowedListIp, allowedIp => allowedIp == ip);
        }
        Thread[] _workers;
        int WorkerCount = 1;
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        protected void StartServer()
        {
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
                _serverSocket.Listen(100); // Increase backlog queue
                WorkerCount = Settings.MaxServerConnections;
                OnInfo($"Server started on port {Settings.Port}...");
                Initilized = true;
                OnStart();
                //Task.Run(() => RunServer());

                _workers = new Thread[WorkerCount];
                ThreadStart threadWorker = new ThreadStart(RunServer);
                for (int i = 0; i < WorkerCount; i++)
                {
                    _workers[i] = new Thread(new ThreadStart(threadWorker));
                    _workers[i].IsBackground = true;
                    _workers[i].Start();
                }
            }
            catch (Exception ex)
            {
                OnFault($"TcpSoketServer.StartServerAsync error : {ex.Message}");
            }
        }

        protected void StartServerAsync()
        {
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
                _serverSocket.Listen(100); // Increase backlog queue

                OnInfo($"Server started on port {Settings.Port}...");
                Initilized = true;
                OnStart();
                //await RunServerAsync();
                _workers = new Thread[WorkerCount];
                ThreadStart threadWorker = new ThreadStart(RunServerAsync);
                for (int i = 0; i < WorkerCount; i++)
                {
                    _workers[i] = new Thread(new ThreadStart(threadWorker));
                    _workers[i].IsBackground = true;
                    _workers[i].Start();
                }
            }
            catch (Exception ex)
            {
                OnFault($"TcpSoketServer.StartServerAsync error : {ex.Message}");
            }
        }

        protected virtual bool ReadyToAccept()
        {
            return true;
        }

        protected void RunServer()
        {

            OnInfo($"Server RunServerAsync on port {Settings.Port}...");

            while (_listen)
            {
                try
                {

                    while (!ReadyToAccept())
                    {
                        Task.Delay(1000);
                    }

                    //using (Socket clientSocket = await _serverSocket.AcceptAsync())
                    Socket clientSocket = _serverSocket.Accept();
                    {
                        clientSocket.ReceiveTimeout = -1;// Settings.ReadTimeout;
                        clientSocket.SendTimeout = Settings.ConnectTimeout;
                        string clientIP = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();

                        if (!IsAllowedIP(clientIP))
                        {
                            OnInfo("Rejected connection from {clientIP}");
                            clientSocket.Shutdown(SocketShutdown.Both);
                            clientSocket.Close();
                            //continue;
                        }
                        else
                        {
                            //OnInfo($"Client connected! {clientIP}");
                            //string clientKey = UUID.NewId();
                            //Clients.TryAdd(clientKey, clientSocket);
                            //_ = Task.Run( async () => await HandleClientAsync(clientSocket)); // Handle client in a separate task
                            HandleClient(clientSocket);
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    OnFault($"TcpSoketServer.RunServerAsync error : {ex.Message}");
                }
            }
        }
        protected async void RunServerAsync()
        {

            OnInfo($"Server RunServerAsync on port {Settings.Port}...");

            while (_listen)
            {
                try
                {
                    while (!ReadyToAccept())
                    {
                        await Task.Delay(1000);
                    }

                    //using (Socket clientSocket = await _serverSocket.AcceptAsync())
                    Socket clientSocket = await _serverSocket.AcceptAsync();
                    {
                        clientSocket.ReceiveTimeout = -1;// Settings.ReadTimeout;
                        clientSocket.SendTimeout = Settings.ConnectTimeout;
                        string clientIP = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();

                        if (!IsAllowedIP(clientIP))
                        {
                            OnInfo("Rejected connection from {clientIP}");
                            clientSocket.Shutdown(SocketShutdown.Both);
                            clientSocket.Close();
                            //continue;
                        }
                        else
                        {
                            //OnInfo($"Client connected! {clientIP}");
                            //string clientKey = UUID.NewId();
                            //Clients.TryAdd(clientKey, clientSocket);
                            await HandleClientAsync(clientSocket);
                            //_ = Task.Run(async () => await HandleClientAsync(clientSocket)); // Handle client in a separate task
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    OnFault($"TcpSoketServer.RunServerAsync error : {ex.Message}");
                }
            }
        }
        protected virtual void HandleClient(Socket clientSocket)
        {
            //IDataStream trans;
            byte[] trans;

            try
            {
                byte[] buffer = new byte[4096]; // Chunk size
                int bytesRead;
                using (var stream = new System.IO.MemoryStream())
                {

                    while ((bytesRead = clientSocket.Receive(buffer, SocketFlags.None)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break; // End of message
                    }
                    //byte[] receivedData = stream.ToArray();
                    //OnInfo($"Received {stream.Length} bytes.");
                    trans = stream.ToArray();
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());

                    //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    //stream.Write(buffer, 0, bytesRead);
                    //OnInfo($"Received {bytesRead} bytes.");
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());
                }

                //if (trans.TransType == TransType.None)
                //{
                //    Task.Delay(1000);
                //}
                var response = ServerHandle(trans);
                if (response == null || response.Length == 0)//.IsEmpty)//.TransType == TransType.None)
                {
                    Task.Delay(1000);
                    //OnFault($"Client Response.IsEmpty");
                }
                else
                {
                    OnInfo($"Received {trans.Length} bytes.");
                    clientSocket.Send(response, SocketFlags.None);//.DataStream()
                    OnInfo($"Sent {bytesRead} bytes...");
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling client: {ex.Message} , Trace: {ex.StackTrace}");
            }
            finally
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                //OnInfo("Client disconnected.");
            }
        }

        protected virtual async Task HandleClientAsync(Socket clientSocket)
        {
            //IDataStream trans;
            byte[] trans;
            try
            {
                byte[] buffer = new byte[4096]; // Chunk size
                int bytesRead;
                using (var stream = new System.IO.MemoryStream())
                {

                    while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break; // End of message
                    }
                    //byte[] receivedData = stream.ToArray();
                    //OnInfo($"Received {stream.Length} bytes.");
                    trans = stream.ToArray();
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());


                    //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    //stream.Write(buffer, 0, bytesRead);
                    //OnInfo($"Received {bytesRead} bytes.");
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());
                }

                //if (trans.TransType == TransType.None)
                //{
                //    await Task.Delay(1000);
                //}

                var response = await ServerHandleAsync(trans);
                //var response = ServerHandle(trans);// await ServerHandleAsync(trans);
                if (response == null || response.Length == 0)//.IsEmpty)//.TransType == TransType.None)
                {
                    await Task.Delay(1000);
                    //OnFault($"Client Response.IsEmpty");
                }
                else
                {
                    OnInfo($"Received {trans.Length} bytes.");
                    await clientSocket.SendAsync(new ArraySegment<byte>(response), SocketFlags.None);//.DataStream()
                    OnInfo($"Sent {bytesRead} bytes...");
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling async client: {ex.Message} , Trace: {ex.StackTrace}");
            }
            finally
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                //OnInfo("Client disconnected.");
            }
        }

        #endregion
              
        #region override

        protected virtual byte[] ServerHandle(byte[] bytes)
        {
            return null;
        }

        protected virtual async Task<byte[]> ServerHandleAsync(byte[] bytes)
        {
            return await Task.Run(() => TransStream.EmptyStream());
        }

        //protected virtual IDataStream ReceivedData(byte[] bytes)
        //{
        //    return TransStream.FromBytes(bytes);//binary
        //}

        #endregion

        #region start/stop ang logs

        protected virtual void OnInfo(string message)
        {
            Console.WriteLine(message);
        }
        protected virtual void OnFault(string message)
        {
            Console.WriteLine(message);
        }

        protected virtual void OnStart()
        {
            Console.WriteLine("TcpSoketServer strted");
        }
        protected virtual void OnStop()
        {
            Console.WriteLine("TcpSoketServer stoped");
        }
        //protected virtual void OnLoad()
        //{
        //    Console.WriteLine("TcpSoketServer loaded");
        //}

        public virtual void Start()
        {
            if (_listen)
            {
                OnInfo("Server is already running.");
                return;
            }

            _listen = true;

            if (IsAsync)
                StartServerAsync();// Task.Run(() => StartServerAsync());// StartServerAsync().ConfigureAwait(false);//.GetAwaiter().GetResult();
            else
                StartServer();
        }

        public virtual void Stop()
        {
            _listen = false;
            OnStop();
            //StartServerAsync().GetAwaiter().GetResult();
        }
        #endregion

        #region legacy staff
        /*
       private async Task HandleClientStreamAsync(Socket clientSocket)
       {
           try
           {
               byte[] buffer = new byte[8192]; // Optimized buffer size
               int bytesRead;
               bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
               //while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
               {
                   OnInfo($"Received {bytesRead} bytes...");

                   var netstream = new NetStream(buffer, 0, bytesRead);
                   var response = await ServerHandle(netstream);
                   if (response== null)
                   {
                       Thread.Sleep(1000);
                   }
                   else
                   {
                       //TransBinary response = await ServerHandle(tb);
                       await clientSocket.SendAsync(new ArraySegment<byte>(response.ToArray()), SocketFlags.None);
                       //await clientSocket.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                       OnInfo($"Sent {bytesRead} bytes...");
                   }
               }
           }
           catch (Exception ex)
           {
               OnFault($"Error handling client: {ex.Message}");
           }
           finally
           {
               clientSocket.Close();
               OnInfo("Client disconnected.");
           }
       }

       private async Task ___HandleClientAsync(Socket clientSocket)
       {
           try
           {
               byte[] buffer = new byte[8192]; // Optimized buffer size
               int bytesRead;
               //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
               using (var netstream = new NetStream())
               {
                   while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                   {
                       OnInfo($"Received {bytesRead} bytes...");
                       netstream.Write(buffer, 0, bytesRead);
                   }
               }
               var tb = new TransBinary(new NetStream(buffer, 0, bytesRead));
               if (tb.TransType == TransType.None)
               {
                   Thread.Sleep(1000);
               }
               var response = await ServerHandle(tb);
               if (response.TransType == TransType.None)
               {
                   Thread.Sleep(1000);
               }
               else
               {
                   //TransBinary response = await ServerHandle(tb);
                   await clientSocket.SendAsync(new ArraySegment<byte>(response.BodyStream), SocketFlags.None);
                   //await clientSocket.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                   OnInfo($"Sent {bytesRead} bytes...");
               }
           }
           catch (Exception ex)
           {
               OnFault($"Error handling client: {ex.Message}");
           }
           finally
           {
               clientSocket.Close();
               OnInfo("Client disconnected.");
           }
       }


       private async byte[] ServerHandle(TransBinary request)
       {
           //TransBinary request = new TransBinary(bytes.Array);
           OnInfo($"Server Request Received {request.TransType} bytes...");
           var body = request.ReadBody();
           OnInfo($"Server Request Body {body}");

           //Server business process
           OnInfo($"Server business process...");
           //TransBinary response = await ExecRequsetAsync(request);

           TransBinary tb = new TransBinary("ok", "Ack", TransType.Text);
           OnInfo($"Server Response send");

           return tb.ToStream().ToArray();
       }
      


        #region Read/Write Async

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<TransBinary> ServerHandle(TransBinary request)
        {
            while(!Initilized)
            {
                Thread.Sleep(1000);
            }
            //return new TransBinary("NA", TransType.None);
            return await Task.Run(() =>
            {
                return new TransBinary("NA", "NA", TransType.None);
            });
        }

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<NetStream> ServerHandle(NetStream request)
        {
            while (!Initilized)
            {
                Thread.Sleep(1000);
            }
            //return new TransBinary("NA", TransType.None);
            return await Task.Run(() =>
            {
                return new NetStream(new byte[] {0});
            });
        }

        ///// <summary>
        ///// Write response to client.
        ///// </summary>
        ///// <param name="response"></param>
        //protected virtual async Task WriteResponseAsync(TransBinary response)
        //{
        //    if (response == null)
        //    {
        //        return;
        //    }

        //    var bytes = bResponse.GetBytes();
        //    if (bytes == null || bytes.Length == 0)
        //    {
        //        return;
        //    }
        //    await stream.WriteAsync(bytes, 0, bytes.Length);
        //}

        #endregion

   */
        #endregion

    }

    public class TcpSoketServer
    {
        #region members/ctor
        //protected readonly string MsgType;
        //protected int Port;
        //protected string[] AllowedIPs;
        private Socket _serverSocket;
        protected bool _listen= false;
        protected bool Initilized=false;
        protected bool IsAsync = true;
        //static readonly ConcurrentDictionary<string, Socket> Clients = new ConcurrentDictionary<string, Socket>();

     
        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpSoketServer()
        {
            Settings = new TcpSettings();
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="allowedIps"></param>
        protected TcpSoketServer(string hostAddress, int port, string allowedIps)
        {
            Settings = new TcpSettings(hostAddress, port);
            Settings.AllowedIp= allowedIps;
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpSoketServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpSoketServer(TcpSettings settings)
        {
            Settings = settings;
            IsAsync = Settings.IsAsync;
            Log = settings.Log;
        }
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

        #region Tcp Server
        private bool IsAllowedIP(string ip)
        {
            return Array.Exists(Settings.AllowedListIp, allowedIp => allowedIp == ip);
        }

        protected void StartServer()
        {
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
                _serverSocket.Listen(100); // Increase backlog queue

                OnInfo($"Server started on port {Settings.Port}...");
                Initilized = true;
                OnStart();
                Task.Run(() => RunServer());
            }
            catch (Exception ex)
            {
                OnFault($"TcpSoketServer.StartServerAsync error : {ex.Message}");
            }
        }

        protected async Task StartServerAsync()
        {
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
                _serverSocket.Listen(100); // Increase backlog queue

                OnInfo($"Server started on port {Settings.Port}...");
                Initilized = true;
                OnStart();
                await RunServerAsync();

            }
            catch (Exception ex)
            {
                OnFault($"TcpSoketServer.StartServerAsync error : {ex.Message}");
            }
        }

        protected virtual bool ReadyToAccept()
        {
            return true;
        }

        protected void RunServer()
        {

            OnInfo($"Server RunServerAsync on port {Settings.Port}...");

            while (_listen)
            {
                try
                {

                    while (!ReadyToAccept())
                    {
                        Task.Delay(1000);
                    }

                    //using (Socket clientSocket = await _serverSocket.AcceptAsync())
                    Socket clientSocket = _serverSocket.Accept();
                    {
                        clientSocket.ReceiveTimeout = Settings.ReadTimeout;
                        clientSocket.SendTimeout = Settings.ConnectTimeout;
                        string clientIP = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();

                        if (!IsAllowedIP(clientIP))
                        {
                            OnInfo("Rejected connection from {clientIP}");
                            clientSocket.Close();
                            continue;
                        }
                        //OnInfo($"Client connected! {clientIP}");
                        //string clientKey = UUID.NewId();
                        //Clients.TryAdd(clientKey, clientSocket);
                        _ = Task.Run(() => HandleClient(clientSocket)); // Handle client in a separate task
                    }
                    Task.Delay(100);
                }
                catch (Exception ex)
                {
                    OnFault($"TcpSoketServer.RunServerAsync error : {ex.Message}");
                }
            }
        }
        protected async Task RunServerAsync()
        {

            OnInfo($"Server RunServerAsync on port {Settings.Port}...");

            while (_listen)
            {
                try
                {
                    while (!ReadyToAccept())
                    {
                       await Task.Delay(1000);
                    }

                    //using (Socket clientSocket = await _serverSocket.AcceptAsync())
                    Socket clientSocket = await _serverSocket.AcceptAsync();
                    {
                        clientSocket.ReceiveTimeout = Settings.ReadTimeout;
                        clientSocket.SendTimeout = Settings.ConnectTimeout;
                        string clientIP = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();

                        if (!IsAllowedIP(clientIP))
                        {
                            OnInfo("Rejected connection from {clientIP}");
                            clientSocket.Close();
                            continue;
                        }
                        //OnInfo($"Client connected! {clientIP}");
                        //string clientKey = UUID.NewId();
                        //Clients.TryAdd(clientKey, clientSocket);
                        _ = Task.Run(() => HandleClientAsync(clientSocket)); // Handle client in a separate task
                    }
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    OnFault($"TcpSoketServer.RunServerAsync error : {ex.Message}");
                }
            }
        }
        protected virtual void HandleClient(Socket clientSocket)
        {
            //IDataStream trans;
            byte[] trans;

            try
            {
                byte[] buffer = new byte[4096]; // Chunk size
                int bytesRead;
                using (var stream = new System.IO.MemoryStream())
                {

                    while ((bytesRead = clientSocket.Receive(buffer, SocketFlags.None)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break; // End of message
                    }
                    //byte[] receivedData = stream.ToArray();
                    //OnInfo($"Received {stream.Length} bytes.");
                    trans = stream.ToArray();
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());

                    //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    //stream.Write(buffer, 0, bytesRead);
                    //OnInfo($"Received {bytesRead} bytes.");
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());
                }

                //if (trans.TransType == TransType.None)
                //{
                //    Task.Delay(1000);
                //}
                var response = ServerHandle(trans);
                if (response == null || response.Length==0)//.IsEmpty)//.TransType == TransType.None)
                {
                    Task.Delay(1000);
                    //OnFault($"Client Response.IsEmpty");
                }
                else
                {
                    OnInfo($"Received {trans.Length} bytes.");
                    clientSocket.Send(response, SocketFlags.None);//.DataStream()
                    OnInfo($"Sent {bytesRead} bytes...");
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling client: {ex.Message} , Trace: {ex.StackTrace}");
            }
            finally
            {
                clientSocket.Close();
                //OnInfo("Client disconnected.");
            }
        }

        protected virtual async Task HandleClientAsync(Socket clientSocket)
        {
            //IDataStream trans;
            byte[] trans;
            try
            {
                byte[] buffer = new byte[4096]; // Chunk size
                int bytesRead;
                using (var stream = new System.IO.MemoryStream())
                {

                    while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break; // End of message
                    }
                    //byte[] receivedData = stream.ToArray();
                    //OnInfo($"Received {stream.Length} bytes.");
                    trans = stream.ToArray();
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());


                    //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    //stream.Write(buffer, 0, bytesRead);
                    //OnInfo($"Received {bytesRead} bytes.");
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());
                }

                //if (trans.TransType == TransType.None)
                //{
                //    await Task.Delay(1000);
                //}

                var response = await ServerHandleAsync(trans);
                //var response = ServerHandle(trans);// await ServerHandleAsync(trans);
                if (response == null || response.Length==0)//.IsEmpty)//.TransType == TransType.None)
                {
                    await Task.Delay(1000);
                    //OnFault($"Client Response.IsEmpty");
                }
                else
                {
                    OnInfo($"Received {trans.Length} bytes.");
                    await clientSocket.SendAsync(new ArraySegment<byte>(response), SocketFlags.None);//.DataStream()
                    OnInfo($"Sent {bytesRead} bytes...");
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling async client: {ex.Message} , Trace: {ex.StackTrace}");
            }
            finally
            {
                clientSocket.Close();
                //OnInfo("Client disconnected.");
            }
        }

        #endregion

        #region override

        protected virtual byte[] ServerHandle(byte[] bytes)
        {
            return null;
        }

        protected virtual async Task<byte[]> ServerHandleAsync(byte[] bytes)
        {
            return await Task.Run(() => TransStream.EmptyStream());
        }

        //protected virtual IDataStream ReceivedData(byte[] bytes)
        //{
        //    return TransStream.FromBytes(bytes);//binary
        //}

        #endregion

        #region start/stop ang logs

        protected virtual void OnInfo(string message)
        {
            Console.WriteLine(message);
        }
        protected virtual void OnFault(string message)
        {
            Console.WriteLine(message);
        }

        protected virtual void OnStart()
        {
            Console.WriteLine("TcpSoketServer strted");
        }
        protected virtual void OnStop()
        {
            Console.WriteLine("TcpSoketServer stoped");
        }
        //protected virtual void OnLoad()
        //{
        //    Console.WriteLine("TcpSoketServer loaded");
        //}

        public virtual void Start()
        {
            if (_listen)
            {
                OnInfo("Server is already running.");
                return;
            }

            _listen = true;

            if (IsAsync)
                Task.Run(async () => await StartServerAsync());// StartServerAsync().ConfigureAwait(false);//.GetAwaiter().GetResult();
            else
                StartServer();
        }

        public virtual void Stop()
        {
            _listen = false;
            OnStop();
            //StartServerAsync().GetAwaiter().GetResult();
        }
        #endregion

        #region legacy staff
        /*
       private async Task HandleClientStreamAsync(Socket clientSocket)
       {
           try
           {
               byte[] buffer = new byte[8192]; // Optimized buffer size
               int bytesRead;
               bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
               //while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
               {
                   OnInfo($"Received {bytesRead} bytes...");

                   var netstream = new NetStream(buffer, 0, bytesRead);
                   var response = await ServerHandle(netstream);
                   if (response== null)
                   {
                       Thread.Sleep(1000);
                   }
                   else
                   {
                       //TransBinary response = await ServerHandle(tb);
                       await clientSocket.SendAsync(new ArraySegment<byte>(response.ToArray()), SocketFlags.None);
                       //await clientSocket.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                       OnInfo($"Sent {bytesRead} bytes...");
                   }
               }
           }
           catch (Exception ex)
           {
               OnFault($"Error handling client: {ex.Message}");
           }
           finally
           {
               clientSocket.Close();
               OnInfo("Client disconnected.");
           }
       }

       private async Task ___HandleClientAsync(Socket clientSocket)
       {
           try
           {
               byte[] buffer = new byte[8192]; // Optimized buffer size
               int bytesRead;
               //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
               using (var netstream = new NetStream())
               {
                   while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                   {
                       OnInfo($"Received {bytesRead} bytes...");
                       netstream.Write(buffer, 0, bytesRead);
                   }
               }
               var tb = new TransBinary(new NetStream(buffer, 0, bytesRead));
               if (tb.TransType == TransType.None)
               {
                   Thread.Sleep(1000);
               }
               var response = await ServerHandle(tb);
               if (response.TransType == TransType.None)
               {
                   Thread.Sleep(1000);
               }
               else
               {
                   //TransBinary response = await ServerHandle(tb);
                   await clientSocket.SendAsync(new ArraySegment<byte>(response.BodyStream), SocketFlags.None);
                   //await clientSocket.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                   OnInfo($"Sent {bytesRead} bytes...");
               }
           }
           catch (Exception ex)
           {
               OnFault($"Error handling client: {ex.Message}");
           }
           finally
           {
               clientSocket.Close();
               OnInfo("Client disconnected.");
           }
       }


       private async byte[] ServerHandle(TransBinary request)
       {
           //TransBinary request = new TransBinary(bytes.Array);
           OnInfo($"Server Request Received {request.TransType} bytes...");
           var body = request.ReadBody();
           OnInfo($"Server Request Body {body}");

           //Server business process
           OnInfo($"Server business process...");
           //TransBinary response = await ExecRequsetAsync(request);

           TransBinary tb = new TransBinary("ok", "Ack", TransType.Text);
           OnInfo($"Server Response send");

           return tb.ToStream().ToArray();
       }
      


        #region Read/Write Async

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<TransBinary> ServerHandle(TransBinary request)
        {
            while(!Initilized)
            {
                Thread.Sleep(1000);
            }
            //return new TransBinary("NA", TransType.None);
            return await Task.Run(() =>
            {
                return new TransBinary("NA", "NA", TransType.None);
            });
        }

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<NetStream> ServerHandle(NetStream request)
        {
            while (!Initilized)
            {
                Thread.Sleep(1000);
            }
            //return new TransBinary("NA", TransType.None);
            return await Task.Run(() =>
            {
                return new NetStream(new byte[] {0});
            });
        }

        ///// <summary>
        ///// Write response to client.
        ///// </summary>
        ///// <param name="response"></param>
        //protected virtual async Task WriteResponseAsync(TransBinary response)
        //{
        //    if (response == null)
        //    {
        //        return;
        //    }

        //    var bytes = bResponse.GetBytes();
        //    if (bytes == null || bytes.Length == 0)
        //    {
        //        return;
        //    }
        //    await stream.WriteAsync(bytes, 0, bytes.Length);
        //}

        #endregion

   */
        #endregion
    }

    public class TcpSoketTransServer: TcpSoketServer
    {
        #region members/ctor
     
        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpSoketTransServer()
        {
            Settings = new TcpSettings();
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="allowedIps"></param>
        protected TcpSoketTransServer(string hostAddress, int port, string allowedIps)
        {
            Settings = new TcpSettings(hostAddress, port);
            Settings.AllowedIp = allowedIps;
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpSoketTransServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
            IsAsync = Settings.IsAsync;
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpSoketTransServer(TcpSettings settings)
        {
            Settings = settings;
            IsAsync = Settings.IsAsync;
            Log = settings.Log;
        }
        #endregion

        #region Tcp Server
      
        protected override void HandleClient(Socket clientSocket)
        {
            IDataStream trans;
            //byte[] trans;

            try
            {
                byte[] buffer = new byte[4096]; // Chunk size
                int bytesRead;
                using (var stream = new System.IO.MemoryStream())
                {

                    while ((bytesRead = clientSocket.Receive(buffer, SocketFlags.None)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break; // End of message
                    }
                    //byte[] receivedData = stream.ToArray();
                    OnInfo($"Received {stream.Length} bytes.");
                    //trans = stream.ToArray();
                    trans = new TransStream(stream.ToArray());

                    //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    //stream.Write(buffer, 0, bytesRead);
                    //OnInfo($"Received {bytesRead} bytes.");
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());
                }

                //if (trans.TransType == TransType.None)
                //{
                //    Task.Delay(1000);
                //}
                var response = ServerHandle(trans);
                if (response == null || response.IsEmpty)//.TransType == TransType.None)
                {
                    Task.Delay(1000);
                    //OnFault($"Client Response.IsEmpty");
                }
                else
                {
                    clientSocket.Send(response.DataStream(), SocketFlags.None);//
                    OnInfo($"Sent {bytesRead} bytes...");
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling client: {ex.Message}");
            }
            finally
            {
                clientSocket.Close();
                OnInfo("Client disconnected.");
            }
        }

        protected override async Task HandleClientAsync(Socket clientSocket)
        {
            IDataStream trans;
            //byte[] trans;
            try
            {
                byte[] buffer = new byte[4096]; // Chunk size
                int bytesRead;
                using (var stream = new System.IO.MemoryStream())
                {

                    while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break; // End of message
                    }
                    //byte[] receivedData = stream.ToArray();
                    OnInfo($"Received {stream.Length} bytes.");
                    trans = new TransStream(stream.ToArray());// stream.ToArray();
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());

                    //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    //stream.Write(buffer, 0, bytesRead);
                    //OnInfo($"Received {bytesRead} bytes.");
                    //trans = ReceivedData(stream.ToArray());// new TransBinary(stream.ToArray());
                }

                //if (trans.TransType == TransType.None)
                //{
                //    await Task.Delay(1000);
                //}

                var response = await ServerHandleAsync(trans);
                //var response = ServerHandle(trans);// await ServerHandleAsync(trans);
                if (response == null || response.IsEmpty)//.TransType == TransType.None)
                {
                    await Task.Delay(1000);
                    //OnFault($"Client Response.IsEmpty");
                }
                else
                {
                    await clientSocket.SendAsync(new ArraySegment<byte>(response.DataStream()), SocketFlags.None);
                    OnInfo($"Sent {bytesRead} bytes...");
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling client: {ex.Message}");
            }
            finally
            {
                clientSocket.Close();
                OnInfo("Client disconnected.");
            }
        }

        #endregion

        #region override

        //protected virtual IDataStream ReceivedData(byte[] data)
        //{
        //    return new TransStream(data);//binary
        //}

        protected virtual async Task<IDataStream> ServerHandleAsync(IDataStream request)
        {
            return await Task.Run(() =>
            {
                return new TransStream();// "NA", "NA", TransType.None); //binary
            });
        }
        protected virtual IDataStream ServerHandle(IDataStream request)
        {
            return new TransStream();// "NA", "NA", TransType.None); //binary
        }
        #endregion

    }

    /// <summary>
    /// Represent a tcp socket acceptor for tcp listener.
    /// </summary>
    public static class SocketAcceptor
    {
        private static bool IsConnected = false;
        private static Exception socketexception;
        private static ManualResetEvent tcpConnector = new ManualResetEvent(false);
        private static TCP.TcpClient client;
        /// <summary>
        /// Connect asynchronaizly to tcp server.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TCP.TcpClient AcceptTcpClient(TcpListener listener, int timeout)
        {
            tcpConnector.Reset();
            socketexception = null;

            listener.BeginAcceptTcpClient(new AsyncCallback(CallBackMethod), listener);

            if (tcpConnector.WaitOne(timeout, false))
            {
                if (IsConnected)
                {
                    return client;
                }
                else
                {
                    throw socketexception;
                }
            }
            else
            {
                throw new TimeoutException("SocketAcceptor TimeOut Exception");
            }
        }

        private static void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                IsConnected = false;
                TcpListener listener = asyncresult.AsyncState as TcpListener;

                if (listener.Server != null)
                {
                    client = listener.EndAcceptTcpClient(asyncresult);
                    IsConnected = true;
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                socketexception = ex;
            }
            finally
            {
                tcpConnector.Set();
            }
        }
    }

    /// <summary>
    /// Represent a base class for tcp server listner.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpServer<TRequest> : TcpServer<TRequest, TransStream> where TRequest : ITransformMessage, IDisposable
    {
        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpServer() : base()
        {
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServer(string hostAddress, int port) : base(hostAddress, port)
        {
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServer(string configHost) : base(configHost)
        {
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServer(TcpSettings settings) : base(settings)
        {
        }
        #endregion
    }

    /// <summary>
    /// Represent a base class for tcp server listner.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public abstract class TcpServer<TRequest, TResponse>
    where TRequest : ITransformMessage
    where TResponse : ITransformResponse
    {

        #region membrs
        volatile bool Listen;
        private bool Initilized = false;
        private bool _IsAsync = true;
        private bool EnableDataAvailable = false;
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

        /// <summary>
        /// Get if the current IsAsync settings.
        /// </summary>
        public bool IsAsync { get { return _IsAsync; } protected set {_IsAsync = value; } }
        #endregion

        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpServer()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServer(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServer(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
        #endregion

        #region Initilize

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
        IPEndPoint endpoint;
        //bool connected = false;
        int sockeErrors = 0;
        int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
        private Thread _listenerThread;
        TcpListener _listener;
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;
            EnableDataAvailable = Settings.UseDataAvailable;
            endpoint = Settings.GetEndpoint();
            MAX_SOCKET_ERRORS = Settings.MaxSocketError;
            IsAsync = Settings.IsAsync;
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


        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
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

                Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                StartInternal(IsAsync);
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: ", ex);
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

                //_listenerThread = new Thread(RunListener);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();
                Initilized = true;
            }
            catch (Exception ex)
            {
                OnFault("The tcp server async listener on Start throws the error: ", ex);
                return;
            }
        }

        void StopInternal()
        {
            try
            {
                Log.Info("The tcp server listener Stoping...");
                Task.Delay(3000);
                if (_listener != null)
                {
                    _listener.Stop();
                    //_listenerThread.Interrupt();
                    //_listenerThread.Join(5000);
                }
                Initilized = false;
            }
            catch (ThreadInterruptedException ex)
            {
                /* Clean up. */
                OnFault("The tcp server on Stop throws ThreadInterruptedException: ", ex);
            }
            catch (Exception ex)
            {
                OnFault("The tcp server listener on Stop throws the error: ", ex);
            }
        }

        public void Stop()
        {
            Listen = false;
            StopInternal();
            Initilized = false;
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
            if (client != null && client.Connected)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    if (client != null)
                    {
                        var ack = FaultAck(reason);
                        WriteResponse(stream, ack);
                        Close(client);
                    }
                }
                catch (Exception ex)
                {
                    OnFault("Tcp listener ExecFault error: ", ex);
                }
                finally
                {
                    Close(client);
                }
            }
        }
        /// <summary>
        /// Create fault ack.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected virtual TResponse FaultAck(string reason)
        {
            var tres = Nistec.Runtime.ActivatorUtil.CreateInstance<TResponse>();
            tres.SetState(-1, reason);
            return tres;

            //return TransStream.WriteState(-1, reason);//.ToStream();
        }
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(NetworkStream stream);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TResponse ExecRequset(TRequest request);

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            stream.Write(bytes, 0, bytes.Length);
        }

        #endregion

        #region Run

        /// <summary>
        /// Occured when client is connected.
        /// </summary>
        protected virtual void OnClientConnected()
        {
            //Console.WriteLine("Debuger-OnTcpClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
        }

        private async void Run()
        {
            //bool hasFault = false;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        await Task.Delay(5000);
                        continue;
                    }

                    //client = await _listener.AcceptTcpClientAsync();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}
                    //connected = true;
                    //OnClientConnected();

                    if (Listen == false)
                    {
                        Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                        await Task.Delay(1000);
                        return;
                    }

                    //await ProcessIncomingData(client, false);

                    TCP.TcpClient client = await _listener.AcceptTcpClientAsync();
                    if (client.Connected)
                    {
                        OnClientConnected();

                        await Task.Run(() =>
                        {
                            ProcessIncomingData(client, false);
                            // Simulate a long-running task
                            //await Task.WaitAny();// ..Delay(1000);
                            //Console.WriteLine("Done with the async task!");
                        });
                    }

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }

            }
        }

        void ProcessIncomingData(TCP.TcpClient client, bool enableException)
        {
            int count = 0;
            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                    Task.Delay(1000);
                    return;
                }

                                
                 if (EnableDataAvailable)
                 {
                     using (NetworkStream stream = client.GetStream())
                     {
                         while (count < 3)
                         {
                             if (stream.DataAvailable)
                             {
                                 count = 3;
                                 TRequest req = ReadRequest(stream);

                                 var res = ExecRequset(req);
                                 if (req.DuplexType.IsDuplex())
                                     WriteResponse(stream, res);
                             }
                             else
                             {
                                 count++;
                                 Console.WriteLine("stream is not DataAvailable, retry: " + count.ToString());
                                 Task.Delay(500);
                                 if (count >= 3)
                                     OnFault("The tcp server async ProcessIncomingData failed", new Exception("stream is not DataAvailable, After several attempts: " + count.ToString()));
                             }
                         }
                     }
                 }
                 else {
                     using (NetworkStream stream = client.GetStream())
                     {
                         TRequest req = ReadRequest(stream);

                         var res = ExecRequset(req);
                         if (req.DuplexType.IsDuplex())
                             WriteResponse(stream, res);
                     }
                 }
                sockeErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                else
                    OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
            }
            finally
            {
                Close(client);
            }
        }

        void Close(TCP.TcpClient client)
        {
            try
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
            catch (Exception ex)
            {
                OnFault("Close TcpClient error ", ex);
            }
        }
        #endregion

        #region Read/Write Async
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual async Task<TRequest> ReadRequestAsync(NetworkStream stream)
        {
            return await Task.Run(() =>
            {
                return ReadRequest(stream);
            });
        }
       

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<TResponse> ExecRequsetAsync(TRequest request)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(request);
            });
        }

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual async Task WriteResponseAsync(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        #endregion

        #region Run Async
        private async void RunAsync()
        {
            //bool hasFault = false;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        await Task.Delay(5000);
                        continue;
                    }

                    //client = await _listener.AcceptTcpClientAsync();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}
                    //connected = true;
                    //OnClientConnected();

                    if (Listen == false)
                    {
                        Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                        await Task.Delay(1000);
                        return;
                    }

                    //await ProcessIncomingData(client, false);

                    TCP.TcpClient client = await _listener.AcceptTcpClientAsync();
                    if (client.Connected)
                    {
                        OnClientConnected();

                        await Task.Run(async () =>
                        {
                            await ProcessIncomingDataAsync(client, false);
                            // Simulate a long-running task
                            //await Task.WaitAny();// ..Delay(1000);
                            //Console.WriteLine("Done with the async task!");
                        });
                    }

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }

            }
        }

        async Task ProcessIncomingDataAsync(TCP.TcpClient client, bool enableException)
        {
            int count = 0;
            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                    await Task.Delay(1000);
                    return;
                }

                if (EnableDataAvailable)
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        while (count < 3)
                        {
                            if (stream.DataAvailable)
                            {
                                count = 3;
                                TRequest req = await ReadRequestAsync(stream);

                                var res = await ExecRequsetAsync(req);
                                if (req.DuplexType.IsDuplex())
                                    WriteResponse(stream, res);
                            }
                            else
                            {
                                count++;
                                Console.WriteLine("stream is not DataAvailable, retry: " + count.ToString());
                                await Task.Delay(500);
                                if (count >= 3)
                                    OnFault("The tcp server async ProcessIncomingData failed", new Exception("stream is not DataAvailable, After several attempts: " + count.ToString()));
                            }
                        }
                    }
                }
                else
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        TRequest req = await ReadRequestAsync(stream);

                        var res = await ExecRequsetAsync(req);
                        if (req.DuplexType.IsDuplex())
                            WriteResponse(stream, res);
                    }
                }
                sockeErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                else
                    OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
            }
            finally
            {
                Close(client);
            }
        }

        #endregion

        #region Run Async OLD
        //NOT IN USE

        private void RunAsyncTask()
        {
            //bool hasFault = false;

            while (Listen)
            {
                TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    client = _listener.AcceptTcpClient();

                    if (IsReady == false)
                    {
                        //hasFault = true;
                        ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                        Thread.Sleep(1000);
                        continue;
                    }
                    //connected = true;
                    OnClientConnected();

                    //using (NetworkStream stream = client.GetStream())
                    //{
                    //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                    //    var res = ExecRequset(req);
                    //    if (req.IsDuplex)
                    //        WriteResponse(stream, res);
                    //}
                    //sockeErrors = 0;

                    //ProcessIncomingData(client,true);

                    Task.Run(() =>
                    {
                        ProcessIncomingData(client, false);
                    });

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }
                finally
                {
                    //Close(client);
                }
            }

        }
        #pragma warning disable CS0649
        private class ServerCom : IDisposable
        {
            public long Uid;
            public TCP.TcpClient client;
            public ManualResetEvent ManualReset;

            public void Dispose()
            {
                if (ManualReset != null)
                {
                    ManualReset.Close();
                    ManualReset.Dispose();
                    ManualReset = null;
                }

                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                    //client.Close();
                    client = null;
                }
            }
        }
        private void RunAsyncInvoke()
        {

            while (Listen)
            {

                try
                {
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    //client = _listener.AcceptTcpClient();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}

                    tcpClientConnected.Reset();
                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), _listener);
                    //connected = true;
                    //OnClientConnected();
                    tcpClientConnected.WaitOne();

                }
                catch (SocketException se)
                {
                    sockeErrors++;
                    OnFault("The tcp server async throws SocketException: {0}", se);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    OnFault("The tcp server async throws the error: ", ex);
                }
            }
        }
        async void ProcessIncomingConnection(IAsyncResult ar)
        {
            TcpListener listener = null;
            TCP.TcpClient client = null;

            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingConnection not lisenning... ");
                    Thread.Sleep(1000);
                    return;
                }

                listener = (TcpListener)ar.AsyncState;
                client = listener.EndAcceptTcpClient(ar);

                if (IsReady == false)
                {
                    //hasFault = true;
                    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    Thread.Sleep(1000);
                    return;
                }
                OnClientConnected();

                //using (NetworkStream stream = client.GetStream())
                //{
                //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                //    var res = ExecRequset(req);
                //    if (req.IsDuplex)
                //        WriteResponse(stream, res);
                //}
                //sockeErrors = 0;

                //ProcessIncomingData(client, readtimeout, ReceiveBufferSize,true);

                await Task.Run(() => 
                {
                    ProcessIncomingData(client, false);
                });
            }
            catch (SocketException se)
            {
                int val = Interlocked.Increment(ref sockeErrors);
                OnFault("The tcp server async throws SocketException: {0}", se);
                ExecFault(client, "The tcp server throws Exception: " + se.Message);
                if (val > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                {
                    Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                    listener.Stop();
                }
                OnFault("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);
            }
            catch (Exception ex)
            {
                ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                OnFault("The tcp server async ProcessIncomingConnection throws the error: ", ex);
            }
            finally
            {
                //Close(client);
                tcpClientConnected.Set();
            }
        }

        #endregion

    }


    /// <summary>
    /// Represent a base class for tcp server listner.
    /// </summary>
    public abstract class TcpDataServer//<IDataStream, IDataStream>
    //where TRequest : IDataStream
    //where TResponse : IDataStream
    {

        #region membrs
        volatile bool Listen;
        private bool Initilized = false;
        private bool _IsAsync = true;
        private bool EnableDataAvailable = false;
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

        /// <summary>
        /// Get if the current IsAsync settings.
        /// </summary>
        public bool IsAsync { get { return _IsAsync; } protected set { _IsAsync = value; } }
        #endregion

        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpDataServer()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpDataServer(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpDataServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpDataServer(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
        #endregion

        #region Initilize

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
        IPEndPoint endpoint;
        //bool connected = false;
        int sockeErrors = 0;
        int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
        private Thread _listenerThread;
        TcpListener _listener;
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;
            EnableDataAvailable = Settings.UseDataAvailable;
            endpoint = Settings.GetEndpoint();
            MAX_SOCKET_ERRORS = Settings.MaxSocketError;
            IsAsync = Settings.IsAsync;
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


        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
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

                Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                StartInternal(IsAsync);
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: ", ex);
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

                //_listenerThread = new Thread(RunListener);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();
                Initilized = true;
            }
            catch (Exception ex)
            {
                OnFault("The tcp server async listener on Start throws the error: ", ex);
                return;
            }
        }

        void StopInternal()
        {
            try
            {
                Log.Info("The tcp server listener Stoping...");
                Task.Delay(3000);
                if (_listener != null)
                {
                    _listener.Stop();
                    //_listenerThread.Interrupt();
                    //_listenerThread.Join(5000);
                }
                Initilized = false;
            }
            catch (ThreadInterruptedException ex)
            {
                /* Clean up. */
                OnFault("The tcp server on Stop throws ThreadInterruptedException: ", ex);
            }
            catch (Exception ex)
            {
                OnFault("The tcp server listener on Stop throws the error: ", ex);
            }
        }

        public void Stop()
        {
            Listen = false;
            StopInternal();
            Initilized = false;
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
            if (client != null && client.Connected)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    if (client != null)
                    {
                        var ack = FaultAck(reason);
                        WriteResponse(stream, ack);
                        Close(client);
                    }
                }
                catch (Exception ex)
                {
                    OnFault("Tcp listener ExecFault error: ", ex);
                }
                finally
                {
                    Close(client);
                }
            }
        }

        /// <summary>
        /// Create fault ack.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected virtual IDataStream FaultAck(string reason)
        {
            return TransStream.WriteState(-1, reason); 
            //var tres = Nistec.Runtime.ActivatorUtil.CreateInstance<TResponse>();
            //tres.SetState(-1, reason);
            //return tres;

            //return TransStream.WriteState(-1, reason);//.ToStream();
        }


        protected abstract IDataStream ExecRequset(NetworkStream stream);

        /*
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected abstract IDataStream ReadRequest(NetworkStream stream);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract IDataStream ExecRequset(IDataStream request);
        */
        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(NetworkStream stream, IDataStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            stream.Write(bytes, 0, bytes.Length);
        }

        #endregion

        #region Run

        /// <summary>
        /// Occured when client is connected.
        /// </summary>
        protected virtual void OnClientConnected()
        {
            //Console.WriteLine("Debuger-OnTcpClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
        }

        private async void Run()
        {
            //bool hasFault = false;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        await Task.Delay(5000);
                        continue;
                    }

                    //client = await _listener.AcceptTcpClientAsync();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}
                    //connected = true;
                    //OnClientConnected();

                    if (Listen == false)
                    {
                        Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                        await Task.Delay(1000);
                        return;
                    }

                    //await ProcessIncomingData(client, false);

                    TCP.TcpClient client = await _listener.AcceptTcpClientAsync();
                    if (client.Connected)
                    {
                        OnClientConnected();

                        await Task.Run(() =>
                        {
                            ProcessIncomingData(client, false);
                            // Simulate a long-running task
                            //await Task.WaitAny();// ..Delay(1000);
                            //Console.WriteLine("Done with the async task!");
                        });
                    }

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }

            }
        }

        void ProcessIncomingData(TCP.TcpClient client, bool enableException)
        {
            int count = 0;
            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                    Task.Delay(1000);
                    return;
                }

                
                 if (EnableDataAvailable)
                 {
                     using (NetworkStream stream = client.GetStream())
                     {
                         while (count < 3)
                         {
                             if (stream.DataAvailable)
                             {
                                 count = 3;
                                /*
                                IDataStream req = ReadRequest(stream);

                                 var res = ExecRequset(req);
                                //if (req.DuplexType.IsDuplex())
                                */
                                IDataStream res = ExecRequset(stream);
                                if (res != null)
                                    WriteResponse(stream, res);
                             }
                             else
                             {
                                 count++;
                                 Console.WriteLine("stream is not DataAvailable, retry: " + count.ToString());
                                 Task.Delay(500);
                                 if (count >= 3)
                                     OnFault("The tcp server async ProcessIncomingData failed", new Exception("stream is not DataAvailable, After several attempts: " + count.ToString()));
                             }
                         }
                     }
                 }
                 else {
                    using (NetworkStream stream = client.GetStream())
                    {
                        /*
                        IDataStream req = ReadRequest(stream);

                         var res = ExecRequset(req);
                        //if (req.DuplexType.IsDuplex())
                        */
                        IDataStream res = ExecRequset(stream);
                        if (res != null)
                            WriteResponse(stream, res);
                    }
                 }
                sockeErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                else
                    OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
            }
            finally
            {
                Close(client);
            }
        }

        void Close(TCP.TcpClient client)
        {
            try
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
            catch (Exception ex)
            {
                OnFault("Close TcpClient error ", ex);
            }
        }
        #endregion

        #region Read/Write Async
        /*
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual async Task<IDataStream> ReadRequestAsync(NetworkStream stream)
        {
            return await Task.Run(() =>
            {
                return ReadRequest(stream);
            });
        }


        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<IDataStream> ExecRequsetAsync(IDataStream request)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(request);
            });
        }
        */

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<IDataStream> ExecRequsetAsync(NetworkStream stream)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(stream);
            });
        }

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual async Task WriteResponseAsync(NetworkStream stream, IDataStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        #endregion

        #region Run Async
        private async void RunAsync()
        {
            //bool hasFault = false;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        await Task.Delay(5000);
                        continue;
                    }

                    //client = await _listener.AcceptTcpClientAsync();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}
                    //connected = true;
                    //OnClientConnected();

                    if (Listen == false)
                    {
                        Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                        await Task.Delay(1000);
                        return;
                    }

                    //await ProcessIncomingData(client, false);

                    TCP.TcpClient client = await _listener.AcceptTcpClientAsync();
                    if (client.Connected)
                    {
                        OnClientConnected();

                        await Task.Run(async () =>
                        {
                            await ProcessIncomingDataAsync(client, false);
                            // Simulate a long-running task
                            //await Task.WaitAny();// ..Delay(1000);
                            //Console.WriteLine("Done with the async task!");
                        });
                    }

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }

            }
        }

        async Task ProcessIncomingDataAsync(TCP.TcpClient client, bool enableException)
        {
            int count = 0;
            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                    await Task.Delay(1000);
                    return;
                }

                if (EnableDataAvailable)
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        while (count < 3)
                        {
                            if (stream.DataAvailable)
                            {
                                count = 3;
                                /*
                                IDataStream req = await ReadRequestAsync(stream);

                                var res = await ExecRequsetAsync(req);
                                //if (req.DuplexType.IsDuplex())
                                var res = await ExecRequsetAsync(req);
                                */
                                IDataStream res = await ExecRequsetAsync(stream);
                                if (res != null)
                                    WriteResponse(stream, res);
                            }
                            else
                            {
                                count++;
                                Console.WriteLine("stream is not DataAvailable, retry: " + count.ToString());
                                await Task.Delay(500);
                                if (count >= 3)
                                    OnFault("The tcp server async ProcessIncomingData failed", new Exception("stream is not DataAvailable, After several attempts: " + count.ToString()));
                            }
                        }
                    }
                }
                else
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        /*
                        IDataStream req = await ReadRequestAsync(stream);

                        var res = await ExecRequsetAsync(req);
                        //if (req.DuplexType.IsDuplex())
                        */
                        IDataStream res = await ExecRequsetAsync(stream);
                        if (res != null)
                           WriteResponse(stream, res);
                    }
                }
                sockeErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                else
                    OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
            }
            finally
            {
                Close(client);
            }
        }

        #endregion

        #region Run Async OLD
        //NOT IN USE

        private void RunAsyncTask()
        {
            //bool hasFault = false;

            while (Listen)
            {
                TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    client = _listener.AcceptTcpClient();

                    if (IsReady == false)
                    {
                        //hasFault = true;
                        ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                        Thread.Sleep(1000);
                        continue;
                    }
                    //connected = true;
                    OnClientConnected();

                    //using (NetworkStream stream = client.GetStream())
                    //{
                    //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                    //    var res = ExecRequset(req);
                    //    if (req.IsDuplex)
                    //        WriteResponse(stream, res);
                    //}
                    //sockeErrors = 0;

                    //ProcessIncomingData(client,true);

                    Task.Run(() =>
                    {
                        ProcessIncomingData(client, false);
                    });

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }
                finally
                {
                    //Close(client);
                }
            }

        }
#pragma warning disable CS0649
        private class ServerCom : IDisposable
        {
            public long Uid;
            public TCP.TcpClient client;
            public ManualResetEvent ManualReset;

            public void Dispose()
            {
                if (ManualReset != null)
                {
                    ManualReset.Close();
                    ManualReset.Dispose();
                    ManualReset = null;
                }

                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                    //client.Close();
                    client = null;
                }
            }
        }
        private void RunAsyncInvoke()
        {

            while (Listen)
            {

                try
                {
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    //client = _listener.AcceptTcpClient();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}

                    tcpClientConnected.Reset();
                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), _listener);
                    //connected = true;
                    //OnClientConnected();
                    tcpClientConnected.WaitOne();

                }
                catch (SocketException se)
                {
                    sockeErrors++;
                    OnFault("The tcp server async throws SocketException: {0}", se);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    OnFault("The tcp server async throws the error: ", ex);
                }
            }
        }
        async void ProcessIncomingConnection(IAsyncResult ar)
        {
            TcpListener listener = null;
            TCP.TcpClient client = null;

            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingConnection not lisenning... ");
                    Thread.Sleep(1000);
                    return;
                }

                listener = (TcpListener)ar.AsyncState;
                client = listener.EndAcceptTcpClient(ar);

                if (IsReady == false)
                {
                    //hasFault = true;
                    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    Thread.Sleep(1000);
                    return;
                }
                OnClientConnected();

                //using (NetworkStream stream = client.GetStream())
                //{
                //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                //    var res = ExecRequset(req);
                //    if (req.IsDuplex)
                //        WriteResponse(stream, res);
                //}
                //sockeErrors = 0;

                //ProcessIncomingData(client, readtimeout, ReceiveBufferSize,true);

                await Task.Run(() =>
                {
                    ProcessIncomingData(client, false);
                });
            }
            catch (SocketException se)
            {
                int val = Interlocked.Increment(ref sockeErrors);
                OnFault("The tcp server async throws SocketException: {0}", se);
                ExecFault(client, "The tcp server throws Exception: " + se.Message);
                if (val > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                {
                    Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                    listener.Stop();
                }
                OnFault("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);
            }
            catch (Exception ex)
            {
                ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                OnFault("The tcp server async ProcessIncomingConnection throws the error: ", ex);
            }
            finally
            {
                //Close(client);
                tcpClientConnected.Set();
            }
        }

        #endregion

    }

    /// <summary>
    /// Represent a tcp server listner.
    /// </summary>
    public abstract class TcpServer : TcpServer<TcpMessage>
    {
        #region ctor

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        protected TcpServer(string hostName, int port)
            : base(hostName, port)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpServer"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServer(string configHost)
            : base(configHost)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpServer"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServer(TcpSettings settings)
            : base(settings)
        {

        }
        #endregion

        #region abstract methods

        ///// <summary>
        ///// Read Request
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        //protected override TcpMessage ReadRequest(NetworkStream stream)
        //{

        //    return TcpMessage.ServerReadRequest(stream);
        //}

        #endregion

    }

#if (false)


    /// <summary>
    /// Represent a base class for tcp server listner.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public abstract class TcpServerSecure<TRequest, TResponse>
    where TRequest : ITransformMessage
    where TResponse : ITransformResponse
    {

    #region membrs
        volatile bool Listen;
        private bool Initilized = false;
        //private bool _IsAsync = true;
        //private bool EnableDataAvailable = false;
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

        ///// <summary>
        ///// Get if the current IsAsync settings.
        ///// </summary>
        //public bool IsAsync { get { return _IsAsync; } protected set { _IsAsync = value; } }
    #endregion

    #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpServerSecure()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServerSecure(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServerSecure(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServerSecure(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
    #endregion

    #region Initilize
        string[] AllowedIPs;
        int ReceiveBufferSize = 4096;
        int SendBufferSize = 4096;

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
        IPEndPoint endpoint;
        //bool connected = false;
        int sockeErrors = 0;
        int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
        private Thread _listenerThread;
        TcpListener _listener;
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;
            AllowedIPs = Settings.AllowedListIp;
            ReceiveBufferSize = Settings.ReceiveBufferSize;
            SendBufferSize = Settings.SendBufferSize;
            //EnableDataAvailable = Settings.UseDataAvailable;
            endpoint = Settings.GetEndpoint();
            MAX_SOCKET_ERRORS = Settings.MaxSocketError;
            //IsAsync = Settings.IsAsync;
            OnLoad();
            Log.Info("TcpServer Initilized...\n");
            IsReady = true;
        }

        /// <summary>
        /// Occured when client is connected.
        /// </summary>
        protected virtual void OnClientConnected()
        {
            //Console.WriteLine("Debuger-OnTcpClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
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

        protected virtual void OnInfo(string message)
        {
            Log.Info(message);
            Console.WriteLine(message);
        }
        protected virtual void OnFault(string message)
        {
            Log.Error(message);
            Console.WriteLine(message);
        }

        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
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

                Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                StartInternal();
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: ", ex);
            }
        }

        void StartInternal()
        {
            try
            {
                _listener = new TcpListener(endpoint);
                _listener.ExclusiveAddressUse = false;
                _listener.Start();
                _listenerThread = new Thread(RunSecure);

                //_listenerThread = new Thread(RunListener);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();
                Initilized = true;
            }
            catch (Exception ex)
            {
                OnFault("The tcp server async listener on Start throws the error: ", ex);
                return;
            }
        }

        void StopInternal()
        {
            try
            {
                Log.Info("The tcp server listener Stoping...");
                Thread.Sleep(3000);
                if (_listener != null)
                {
                    _listener.Stop();
                    //_listenerThread.Interrupt();
                    //_listenerThread.Join(5000);
                }
                Initilized = false;
            }
            catch (ThreadInterruptedException ex)
            {
                /* Clean up. */
                                OnFault("The tcp server on Stop throws ThreadInterruptedException: ", ex);
            }
            catch (Exception ex)
            {
                OnFault("The tcp server listener on Stop throws the error: ", ex);
            }
        }

        public void Stop()
        {
            Listen = false;
            StopInternal();
            Initilized = false;
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

        //internal void ExecFault(TCP.TcpClient client, string reason)
        //{
        //    Log.Error("Tcp listener fault error: " + reason);
        //    if (client != null && client.Connected)
        //    {
        //        try
        //        {
        //            NetworkStream stream = client.GetStream();
        //            if (client != null)
        //            {
        //                var ack = FaultAck(reason);
        //                WriteResponse(stream, ack);
        //                Close(client);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            OnFault("Tcp listener ExecFault error: ", ex);
        //        }
        //        finally
        //        {
        //            Close(client);
        //        }
        //    }
        //}
        /// <summary>
        /// Create fault ack.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected virtual TResponse FaultAck(string reason)
        {
            var tres = Nistec.Runtime.ActivatorUtil.CreateInstance<TResponse>();
            tres.SetState(-1, reason);
            return tres;

            //return TransStream.WriteState(-1, reason);//.ToStream();
        }
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(NetStream stream);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TResponse ExecRequset(TRequest request);

    #endregion

    #region Run Secure
        private async void RunSecure()
        {
            //bool hasFault = false;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    if (Listen == false)
                    {
                        Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                        Thread.Sleep(1000);
                        return;
                    }

                    await AcceptClientsAsync();

                    //var _cts = new CancellationTokenSource();

                    //await Task.Run(() => AcceptClientsAsync());//   _cts.Token);// ; ;// ;//);

                    //TCP.TcpClient client = await _listener.AcceptTcpClientAsync();
                    //string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    //if (!IsAllowedIP(clientIP))
                    //{
                    //    OnFault("Rejected connection from " + clientIP);
                    //    client.Close();
                    //    continue;
                    //}

                    //if (client.Connected)
                    //{
                    //    OnClientConnected();

                    //    await Task.Run(async () =>
                    //    {
                    //        await HandleClientAsync(client);

                    //        //await ProcessIncomingDataAsync(client, false);
                    //        // Simulate a long-running task
                    //        //await Task.WaitAny();// ..Delay(1000);
                    //        //Console.WriteLine("Done with the async task!");
                    //    });
                    //}

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }

            }
        }

        private async Task AcceptClientsAsync()
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                if (!IsAllowedIP(clientIP))
                {
                    OnFault("Rejected connection from " + clientIP);
                    client.Close();
                }
                else
                {
                    OnInfo($"Client connected from {clientIP}");
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error accepting client: {ex.Message}");
            }
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    if (!IsAllowedIP(clientIP))
                    {
                        OnFault("Rejected connection from " + clientIP);
                        client.Close();
                        continue;
                    }
                    else
                    {
                        OnInfo($"Client connected from {clientIP}");
                        _ = HandleClientAsync(client);
                    }
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        OnFault($"Error accepting client: {ex.Message}");
                }
            }
        }

        private bool IsAllowedIP(string ip)
        {
            return Array.Exists(AllowedIPs, allowedIp => allowedIp == ip);
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    NetStream memoryStream = new NetStream(); // Efficient byte storage
                    {
                        //StringBuilder fullMessage = new StringBuilder();
                        byte[] buffer = new byte[ReceiveBufferSize]; // Large buffer size
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await memoryStream.WriteAsync(buffer, 0, bytesRead); // Store bytes in memory
                            Console.WriteLine($"Received {bytesRead} bytes...");
                        }
                        OnInfo($"Total received bytes: {memoryStream.Length}");

                        TRequest req = await ReadRequestAsync(memoryStream);
                        OnInfo("ReadRequestAsync ok");

                        var res = await ExecRequsetAsync(req);
                        OnInfo("ExecRequsetAsync ok");

                        if (req.DuplexType.IsDuplex())
                            await WriteResponseAsync(stream, res);
                        OnInfo("WriteResponseAsync completed");


                        //byte[] receivedData = memoryStream.ToArray(); // Convert to byte array
                        //OnInfo($"✅ Total received bytes: {receivedData.Length}");

                        //var request = await ReadRequestAsync(receivedData);
                        //var response = await ExecRequsetAsync(request);
                        //await WriteResponseAsync(stream, response);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                OnInfo("Client disconnected.");
            }
        }

    #endregion

    #region Read/Write Async
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual async Task<TRequest> ReadRequestAsync(NetStream stream)
        {
            return await Task.Run(() =>
            {
                return ReadRequest(stream);
            });
        }
   
        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<TResponse> ExecRequsetAsync(TRequest request)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(request);
            });
        }

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual async Task WriteResponseAsync(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

    #endregion

    }

    public abstract class SecureTcpServer<TRequest, TResponse>
    where TRequest : ITransformMessage
    where TResponse : ITransformResponse
    {

    #region membrs
        //volatile bool Listen;
        private bool Initilized = false;

        private static int Port;// = 5000;
        private static string[] AllowedIPs; //= { "127.0.0.1", "192.168.1.100" }; // Whitelisted IPs
        private static TcpListener _listener;
        private static bool _isRunning = false;
        private static CancellationTokenSource _cts;
        private static int ReceiveBufferSize = 4096;

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
        protected SecureTcpServer()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected SecureTcpServer(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected SecureTcpServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected SecureTcpServer(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
    #endregion

    #region Initilize
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;

            AllowedIPs = Settings.AllowedListIp;
            Port = Settings.Port;
            ReceiveBufferSize = Settings.ReceiveBufferSize;
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

        protected virtual void OnInfo(string message)
        {
            Console.WriteLine(message);
            Log.Info(message);
        }

        protected virtual void OnFault(string message)
        {
            Console.WriteLine(message);
            Log.Error(message);
        }

        //protected virtual void OnFault(string message, Exception ex)
        //{
        //    Log.Exception(message, ex, true);
        //}

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
                StartServer();
            }
            catch (Exception ex)
            {
                //Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: "+ ex.Message);
            }
        }

        public void Stop()
        {
            //Listen = false;
            StopServer();
            Initilized = false;
            _State = ChannelServiceState.Stoped;
            OnStop();
            Log.Info("TcpServer stoped: {0}", Settings.HostName);
        }

        public void Pause()
        {
            //Listen = false;
            _State = ChannelServiceState.Paused;
            OnPause();
            Log.Debug("TcpServer paused: {0}", Settings.HostName);
        }
    #endregion

    #region Read/Write

        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(NetStream stream);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TResponse ExecRequset(TRequest request);

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            stream.Write(bytes, 0, bytes.Length);
        }

    #endregion

    #region Read/Write Async
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual async Task<TRequest> ReadRequestAsync(NetStream stream)
        {
            return await Task.Run(() =>
            {
                return ReadRequest(stream);
            });
        }

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<TResponse> ExecRequsetAsync(TRequest request)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(request);
            });
        }

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual async Task WriteResponseAsync(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

    #endregion

    #region Server

        protected void StartServer()
        {
            if (_isRunning)
            {
                OnInfo("Server is already running.");
                return;
            }

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start(100);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _isRunning = true;
            OnStart();
            OnInfo(string.Format("Server started on port :{0},  AllowedIPs: {1}", Port, AllowedIPs == null ? "NA" : AllowedIPs.JoinTrim()));

            Task.Run(() => AcceptClientsAsync(_cts.Token));
        }

        protected void StopServer()
        {
            if (!_isRunning)
            {
                OnInfo("Server is not running.");
                return;
            }

            _cts.Cancel();
            _listener.Stop();
            _isRunning = false;

            OnInfo("Server stopped.");
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    if (!IsAllowedIP(clientIP))
                    {
                        OnFault("Rejected connection from " + clientIP);
                        client.Close();
                        continue;
                    }

                    OnInfo($"🔗 Client connected from {clientIP}");

                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        OnFault($"⚠️ Error accepting client: {ex.Message}");
                }
            }
        }
  
        private bool IsAllowedIP(string ip)
        {
            return Array.Exists(AllowedIPs, allowedIp => allowedIp == ip);
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    //using (MemoryStream memoryStream = new MemoryStream()) // Efficient byte storage
                    NetStream memoryStream = new NetStream();
                    {
                        //StringBuilder fullMessage = new StringBuilder();
                        byte[] buffer = new byte[ReceiveBufferSize]; // Large buffer size
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await memoryStream.WriteAsync(buffer, 0, bytesRead); // Store bytes in memory
                            OnInfo($"📩 Received {bytesRead} bytes...");
                        }
                        //byte[] receivedData = memoryStream.ToArray(); // Convert to byte array
                        OnInfo($"✅ Total received bytes: {memoryStream.Length}");

                        var request = ReadRequest(memoryStream);
                        OnInfo("ReadRequestAsync ok");
                        var response = ExecRequset(request);
                        OnInfo("ExecRequsetAsync ok");
                        if (request.DuplexType.IsDuplex())
                        {
                            await WriteResponseAsync(stream, response);
                            OnInfo("WriteResponseAsync completed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"⚠️ Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                OnInfo("🔌 Client disconnected.");
            }
        }

    #endregion
    }

    /// <summary>
    /// SecureTcpServer using string request and response
    /// </summary>
    public abstract class SecureTcpServer
    {

    #region membrs
        //volatile bool Listen;
        private bool Initilized = false;

        private static int Port;// = 5000;
        private static string[] AllowedIPs; //= { "127.0.0.1", "192.168.1.100" }; // Whitelisted IPs
        private static TcpListener _listener;
        private static bool _isRunning = false;
        private static CancellationTokenSource _cts;
        private static int ReceiveBufferSize = 4096;

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
        protected SecureTcpServer()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected SecureTcpServer(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected SecureTcpServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected SecureTcpServer(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
    #endregion

    #region Initilize

        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;

            AllowedIPs = Settings.AllowedListIp;
            Port = Settings.Port;
            ReceiveBufferSize = Settings.ReceiveBufferSize;
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

        protected virtual void OnInfo(string message)
        {
            Log.Info(message);
        }

        protected virtual void OnFault(string message)
        {
            Log.Error(message);
        }

        //protected virtual void OnFault(string message, Exception ex)
        //{
        //    Log.Exception(message, ex, true);
        //}

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
                StartServer();
            }
            catch (Exception ex)
            {
                //Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: " + ex.Message);
            }
        }

        public void Stop()
        {
            //Listen = false;
            StopServer();
            Initilized = false;
            _State = ChannelServiceState.Stoped;
            OnStop();
            Log.Info("TcpServer stoped: {0}", Settings.HostName);
        }

        public void Pause()
        {
            //Listen = false;
            _State = ChannelServiceState.Paused;
            OnPause();
            Log.Debug("TcpServer paused: {0}", Settings.HostName);
        }
    #endregion

    #region Read/Write

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract string ExecRequset(string request);

    #endregion

    #region Read/Write Async
       
        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<string> ExecRequsetAsync(string request)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(request);
            });
        }

    #endregion

    #region Server
        protected void StartServer()
        {
            if (_isRunning)
            {
                OnInfo("Server is already running.");
                return;
            }

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _isRunning = true;
            OnStart();
            OnInfo(string.Format("Server started on port :{0},  AllowedIPs: {1}", Port, AllowedIPs==null ? "NA" : AllowedIPs.JoinTrim()));

            Task.Run(() => AcceptClientsAsync(_cts.Token));
        }

        protected void StopServer()
        {
            if (!_isRunning)
            {
                OnInfo("Server is not running.");
                return;
            }

            _cts.Cancel();
            _listener.Stop();
            _isRunning = false;

            OnInfo("Server stopped.");
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    if (!IsAllowedIP(clientIP))
                    {
                        OnFault("Rejected connection from " + clientIP);
                        client.Close();
                        continue;
                    }

                    OnInfo($"🔗 Client connected from {clientIP}");

                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        OnFault($"⚠️ Error accepting client: {ex.Message}");
                }
            }
        }

        private bool IsAllowedIP(string ip)
        {
            return Array.Exists(AllowedIPs, allowedIp => allowedIp == ip);
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    StringBuilder fullMessage = new StringBuilder();
                    byte[] buffer = new byte[ReceiveBufferSize]; // Large buffer size

                    while (true)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // Client disconnected

                        fullMessage.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                        // Check if message is complete (e.g., based on a delimiter)
                        if (fullMessage.ToString().EndsWith("<EOF>")) break;
                    }

                    string message = fullMessage.ToString().Replace("<EOF>", ""); // Remove delimiter
                    OnInfo($"📩 Received full message: {message}");
                    var result = await ExecRequsetAsync(message);

                    byte[] response = Encoding.UTF8.GetBytes(result);// $"✅ Server received: {message}");
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                OnFault($"⚠️ Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                OnInfo("🔌 Client disconnected.");
            }
        }

    #endregion
    }


    public abstract class FlexTcpServer
    {
    #region membrs
        //volatile bool Listen;
        private bool Initilized = false;

        private static int Port;// = 5000;
        private static string[] AllowedIPs; //= { "127.0.0.1", "192.168.1.100" }; // Whitelisted IPs
        private static TcpListener _listener;
        private static bool _isRunning = false;
        private static CancellationTokenSource _cts;
        private static int ReceiveBufferSize = 4096;

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
        protected FlexTcpServer()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected FlexTcpServer(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected FlexTcpServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected FlexTcpServer(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
    #endregion

    #region Initilize
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;

            AllowedIPs = Settings.AllowedListIp;
            Port = Settings.Port;
            ReceiveBufferSize = Settings.ReceiveBufferSize;
            OnLoad();
            Log.Info("FlexTcpServer Initilized...\n");
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

        protected virtual void OnInfo(string message)
        {
            Log.Info(message);
        }

        protected virtual void OnFault(string message)
        {
            Log.Error(message);
        }

        //protected virtual void OnFault(string message, Exception ex)
        //{
        //    Log.Exception(message, ex, true);
        //}

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
                StartServer();
            }
            catch (Exception ex)
            {
                //Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: " + ex.Message);
            }
        }

        public void Stop()
        {
            //Listen = false;
            StopServer();
            Initilized = false;
            _State = ChannelServiceState.Stoped;
            OnStop();
            Log.Info("TcpServer stoped: {0}", Settings.HostName);
        }

        public void Pause()
        {
            //Listen = false;
            _State = ChannelServiceState.Paused;
            OnPause();
            Log.Debug("TcpServer paused: {0}", Settings.HostName);
        }
    #endregion

    #region Read/Write

        ///// <summary>
        ///// Read Request from client.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        //protected abstract TransFlex ReadRequest(byte[] stream);

        ///// <summary>
        ///// Exec client requset.
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //protected abstract TransFlex ExecRequset(TransFlex request);

        ///// <summary>
        ///// Write response to client.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="bResponse"></param>
        //protected virtual void WriteResponse(NetworkStream stream, TransFlex bResponse)
        //{
        //    if (bResponse == null)
        //    {
        //        return;
        //    }
        //    var bytes = bResponse.GetBytes();
        //    if (bytes == null || bytes.Length == 0)
        //    {
        //        return;
        //    }
        //    stream.Write(bytes, 0, bytes.Length);
        //}

    #endregion

    #region Read/Write Async
        ///// <summary>
        ///// Read Request from client.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        //protected virtual async Task<TransFlex> ReadRequestAsync(byte[] stream)
        //{
        //    return await Task.Run(() =>
        //    {
        //        return ReadRequest(stream);
        //    });
        //}

        ///// <summary>
        ///// Exec client requset.
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //protected virtual async Task<TransFlex> ExecRequsetAsync(TransFlex request)
        //{
        //    return await Task.Run(() =>
        //    {
        //        return ExecRequset(request);
        //    });
        //}

        ///// <summary>
        ///// Write response to client.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="bResponse"></param>
        //protected virtual async Task WriteResponseAsync(NetworkStream stream, TransFlex bResponse)
        //{
        //    if (bResponse == null)
        //    {
        //        return;
        //    }
        //    var bytes = bResponse.GetBytes();
        //    if (bytes == null || bytes.Length == 0)
        //    {
        //        return;
        //    }
        //    await stream.WriteAsync(bytes, 0, bytes.Length);
        //}

    #endregion

    #region Server

        private bool IsAllowedIP(string ip)
        {
            return Array.Exists(AllowedIPs, allowedIp => allowedIp == ip);
        }

        public void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            Console.WriteLine($"✅ Server started on port {Port}...");

            Task.Run(() => AcceptClientsAsync());
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                if (!IsAllowedIP(clientIP))
                {
                    Console.WriteLine($"Rejected connection from {clientIP}");
                    client.Close();
                    continue;
                }
                Console.WriteLine("🔗 Client connected!");

                _ = HandleClientAsync(client);
            }
        }

        protected abstract Task<TransFlex> OnReceiveStreamAsync(TransFlex message);
       

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    var message = await ReceiveStreamAsync(stream);
                    OnInfo("ReadRequestAsync ok");
                    var result = await OnReceiveStreamAsync(message).ConfigureAwait(false);
                    OnInfo("ExecRequsetAsync ok");
                    await SendStreamAsync(stream, result);
                    OnInfo("WriteResponseAsync completed");
                }
            }
            catch (Exception ex)
            {
                OnFault($"⚠️ Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                OnFault("🔌 Client disconnected.");
            }
        }

        private async Task<TransFlex> ReceiveStreamAsync(NetworkStream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[ReceiveBufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("📥 Receiving stream...");
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"📥 Received {bytesRead} bytes...");
                }

                //string receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                var binary = memoryStream.ToArray();
                OnInfo($"✅ Received Stream Data: {binary.Length}");

                return await Task.FromResult<TransFlex>(new TransFlex(FlexType.Stream,binary));
            }
        }

        private async Task SendStreamAsync(NetworkStream stream, TransFlex message)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Simulate writing response data into the stream
                //byte[] responseData = Encoding.UTF8.GetBytes("Hello, Client! Your message was received.");
                //memoryStream.Write(responseData, 0, responseData.Length);
                //memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                message.EntityRead(memoryStream, null);

                byte[] buffer = new byte[ReceiveBufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("📤 Sending response stream...");
                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"📤 Sent {bytesRead} bytes...");
                }
                OnInfo("✅ Response stream sent successfully!");
            }
        }

        public void StopServer()
        {
            _listener.Stop();
            OnInfo("🛑 Server stopped.");
        }
    #endregion
    }

#endif

}

//TcpServerAsync
#if (false)
    /// <summary>
    /// Represent a base class for tcp server listner.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpServerAsync<TRequest> : TcpServerAsync<TRequest,TransStream> where TRequest : ITransformMessage, IDisposable
    {
#region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpServerAsync():base()
        {
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServerAsync(string hostAddress, int port) : base(hostAddress, port)
        {
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServerAsync(string configHost) : base(configHost)
        {
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServerAsync(TcpSettings settings) : base(settings)
        {
        }
#endregion
    }

    /// <summary>
    /// Represent a base class for tcp server listner.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public abstract class TcpServerAsync<TRequest,TResponse> 
    where TRequest : ITransformMessage 
    where TResponse : ITransformResponse
    {

#region membrs
        volatile bool Listen;
        private bool Initilized = false;
        private bool IsAsync = true;
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
        protected TcpServerAsync()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServerAsync(string hostAddress, int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServerAsync(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServerAsync(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
#endregion

#region Initilize

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
        IPEndPoint endpoint;
        //bool connected = false;
        int sockeErrors = 0;
        int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
        private Thread _listenerThread;
        TcpListener _listener;
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;
            endpoint = Settings.GetEndpoint();
            MAX_SOCKET_ERRORS = Settings.MaxSocketError;
            IsAsync = Settings.IsAsync;
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


        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
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

                Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                StartInternal(IsAsync);
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: ", ex);
            }
        }

        void StartInternal(bool isAsync)
        {
            try
            {
                _listener = new TcpListener(endpoint);
                _listener.ExclusiveAddressUse = false;
                _listener.Start();

                //if (isAsync)
                //    _listenerThread = new Thread(RunAsync);
                //else
                //    _listenerThread = new Thread(Run);

                _listenerThread = new Thread(RunAsync);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();
                Initilized = true;
            }
            catch (Exception ex)
            {
                OnFault("The tcp server async listener on Start throws the error: ", ex);
                return;
            }
        }

        void StopInternal()
        {
            try
            {
                Log.Info("The tcp server listener Stoping...");
                Thread.Sleep(3000);
                if (_listener != null)
                {
                    _listener.Stop();
                    //_listenerThread.Interrupt();
                    //_listenerThread.Join(5000);
                }
                Initilized = false;
            }
            catch (ThreadInterruptedException ex)
            {
                /* Clean up. */
                OnFault("The tcp server on Stop throws ThreadInterruptedException: ", ex);
            }
            catch (Exception ex)
            {
                OnFault("The tcp server listener on Stop throws the error: ", ex);
            }
        }

        public void Stop()
        {
            Listen = false;
            StopInternal();
            Initilized = false;
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
            if (client != null && client.Connected)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    if (client != null)
                    {
                        var ack = FaultAck(reason);
                        WriteResponse(stream, ack);
                        Close(client);
                    }
                }
                catch (Exception ex)
                {
                    OnFault("Tcp listener ExecFault error: ", ex);
                }
                finally
                {
                    Close(client);
                }
            }
        }
        /// <summary>
        /// Create fault ack.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        protected virtual TResponse FaultAck(string reason)
        {
           var tres=   Nistec.Runtime.ActivatorUtil.CreateInstance<TResponse>();
            tres.SetState(-1, reason);
            return tres;

            //return TransStream.WriteState(-1, reason);//.ToStream();
        }
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(NetworkStream stream);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TResponse ExecRequset(TRequest request);

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length==0)
            {
                return;
            }
            stream.Write(bytes, 0, bytes.Length);
        }
#endregion

#region Read/Write Async
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual async Task<TRequest> ReadRequestAsync(NetworkStream stream)
        {
            return await Task.Run(() =>
            {
                return ReadRequest(stream);
            });
        }

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<TResponse> ExecRequsetAsync(TRequest request)
        {
            return await Task.Run(() =>
            {
                return ExecRequset(request);
            });
        }

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual async Task WriteResponseAsync(NetworkStream stream, TResponse bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var bytes = bResponse.GetBytes();
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

#endregion

#region Run Async

        /// <summary>
        /// Occured when client is connected.
        /// </summary>
        protected virtual void OnClientConnected()
        {
            //Console.WriteLine("Debuger-OnTcpClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
        }

        private async void RunAsync()
        {
            //bool hasFault = false;

            while (Listen)
            {
                //TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    //client = await _listener.AcceptTcpClientAsync();

                    //if (IsReady == false)
                    //{
                    //    //hasFault = true;
                    //    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    //    Thread.Sleep(1000);
                    //    continue;
                    //}
                    //connected = true;
                    //OnClientConnected();

                    if (Listen == false)
                    {
                        Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                        Thread.Sleep(1000);
                        return;
                    }

                    //await ProcessIncomingData(client, false);

                    TCP.TcpClient client = await _listener.AcceptTcpClientAsync();
                    if (client.Connected)
                    {
                        OnClientConnected();

                        await Task.Run(async () =>
                        {
                            await ProcessIncomingDataAsync(client, false);
                        // Simulate a long-running task
                        //await Task.WaitAny();// ..Delay(1000);
                        //Console.WriteLine("Done with the async task!");
                        });
                    }

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }
                
            }
        }

        /*
        private void Run()
        {
            //bool hasFault = false;

            while (Listen)
            {
                TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    client = _listener.AcceptTcpClient();

                    if (IsReady == false)
                    {
                        //hasFault = true;
                        ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                        Thread.Sleep(1000);
                        continue;
                    }
                    //connected = true;
                    OnClientConnected();

                    //using (NetworkStream stream = client.GetStream())
                    //{
                    //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                    //    var res = ExecRequset(req);
                    //    if (req.IsDuplex)
                    //        WriteResponse(stream, res);
                    //}
                    //sockeErrors = 0;

                    //ProcessIncomingData(client,true);

                    Task task = Task.Factory.StartNew(() => ProcessIncomingData(client, false));
                    {
                        task.Wait();
                        //if (task.IsCompleted)
                        //{
                        //    Console.WriteLine("ProcessIncomingData completed");
                        //}
                    }
                    //task.TryDispose();

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }
                finally
                {
                    //Close(client);
                }
            }

        }
#pragma warning disable CS0649
        private class ServerCom : IDisposable
        {
            public long Uid;
            public TCP.TcpClient client;
            public ManualResetEvent ManualReset;

            public void Dispose()
            {
                if (ManualReset != null)
                {
                    ManualReset.Close();
                    ManualReset.Dispose();
                    ManualReset = null;
                }

                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                    //client.Close();
                    client = null;
                }
            }
        }

        private void RunAsync()
        {

            while (Listen)
            {

                try
                {

                    tcpClientConnected.Reset();
                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), _listener);
                    //connected = true;
                    //OnClientConnected();
                    tcpClientConnected.WaitOne();

                }
                catch (SocketException se)
                {
                    sockeErrors++;
                    OnFault("The tcp server async throws SocketException: {0}", se);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    OnFault("The tcp server async throws the error: ", ex);
                }
            }
        }

        async void ProcessIncomingConnection(IAsyncResult ar)
        {
            TcpListener listener = null;
            TCP.TcpClient client = null;

            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingConnection not lisenning... ");
                    Thread.Sleep(1000);
                    return;
                }

                listener = (TcpListener)ar.AsyncState;
                client = listener.EndAcceptTcpClient(ar);

                if (IsReady == false)
                {
                    //hasFault = true;
                    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    Thread.Sleep(1000);
                    return;
                }
                OnClientConnected();

                //using (NetworkStream stream = client.GetStream())
                //{
                //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                //    var res = ExecRequset(req);
                //    if (req.IsDuplex)
                //        WriteResponse(stream, res);
                //}
                //sockeErrors = 0;

                //ProcessIncomingData(client, readtimeout, ReceiveBufferSize,true);

                await ProcessIncomingData (client, false);

                //Task task = Task.Factory.StartNew(() => ProcessIncomingData(client, false));
                //{
                //    task.Wait();
                //    //if(task.IsCompleted)
                //    //{
                //    //    Console.WriteLine("ProcessIncomingConnection completed");
                //    //}
                //}
                //task.TryDispose();
            }
            catch (SocketException se)
            {
                int val = Interlocked.Increment(ref sockeErrors);
                OnFault("The tcp server async throws SocketException: {0}", se);
                ExecFault(client, "The tcp server throws Exception: " + se.Message);
                if (val > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                {
                    Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                    listener.Stop();
                }
                OnFault("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);
            }
            catch (Exception ex)
            {
                ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                OnFault("The tcp server async ProcessIncomingConnection throws the error: ", ex);
            }
            finally
            {
                //Close(client);
                tcpClientConnected.Set();
            }
        }
        */

        async Task ProcessIncomingDataAsync(TCP.TcpClient client, bool enableException)
        {
            int count = 0;
            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingData not lisetnning... ");
                    Thread.Sleep(1000);
                    return;
                }

                using (NetworkStream stream = client.GetStream())
                {
                    while (count < 3)
                    {
                        if (stream.DataAvailable)
                        {
                            count = 3;
                            TRequest req = await ReadRequestAsync(stream);

                            var res = await ExecRequsetAsync(req);
                            if (req.DuplexType.IsDuplex())
                                WriteResponse(stream, res);
                        }
                        else
                        {
                            count++;
                            Console.WriteLine("stream is not DataAvailable, retry: " + count.ToString());
                            Thread.Sleep(400);
                            if (count >= 3)
                                OnFault("The tcp server async ProcessIncomingData failed", new Exception("stream is not DataAvailable, After several attempts: " + count.ToString()));
                        }
                    }
                }
                sockeErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                else
                    OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
            }
            finally
            {
                Close(client);
            }
        }

        void Close(TCP.TcpClient client)
        {
            try
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
            catch (Exception ex)
            {
                OnFault("Close TcpClient error ", ex);
            }
        }
#endregion
    }

    /// <summary>
    /// Represent a tcp server listner.
    /// </summary>
    public abstract class TcpServerAsync : TcpServerAsync<TcpMessage>
    {
#region ctor

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        protected TcpServerAsync(string hostName, int port)
            : base(hostName, port)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpServer"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServerAsync(string configHost)
            : base(configHost)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpServer"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServerAsync(TcpSettings settings)
            : base(settings)
        {

        }
#endregion

#region abstract methods

        /// <summary>
        /// Read Request
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected override TcpMessage ReadRequest(NetworkStream stream)
        {
            return TcpMessage.ServerReadRequest(stream);
        }

#endregion

    }
#endif


#if (false)
   /// <summary>
   /// Represent a base class for tcp server listner.
   /// </summary>
   /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpServer<TRequest> where TRequest : ITransformMessage
    {

#region membrs
        volatile bool Listen;
        private bool Initilized = false;
        private bool IsAsync = true;
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
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        /// <summary>
        /// Get current <see cref="TcpSettings"/> settings.
        /// </summary>
        public bool IsReady { get; protected set; }

#endregion

#region ctor

         /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpServer()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor using hostAddress and port.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        protected TcpServer(string hostAddress,int port)
        {
            Settings = new TcpSettings(hostAddress, port);
        }

        /// <summary>
        /// Constractor using host configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpServer(string configHost)
        {
            Settings = new TcpSettings(configHost, true);
        }

        /// <summary>
        /// Constractor using settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpServer(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }
#endregion

#region Initilize

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
        IPEndPoint endpoint;
        //bool connected = false;
        int sockeErrors = 0;
        int MAX_SOCKET_ERRORS = TcpSettings.DefaultMaxSocketError;
        private Thread _listenerThread;
        TcpListener _listener;
        private void Init()
        {

            if (Initilized)
                return;
            IsReady = false;
            endpoint = Settings.GetEndpoint();
            MAX_SOCKET_ERRORS = Settings.MaxSocketError;
            IsAsync = Settings.IsAsync;
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


        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
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

                Listen = true;
                Init();
                _State = ChannelServiceState.Started;
                OnStart();
                StartInternal(IsAsync);
            }
            catch (Exception ex)
            {
                Listen = false;
                _State = ChannelServiceState.None;
                OnFault("The tcp server on start throws the error: ", ex);
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
                Initilized = true;

            }
            catch (Exception ex)
            {
                OnFault("The tcp server async listener on Start throws the error: ", ex);
                return;
            }
        }

        void StopInternal()
        {
            try
            {
                Log.Info("The tcp server listener Stoping...");
                Thread.Sleep(3000);
                if (_listener != null)
                {
                    _listener.Stop();
                    //_listenerThread.Interrupt();
                    //_listenerThread.Join(5000);
                }
                Initilized = false;
            }
            catch (ThreadInterruptedException ex)
            {
                /* Clean up. */
                OnFault("The tcp server on Stop throws ThreadInterruptedException: ", ex);
            }
            catch (Exception ex)
            {
                OnFault("The tcp server listener on Stop throws the error: ", ex);
            }
        }

        public void Stop()
        {
            Listen = false;
            StopInternal();
            Initilized = false;
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
            if (client != null && client.Connected)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    if (client != null)
                    {
                        var ack = FaultAck(reason);
                        WriteResponse(stream, ack);
                        Close(client);
                    }
                }
                catch (Exception ex)
                {
                    OnFault("Tcp listener ExecFault error: ", ex);
                }
                finally
                {
                    Close(client);
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
            return TransStream.WriteState(-1, reason);//.ToStream();
            //return TransStream.Write(new TcpMessage("Fault", "ack", reason, 0), TransType.Object);//.ToStream();
        }
        /// <summary>
        /// Read Request from client.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected abstract TRequest ReadRequest(NetworkStream stream);

        /// <summary>
        /// Exec client requset.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract TransStream ExecRequset(TRequest request);

        /// <summary>
        /// Write response to client.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bResponse"></param>
        protected virtual void WriteResponse(NetworkStream stream, TransStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }
            var ns = bResponse.GetStream();
            if(ns==null)
            {
                return;
            }
            int lenth = ns.iLength;
            //stream.WriteValue(cbResponse);
            stream.Write(ns.ToArray(), 0, lenth);

        }


#endregion

#region Run

        /// <summary>
        /// Occured when client is connected.
        /// </summary>
        protected virtual void OnClientConnected()
        {
            //Console.WriteLine("Debuger-OnTcpClientConnected : " + Thread.CurrentThread.ManagedThreadId.ToString());
        }

        private void Run()
        {
            //bool hasFault = false;

            while (Listen)
            {
                TCP.TcpClient client = null;
                try
                {
                    //hasFault = false;
                    if (_State == ChannelServiceState.Paused)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    client = _listener.AcceptTcpClient();
                   
                    if (IsReady == false)
                    {
                        //hasFault = true;
                        ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                        Thread.Sleep(1000);
                        continue;
                    }
                    //connected = true;
                    OnClientConnected();

                    //using (NetworkStream stream = client.GetStream())
                    //{
                    //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                    //    var res = ExecRequset(req);
                    //    if (req.IsDuplex)
                    //        WriteResponse(stream, res);
                    //}
                    //sockeErrors = 0;

                    //ProcessIncomingData(client,readtimeout, ReceiveBufferSize,true);

                    Task task = Task.Factory.StartNew(() => ProcessIncomingData(client, false));
                    {
                        task.Wait();
                        //if (task.IsCompleted)
                        //{
                        //    Console.WriteLine("ProcessIncomingData completed");
                        //}
                    }
                    task.TryDispose();

                    //connected = false;
                    sockeErrors = 0;
                }
                catch (SocketException se)
                {
                    //hasFault = true;
                    sockeErrors++;
                    OnFault("The tcp server throws SocketException: ", se);
                    //ExecFault(client, "The tcp server throws SocketException: " + se.Message);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS>0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    //hasFault = true;
                    OnFault("The tcp server throws the error: ", ex);
                    //ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                }
                finally
                {
                    //Close(client);
                }
            }

        }

        private class ServerCom : IDisposable
        {
            public long Uid;
            public TCP.TcpClient client;
            public ManualResetEvent ManualReset;

            public void Dispose()
            {
                if (ManualReset != null)
                {
                    ManualReset.Close();
                    ManualReset.Dispose();
                    ManualReset = null;
                }

                if (client != null)
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                    //client.Close();
                    client = null;
                }
            }
        }

        private void RunAsync()
        {
            
            while (Listen)
            {

                try
                {

                    tcpClientConnected.Reset();
                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), _listener);
                    //connected = true;
                    //OnClientConnected();
                    tcpClientConnected.WaitOne();

                }
                catch (SocketException se)
                {
                    sockeErrors++;
                    OnFault("The tcp server async throws SocketException: {0}", se);
                    if (sockeErrors > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                    {
                        Log.Error("The tcp server shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    OnFault("The tcp server async throws the error: ", ex);
                }
            }
        }

        void ProcessIncomingConnection(IAsyncResult ar)
        {
            TcpListener listener = null;
            TCP.TcpClient client = null;

            try
            {
                if(Listen==false)
                {
                    Log.Warn("The tcp server async ProcessIncomingConnection not lisenning... ");
                    Thread.Sleep(1000);
                    return;
                }

                listener = (TcpListener)ar.AsyncState;
                client = listener.EndAcceptTcpClient(ar);

                if (IsReady == false)
                {
                    //hasFault = true;
                    ExecFault(client, "The tcp server is not ready to accept client requests, please wait for server to be ready.");
                    Thread.Sleep(1000);
                    return;
                }
                OnClientConnected();

                //using (NetworkStream stream = client.GetStream())
                //{
                //    TRequest req = ReadRequest(stream, readtimeout, ReceiveBufferSize);

                //    var res = ExecRequset(req);
                //    if (req.IsDuplex)
                //        WriteResponse(stream, res);
                //}
                //sockeErrors = 0;

                //ProcessIncomingData(client, readtimeout, ReceiveBufferSize,true);

                Task task = Task.Factory.StartNew(() => ProcessIncomingData(client, false));
                {
                    task.Wait();
                    //if(task.IsCompleted)
                    //{
                    //    Console.WriteLine("ProcessIncomingConnection completed");
                    //}
                }
                task.TryDispose();
            }
            catch (SocketException se)
            {
                int val = Interlocked.Increment(ref sockeErrors);
                OnFault("The tcp server async throws SocketException: {0}", se);
                ExecFault(client, "The tcp server throws Exception: " + se.Message);
                if (val > MAX_SOCKET_ERRORS && MAX_SOCKET_ERRORS > 0)
                {
                    Log.Error("The tcp server async ProcessIncomingConnection shutdown after {0} errors ", MAX_SOCKET_ERRORS);
                    listener.Stop();
                }
                OnFault("The tcp server async ProcessIncomingConnection throws SocketException: {0}", se);
            }
            catch (Exception ex)
            {
                ExecFault(client, "The tcp server throws Exception: " + ex.Message);
                OnFault("The tcp server async ProcessIncomingConnection throws the error: ", ex);
            }
            finally
            {
                //Close(client);
                tcpClientConnected.Set();
            }
        }

   
        void ProcessIncomingData(TCP.TcpClient client, bool enableException)
        {
            try
            {
                if (Listen == false)
                {
                    Log.Warn("The tcp server async ProcessIncomingData not lisenning... ");
                    Thread.Sleep(1000);
                    return;
                }
                using (NetworkStream stream = client.GetStream())
                {
                    TRequest req = ReadRequest(stream);

                    var res = ExecRequset(req);
                    if (req.IsDuplex)
                        WriteResponse(stream, res);
                }
                sockeErrors = 0;
                //connected = false;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                else
                    OnFault("The tcp server async ProcessIncomingData throws the error: ", ex);
            }
            finally
            {
                Close(client);
            }
        }

        void Close(TCP.TcpClient client)
        {
            try
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
            catch (Exception ex)
            {
                OnFault("Close TcpClient error ", ex);
            }
        }
#endregion
    }

#endif