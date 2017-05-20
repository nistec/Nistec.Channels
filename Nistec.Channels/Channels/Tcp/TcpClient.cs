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
using TCP=System.Net.Sockets;
using System.Net;
using Nistec.Logging;
using System.Collections.Generic;
using System.Diagnostics;


namespace Nistec.Channels.Tcp
{
    /// <summary>
    /// Represent a base class for tcp client.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpClient<TRequest> : IDisposable
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
        /// <param name="readTimeout"></param>
        /// <param name="isAsync"></param>
        protected TcpClient(string hostAddress, int port,int readTimeout, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress, 
                Address=hostAddress,
                ReadTimeout=readTimeout,
                IsAsync = isAsync,
                Port = port
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
        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="readTimeout"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="sendBufferSize"></param>
        /// <param name="isAsync"></param>
        protected TcpClient(string hostAddress, int port,int readTimeout, int receiveBufferSize, int sendBufferSize, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address=hostAddress,
                IsAsync = isAsync,
                Port = port,
                ReadTimeout=readTimeout,
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

        protected abstract object ExecuteMessage(NetworkStream stream, TRequest message, Type type);

        protected abstract void ExecuteMessage(NetworkStream stream, TRequest message);

        protected abstract TResponse ExecuteMessage<TResponse>(NetworkStream stream, TRequest message);


        #endregion

        #region Run

        void ConnectAsync()
        {
            tcpClient = SocketConnector.Connect(Settings.GetEndpoint(), Settings.SendTimeout);
            tcpClient.SendTimeout = Settings.SendTimeout;
            tcpClient.SendBufferSize = Settings.SendBufferSize;
            tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
        }

        bool Connect()
        {
            int retry = 0;
            
            IPEndPoint ep = new IPEndPoint(Settings.HostAddress, Settings.Port);
            tcpClient = new TCP.TcpClient();
            tcpClient.SendTimeout =Settings.SendTimeout;
            tcpClient.SendBufferSize = Settings.SendBufferSize;
            tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;

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
                        Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, Settings.SendTimeout, toex.Message);
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

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public void ExecuteOut(TRequest message, Type type, bool enableException = false)
        {
            Execute(message, type, enableException);
        }

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public object Execute(TRequest message, Type type, bool enableException = false)
        {

            object response = null;

            try
            {
                if (Settings.IsAsync)
                    ConnectAsync();
                else
                    Connect();

                return ExecuteMessage(tcpClient.GetStream(),message, type);

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
            catch (MessageException mex)
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
        public void Execute(TRequest message, bool enableException = false)
        {

            try
            {
                if (Settings.IsAsync)
                    ConnectAsync();
                else
                    Connect();

                ExecuteMessage(tcpClient.GetStream(), message);

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
            catch (MessageException mex)
            {
                Log.Exception("The tcp client throws the MessageException : ", mex, true);
                if (enableException)
                    throw mex;
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

                return ExecuteMessage<TResponse>(tcpClient.GetStream(), message);

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
            catch (MessageException mex)
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

 
        #endregion
    }

    /// <summary>
    /// Represent a tcp socket connector for tcp client.
    /// </summary>
    public static class SocketConnector
    {
        private static bool IsConnected = false;
        private static Exception socketexception;
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
                    throw socketexception;
                }
            }
            else
            {
                tcpClient.Close();
                throw new TimeoutException("TimeOut Exception");
            }
        }
        private static void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                IsConnected = false;
                TCP.TcpClient tcpclient = asyncresult.AsyncState as TCP.TcpClient;

                if (tcpclient.Client != null)
                {
                    tcpclient.EndConnect(asyncresult);
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
    /// Represent tcp client.
    /// </summary>
    public class TcpClient : TcpClient<TcpMessage>, IDisposable
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

        public static object SendDuplex(TcpMessage request, string HostAddress,int port, int readTimeout, bool IsAsync, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port,readTimeout, IsAsync))
            {
                return client.Execute(request, type, enableException);
            }
        }

        public static T SendDuplex<T>(TcpMessage request, string HostAddress, int port, int readTimeout, bool IsAsync, bool enableException = false)
        {
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostAddress, port,readTimeout, IsAsync))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(TcpMessage request, string HostAddress, int port, int readTimeout, bool IsAsync, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpClient client = new TcpClient(HostAddress, port,readTimeout, IsAsync))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static object SendDuplex(TcpMessage request, string HostName, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostName))
            {
                return client.Execute(request, type, enableException);
            }

        }

        public static T SendDuplex<T>(TcpMessage request, string HostName, bool enableException = false)
        {
            request.IsDuplex = true;
            using (TcpClient client = new TcpClient(HostName))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(TcpMessage request, string HostName, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpClient client = new TcpClient(HostName))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static void SendOut(TcpMessage request, string HostAddress, int Port, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpClient client = new TcpClient(HostAddress, Port))
            {
                client.Execute(request, type, enableException);
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
            : base(hostAddress, port, TcpSettings.DefaultReadTimeout, false)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="readTimeout"></param>
        /// <param name="isAsync"></param>
        public TcpClient(string hostAddress, int port,int readTimeout, bool isAsync)
            : base(hostAddress, port, readTimeout,isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="readTimeout"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isAsync"></param>
        public TcpClient(string hostAddress, int port,int readTimeout, int inBufferSize, int outBufferSize, bool isAsync)
            : base(hostAddress, port, readTimeout,inBufferSize, outBufferSize, isAsync)
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

        protected override void ExecuteMessage(NetworkStream stream, TcpMessage message)
        {
            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.IsDuplex == false)
            {
                return;
            }

            // Receive a response from server.
            message.ReadAck(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize);
        }

        protected override object ExecuteMessage(NetworkStream stream, TcpMessage message, Type type)
        {
            object response = null;

            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.IsDuplex == false)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadAck(stream, type, Settings.ReadTimeout, Settings.ReceiveBufferSize);

            return response;
        }

        protected override TResponse ExecuteMessage<TResponse>(NetworkStream stream, TcpMessage message)
        {
            TResponse response = default(TResponse);

            // Send a request from client to server
            message.EntityWrite(stream, null);
           
            if (message.IsDuplex == false)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadAck<TResponse>(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

            return response;
        }

        /// <summary>
        /// connect to the tcp channel and execute request.
        /// </summary>
        public new MessageAck  Execute(TcpMessage message, bool enableException = false)
        {
            return Execute<MessageAck>(message, enableException);
        }

        #endregion

  
    }
   
}