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

using System;
using System.IO.Pipes;
using System.Text;
using System.IO;
using Nistec.Generic;
using Nistec.Runtime;
using System.Collections;
using Nistec.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Net.Sockets;
using TCP = System.Net.Sockets;
using System.Net;
using Nistec.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using Nistec.Serialization;

namespace Nistec.Channels.Tcp
{
    /// <summary>
    /// Represent a base class for tcp client.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpClient<TRequest> : IDisposable where TRequest: ITransformMessage
    {
        #region members
        protected TCP.TcpClient tcpClient = null;
        const int MaxRetry = 3;
        #endregion

        #region settings
        /// <summary>
        /// Get or Set <see cref="TcpSettings"/> Settings.
        /// </summary>
        public TcpSettings Settings { get; set; }
        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        #endregion

        #region ctor

         /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpClient()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="isAsync"></param>
        protected TcpClient(string hostAddress, int port, int connectTimeout, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort)
            };
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="isAsync"></param>
        protected TcpClient(string hostAddress, int port,int connectTimeout, int readTimeout, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress, 
                Address=hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.EnsureReadTimeout(readTimeout),
                IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort)
            };
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpClient(string configHost)
        {

            Settings = TcpClientSettings.GetTcpClientSettings(configHost);//, false);
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpClient(TcpSettings settings)
        {
            Settings = settings;
            Log = settings.Log;
        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="sendBufferSize"></param>
        /// <param name="isAsync"></param>
        protected TcpClient(string hostAddress, int port, int connectTimeout, int receiveBufferSize, int sendBufferSize, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                IsAsync = isAsync,
                Port=Types.NZero(port, TcpSettings.DefaultPort),
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }


        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="sendBufferSize"></param>
        /// <param name="isAsync"></param>
        protected TcpClient(string hostAddress, int port,int connectTimeout, int readTimeout, int receiveBufferSize, int sendBufferSize, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address=hostAddress,
                IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = readTimeout,
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }

        
        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (tcpClient != null)
            {
                if (tcpClient.Connected)
                    tcpClient.Close();
                tcpClient = null;
            }
        }
        #endregion

        #region Read/Write

        protected abstract object ExecuteMessage(NetworkStream stream, TRequest message);//, Type type);

        protected abstract void ExecuteOneWay(NetworkStream stream, TRequest message);

        protected abstract TResponse ExecuteMessage<TResponse>(NetworkStream stream, TRequest message);


        #endregion

        #region Run

        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
        }

        void ConnectAsync()
        {
            tcpClient = SocketConnector.Connect(Settings.GetEndpoint(), Settings.ConnectTimeout);
            tcpClient.SendTimeout = Settings.ConnectTimeout;
            tcpClient.SendBufferSize = Settings.SendBufferSize;
            tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
            tcpClient.ReceiveTimeout = Settings.ReadTimeout;
        }

        bool Connect()
        {
            int retry = 0;
            
            IPEndPoint ep = new IPEndPoint(Settings.HostAddress, Settings.Port);
            tcpClient = new TCP.TcpClient();
            tcpClient.SendTimeout =Settings.ConnectTimeout;
            tcpClient.SendBufferSize = Settings.SendBufferSize;
            tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
            tcpClient.ReceiveTimeout = Settings.ReadTimeout;

            while (retry <= MaxRetry)
            {

                try
                {
                    tcpClient.Connect(ep);
                    if (!tcpClient.Connected)
                    {
                        retry++;
                        if (retry >= MaxRetry)
                        {
                            throw new Exception("Unable to connect to tcp address: " + Settings.HostName);
                        }
                        Thread.Sleep(10);

                    }
                    else
                    {
                        return true;
                    }
                }
                catch (TimeoutException toex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, Settings.ConnectTimeout, toex.Message);
                        throw toex;
                    }
                    retry++;
                }
                catch (Exception pex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("TcpClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
                        throw pex;
                    }
                    retry++;
                }
            }

            return tcpClient.Connected;
        }

        ///// <summary>
        ///// connect to the host and execute request.
        ///// </summary>
        //public void ExecuteOut(TRequest message, Type type, bool enableException = false)
        //{
        //    Execute(message, enableException);
        //}

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public object Execute(TRequest message,  bool enableException = false)//Type type,
        {

            object response = null;

            try
            {
                if (Settings.IsAsync)
                    ConnectAsync();
                else
                    Connect();

                return ExecuteMessage(tcpClient.GetStream(),message);

            }
            catch (SocketException se)
            {
                Log.Exception("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                return response;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The tcp client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                return response;
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                return response;
            }
            catch (ChannelException mex)
            {
                Log.Exception("The tcp client throws the MessageException : ", mex, true);
                if (enableException)
                    throw mex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The tcp client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
            finally
            {
                // Close the pipe.
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                        tcpClient.Close();
                    tcpClient = null;
                }
            }
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public void ExecuteOut(TRequest message, bool enableException = false)
        {

            try
            {
                if (Settings.IsAsync)
                    ConnectAsync();
                else
                    Connect();

                ExecuteOneWay(tcpClient.GetStream(), message);

            }
            catch (ChannelException mex)
            {
                Log.Exception("The tcp client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
            }
            catch (SocketException se)
            {
                Log.Exception("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The tcp client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
            }
            catch (Exception ex)
            {
                Log.Exception("The tcp client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

            }
            finally
            {
                // Close the pipe.
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                        tcpClient.Close();
                    tcpClient = null;
                }
            }
        }


        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public TResponse Execute<TResponse>(TRequest message, bool enableException = false)
        {

            TResponse response = default(TResponse);
            try
            {
                if (Settings.IsAsync)
                    ConnectAsync();
                else
                    Connect();

                //Console.WriteLine("SendDuplexStream-LocalEndPoint: {0}", tcpClient.Client.LocalEndPoint.ToString());

                if (message.IsDuplex)
                    return ExecuteMessage<TResponse>(tcpClient.GetStream(), message);
                else
                {
                    ExecuteOneWay(tcpClient.GetStream(), message);
                    return default(TResponse);
                }
            }
            catch (ChannelException mex)
            {
                Log.Exception("The tcp client throws the MessageException : ", mex, true);
                if (enableException)
                    throw mex;
                return response;
            }
            catch (SocketException se)
            {
                Log.Exception("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                return response;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The tcp client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                return response;
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The tcp client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
            finally
            {
                // Close the pipe.
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                        tcpClient.Close();
                    tcpClient = null;
                }
            }
        }

        public void ExecuteAsync<TResponse>(TRequest message,Action<TResponse> onCompleted, bool enableException = false)
        {

            TResponse response = default(TResponse);
            try
            {
                if (Settings.IsAsync)
                    ConnectAsync();
                else
                    Connect();

                //Console.WriteLine("SendDuplexStream-LocalEndPoint: {0}", tcpClient.Client.LocalEndPoint.ToString());

                if (message.IsDuplex)
                {
                    response = ExecuteMessage<TResponse>(tcpClient.GetStream(), message);
                    onCompleted(response);
                }
                else
                {
                    ExecuteOneWay(tcpClient.GetStream(), message);
                    onCompleted(default(TResponse));
                }
            }
            catch (ChannelException mex)
            {
                Log.Exception("The tcp client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
                onCompleted(response);
            }
            catch (SocketException se)
            {
                Log.Exception("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                onCompleted( response);
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The tcp client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                onCompleted(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                onCompleted(response);
            }
            catch (Exception ex)
            {
                Log.Exception("The tcp client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                onCompleted(response);
            }
            finally
            {
                // Close the pipe.
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                        tcpClient.Close();
                    tcpClient = null;
                }
            }
        }
        #endregion

    }

    /// <summary>
    /// Represent a tcp socket connector for tcp client.
    /// </summary>
    public static class SocketConnector
    {
        private static bool IsConnected = false;
        //private static SocketException socketexception;
        private static ChannelException socketexception;
        private static ManualResetEvent tcpConnector = new ManualResetEvent(false);

        /// <summary>
        /// Connect asynchronaizly to tcp server.
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TCP.TcpClient Connect(IPEndPoint remoteEndPoint, int timeout)
        {
            tcpConnector.Reset();
            socketexception = null;

            string serverIp = Convert.ToString(remoteEndPoint.Address);
            int port = remoteEndPoint.Port;
            TCP.TcpClient tcpClient = new TCP.TcpClient();

            tcpClient.BeginConnect(serverIp, port, new AsyncCallback(CallBackMethod), tcpClient);

            if (tcpConnector.WaitOne(timeout, false))
            {
                if (IsConnected)
                {
                    return tcpClient;
                }
                else
                {
                    throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp address: " + serverIp);// socketexception;
                }
            }
            else
            {
                tcpClient.Close();
                throw new TimeoutException("TimeOut Exception, Unable to connect to tcp address: " + serverIp);
            }
        }
        private static void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                IsConnected = false;
                if (asyncresult != null)
                {
                    TCP.TcpClient tcpclient = asyncresult.AsyncState as TCP.TcpClient;

                    if (tcpclient.Client != null)
                    {
                        tcpclient.EndConnect(asyncresult);
                        IsConnected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                socketexception = new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp, using asyncresult", ex);
                //throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp, using asyncresult", ex);// socketexception;
            }
            finally
            {
                tcpConnector.Set();
            }
        }
    }

    /// <summary>
    /// Represent tcp client.
    /// </summary>
    public class TcpClient : TcpClient<MessageStream>, IDisposable
    {
        static readonly Dictionary<string, TcpClient> ClientsCache = new Dictionary<string, TcpClient>();
        static TcpClient GetClient(string hostName)
        {
            TcpClient client = null;
            if (ClientsCache.TryGetValue(hostName, out client))
            {
                return client;
            }
            client = new TcpClient(hostName);
            if (client == null)
            {
                throw new Exception("Invalid configuration for tcp client with host name:" + hostName);
            }
            ClientsCache[hostName] = client;
            return client;
        }

        #region static send methods

        public static bool Ping(string HostAddress, int Port, int ConnectTimeout = 5000)
        {
           
            TCP.TcpClient tcpClient = null;
            string rawAddress = HostAddress;
            try
            {
                rawAddress = string.Format("{0}:{1}", HostAddress,Port);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(HostAddress), Port);
                tcpClient = new TCP.TcpClient();
                tcpClient.SendTimeout = ConnectTimeout;
                tcpClient.SendBufferSize = TcpSettings.DefaultSendBufferSize;
                tcpClient.ReceiveBufferSize = TcpSettings.DefaultReceiveBufferSize;
                tcpClient.ReceiveTimeout = TcpSettings.DefaultReadTimeout;
                tcpClient.Connect(ep);

                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp address: " + rawAddress);
                }
                else
                {
                    tcpClient.Close();
                    return true;
                }
            }
            catch (TimeoutException toex)
            {
                throw new ChannelException(ChannelState.TimeoutError, "Unable to connect to tcp address: " + rawAddress, toex);
            }
            catch (Exception pex)
            {
                throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp address: " + rawAddress, pex);
            }
        }

        /// <summary>
        /// Send Duplex
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static TransStream SendDuplexStream(MessageStream request, string hostName, bool enableException = false)
        {
            request.IsDuplex = true;
            request.TransformType = TransformType.Stream;
            using (TcpClient client = new TcpClient(hostName))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string HostAddress, int port, int connectTimeout, int readTimeout, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static void SendDuplexStreamAsync(MessageStream request, string HostAddress, int port, int connectTimeout, Action<TransStream> onCompleted, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, IsAsync))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static void SendDuplexStreamAsync(MessageStream request, string HostAddress, int port, int connectTimeout, int readTimeout, Action<TransStream> onCompleted, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static string SendJsonDuplex(string json, string HostAddress, int port, int connectTimeout, int readTimeout, bool IsAsync, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json,null);
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {
                var response= client.Execute(message, enableException);
                return JsonSerializer.Serialize(response);
            }
        }

        public static object SendJsonDuplex(string json, string HostName, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json, null);
            using (TcpClient client = new TcpClient(HostName))
            {
                return client.Execute(message, enableException);
            }
        }

        public static object SendDuplex(MessageStream request, string HostAddress,int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, IsAsync))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageStream request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, IsAsync))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, IsAsync))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static object SendDuplex(MessageStream request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostName))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageStream request, string HostName, bool enableException = false)
        {
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostName))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpClient client = new TcpClient(HostName))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostAddress, int Port, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpClient client = new TcpClient(HostAddress, Port))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        public TcpClient(string hostAddress, int port)
            : base(hostAddress, port, TcpSettings.DefaultConnectTimeout, TcpSettings.DefaultReadTimeout, false)
        {

        }
        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="isAsync"></param>
        public TcpClient(string hostAddress, int port, int connectTimeout, bool isAsync)
            : base(hostAddress, port, connectTimeout, isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="isAsync"></param>
        public TcpClient(string hostAddress, int port,int connectTimeout,int readTimeout, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout, isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isAsync"></param>
        public TcpClient(string hostAddress, int port, int connectTimeout, int readTimeout, int inBufferSize, int outBufferSize, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout, inBufferSize, outBufferSize, isAsync)
        {

        }
       
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        public TcpClient(string configHost)
            : base(configHost)
        {
           
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        public TcpClient(TcpSettings settings)
            : base(settings)
        {

        }

        #endregion

        #region override

        protected override void ExecuteOneWay(NetworkStream stream, MessageStream message)
        {
            // Send a request from client to server
            message.EntityWrite(stream, null);
        }

        protected override object ExecuteMessage(NetworkStream stream, MessageStream message)//, Type type)
        {
            object response = null;

            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.IsDuplex == false)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadResponse(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize, false);

            return response;
        }
        //protected override object ExecuteMessage(NetworkStream stream, MessageStream message)//, Type type)
        //{
        //    object response = null;

        //    // Send a request from client to server
        //    message.EntityWrite(stream, null);

        //    if (message.IsDuplex == false)
        //    {
        //        return response;
        //    }

        //    // Receive a response from server.
        //    response = message.ReadResponse(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize, message.TransformType, false);

        //    return response;
        //}
        protected override TResponse ExecuteMessage<TResponse>(NetworkStream stream, MessageStream message)
        {
            TResponse response = default(TResponse);

            // Send a request from client to server
            message.EntityWrite(stream, null);
           
            if (message.IsDuplex == false)
            {
                return response;
            }

            // Receive a response from server.
            
            response = message.ReadResponse<TResponse>(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

            return response;
        }

        /// <summary>
        /// connect to the tcp channel and execute request.
        /// </summary>
        public new MessageAck  Execute(MessageStream message, bool enableException = false)
        {
            return Execute<MessageAck>(message, enableException);
        }

        #endregion
  
    }

   
}