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
using System.Net.NetworkInformation;
using System.Threading.Tasks;
#pragma warning disable CS1591
namespace Nistec.Channels.Tcp
{

#if (false)
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
        protected TcpClient(string hostAddress, int port, int connectTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                //IsAsync = isAsync,
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
        protected TcpClient(string hostAddress, int port,int connectTimeout, int readTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress, 
                Address=hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.EnsureReadTimeout(readTimeout),
                //IsAsync = isAsync,
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
        protected TcpClient(string hostAddress, int port, int connectTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
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
        protected TcpClient(string hostAddress, int port,int connectTimeout, int readTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address=hostAddress,
                //IsAsync = isAsync,
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

        protected abstract void ExecuteMessage<TResponse>(NetworkStream stream, TRequest message, Action<TResponse> onCompleted);

        protected virtual async Task ExecuteMessageAsync<TResponse>(NetworkStream stream, TRequest message, Action<TResponse> onCompleted)
        {
            await Task.Run(() =>
            {
                ExecuteMessage<TResponse>(stream, message, onCompleted);
                //onCompleted.Invoke(response);
            }).ConfigureAwait(false);
        }

        protected virtual async Task ExecuteOneWayAsync(NetworkStream stream, TRequest message)
        {
            await Task.Run(() =>
            {
                ExecuteOneWay(stream, message);
            }).ConfigureAwait(false);
        }

    #endregion

    #region Run

        protected virtual void OnFault(string message, Exception ex)
        {
            Log.Exception(message, ex, true);
        }

        //void ConnectAsync()
        //{
        //    tcpClient = SocketConnector.Connect(Settings.GetEndpoint(), Settings.ConnectTimeout);
        //    tcpClient.SendTimeout = Settings.ConnectTimeout;
        //    tcpClient.SendBufferSize = Settings.SendBufferSize;
        //    tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
        //    tcpClient.ReceiveTimeout = Settings.ReadTimeout;
        //}

        async Task<bool> ConnectAsync()
        {
            int retry = 0;

            IPEndPoint ep = new IPEndPoint(Settings.HostAddress, Settings.Port);
            tcpClient = new TCP.TcpClient();
            tcpClient.SendTimeout = Settings.ConnectTimeout;
            tcpClient.SendBufferSize = Settings.SendBufferSize;
            tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
            tcpClient.ReceiveTimeout = Settings.ReadTimeout;

            while (retry <= MaxRetry)
            {

                try
                {
                    Task connectTask = tcpClient.ConnectAsync(ep.Address, ep.Port);
                    Task timeoutTask = Task.Delay(millisecondsDelay: Settings.ConnectTimeout);
                    if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    {
                        retry++;
                        if (retry >= MaxRetry)
                        {
                            throw new TimeoutException("Unable to connect to tcp address: " + Settings.HostName);
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
                //if (Settings.IsAsync)
                //    ConnectAsync();
                //else
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
                //if (Settings.IsAsync)
                //    ConnectAsync();
                //else
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
                //if (Settings.IsAsync)
                //    ConnectAsync();
                //else
                 Connect();

                //Console.WriteLine("SendDuplexStream-LocalEndPoint: {0}", tcpClient.Client.LocalEndPoint.ToString());

                if (message.DuplexType.IsDuplex())
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

        public void Execute<TResponse>(TRequest message,Action<TResponse> onCompleted, bool enableException = false)
        {

            TResponse response = default(TResponse);
            try
            {
                if (Settings.IsAsync)
                    ConnectAsync().ConfigureAwait(false);
                else
                    Connect();

                //Console.WriteLine("SendDuplexStream-LocalEndPoint: {0}", tcpClient.Client.LocalEndPoint.ToString());

                if (message.DuplexType.IsDuplex())
                {
                    response = ExecuteMessage<TResponse>(tcpClient.GetStream(), message);
                    onCompleted.Invoke(response);
                }
                else
                {
                    ExecuteOneWay(tcpClient.GetStream(), message);
                    onCompleted.Invoke(default(TResponse));
                }
            }
            catch (ChannelException mex)
            {
                Log.Exception("The tcp client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
                onCompleted.Invoke(response);
            }
            catch (SocketException se)
            {
                Log.Exception("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                onCompleted.Invoke( response);
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The tcp client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                onCompleted.Invoke(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                onCompleted.Invoke(response);
            }
            catch (Exception ex)
            {
                Log.Exception("The tcp client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                onCompleted.Invoke(response);
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

    #region Async

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public async Task ExecuteOutAsync(TRequest message, bool enableException = false)
        {

            try
            {
                //Settings.IsAsync = true;
                await ExecuteOneWayAsync(tcpClient.GetStream(), message);
            }
            catch (Exception ex)
            {
                OnFault("The ExecuteOutAsync tcp client throws the error: ", ex);
                if (enableException)
                    throw ex;
            }
        }

        public async Task ExecuteAsync<TResponse>(TRequest message, Action<TResponse> onCompleted, bool enableException = false)
        {

            //await Task.Run(() =>
            //{
            //    Execute<TResponse>(message, onCompleted, enableException);
            //});

            TResponse response = default(TResponse);
            try
            {
                 await ConnectAsync();

                if (message.DuplexType.IsDuplex())
                {
                    await ExecuteMessageAsync<TResponse>(tcpClient.GetStream(), message, onCompleted);
                    onCompleted.Invoke(response);
                }
                else
                {
                    await ExecuteOneWayAsync(tcpClient.GetStream(), message);
                    onCompleted.Invoke(default(TResponse));
                }
            }
            catch (ChannelException mex)
            {
                OnFault("The tcp client throws the ChannelException : ", mex);
                if (enableException)
                    throw mex;
                onCompleted.Invoke(response);
            }
            catch (SocketException se)
            {
                OnFault("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                onCompleted.Invoke(response);
            }
            catch (TimeoutException toex)
            {
                OnFault("The tcp client throws the TimeoutException : ", toex);
                if (enableException)
                    throw toex;
                onCompleted.Invoke(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex);
                if (enableException)
                    throw sex;
                onCompleted.Invoke(response);
            }
            catch (Exception ex)
            {
                OnFault("The tcp client throws the error: ", ex);

                if (enableException)
                    throw ex;

                onCompleted.Invoke(response);
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
#endif

    public class TcpSocketClient
    {

        #region members and ctor

        //private string ServerIp;
        //private int Port;

        public TcpSocketClient(int port, string serverIp)
        {
            //ServerIp = serverIp;
            //Port = port;

            Settings = new TcpSettings()
            {
                HostName = serverIp,
                Address = serverIp,
                ConnectTimeout = TcpSettings.DefaultConnectTimeout,
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                Port = Types.NZero(port, TcpSettings.DefaultPort)
            };
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        protected TcpSocketClient(string hostAddress, int port, int connectTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
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
        protected TcpSocketClient(string hostAddress, int port, int connectTimeout, int readTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.EnsureReadTimeout(readTimeout),
                Port = Types.NZero(port, TcpSettings.DefaultPort)
            };
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpSocketClient(string configHost)
        {

            Settings = TcpClientSettings.GetTcpClientSettings(configHost);//, false);
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpSocketClient(TcpSettings settings)
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
        protected TcpSocketClient(string hostAddress, int port, int connectTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
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
        protected TcpSocketClient(string hostAddress, int port, int connectTimeout, int readTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = readTimeout,
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }


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
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

        #endregion

        #region client binary

        public async Task<byte[]> SendRequestAsync(byte[] data)
        {
            //try
            //{
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.InfiniteReadTimeout;//Settings.ReadTimeout;
                    clientSocket.SendTimeout = Settings.ConnectTimeout;
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(Settings.Address), Settings.Port));
                    Console.WriteLine("Connected to server!");
                    //byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    return await ReceiveBinaryAsync(clientSocket);
                }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //    return new TransStream(ex.Message, "Fault", TransType.State); //Binary
            //}
        }

        private static async Task<byte[]> ReceiveBinaryAsync(Socket clientSocket)
        {
            byte[] buffer = new byte[4096]; // Chunk size
            int bytesRead;
            using (var stream = new NetStream())
            {
                //while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    if (bytesRead < buffer.Length) break; // End of message
                }
                byte[] receivedData = stream.ToArray();
                //return new NetStream(receivedData);
                //return new NetStream(buffer, 0, bytesRead);
                return receivedData;// new TransStream(receivedData);// buffer, 0, bytesRead); //binary

                //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                //stream.Write(buffer, 0, bytesRead);
                //return new TransBinary(buffer, 0, bytesRead);
            }
        }

        private static byte[] ReceiveBinary(Socket clientSocket)
        {
            byte[] buffer = new byte[4096]; // Chunk size
            int bytesRead;
            using (var stream = new System.IO.MemoryStream())
            {
                while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    if (bytesRead < buffer.Length) break; // End of message
                }
                byte[] receivedData = stream.ToArray();
                //return new NetStream(receivedData);
                return receivedData;// new TransStream(receivedData);//binary
            }
        }

        #endregion

        #region client IDataStream

        public async Task<IDataStream> SendAsync(IDataStream message)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = Settings.ReadTimeout;
                    clientSocket.SendTimeout = Settings.ConnectTimeout;
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(Settings.Address), Settings.Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    return await ReceiveDataStreamAsync(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new TransStream(ex.Message, "Fault", TransType.State); //Binary
            }
        }

        public async Task<IDataStream> SendChunksAsync(IDataStream message)
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                clientSocket.ReceiveTimeout = Settings.ReadTimeout;
                clientSocket.SendTimeout = Settings.ConnectTimeout;
                await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(Settings.Address), Settings.Port));
                Console.WriteLine("Connected to server!");

                byte[] largeBinaryData = new byte[100000]; // Simulating large binary data
                new Random().NextBytes(largeBinaryData); // Fill with random bytes

                int chunkSize = 4096;
                for (int i = 0; i < largeBinaryData.Length; i += chunkSize)
                {
                    int size = Math.Min(chunkSize, largeBinaryData.Length - i);
                    clientSocket.Send(largeBinaryData, i, size, SocketFlags.None);
                }
                Console.WriteLine("Binary data sent.");
                return await ReceiveDataStreamAsync(clientSocket);
            }
        }

        private static async Task<IDataStream> ReceiveDataStreamAsync(Socket clientSocket)
        {
            byte[] buffer = new byte[4096]; // Chunk size
            int bytesRead;
            using (var stream = new NetStream())
            {
                //while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    if (bytesRead < buffer.Length) break; // End of message
                }
                byte[] receivedData = stream.ToArray();
                //return new NetStream(receivedData);
                //return new NetStream(buffer, 0, bytesRead);
                return new TransStream(receivedData);// buffer, 0, bytesRead); //binary

                //bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                //stream.Write(buffer, 0, bytesRead);
                //return new TransBinary(buffer, 0, bytesRead);
            }
        }

        private static IDataStream ReceiveDataStream(Socket clientSocket)
        {
            byte[] buffer = new byte[4096]; // Chunk size
            int bytesRead;
            using (var stream = new System.IO.MemoryStream())
            {
                while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    if (bytesRead < buffer.Length) break; // End of message
                }
                byte[] receivedData = stream.ToArray();
                //return new NetStream(receivedData);
                return new TransStream(receivedData);//binary
            }
        }

        #endregion

        #region static Binary

        public static async Task<byte[]> SendRequestAsync(byte[] data, string ServerIp, int Port)
        {
            //try
            //{
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.InfiniteReadTimeout;//DefaultReadTimeout;
                clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    //byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    return await ReceiveBinaryAsync(clientSocket);
                }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //    return new TransStream(ex.Message, "Fault", TransType.State);//binary
            //}
        }

        public static byte[] Send(byte[] data, string ServerIp, int Port)
        {
            //try
            //{
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                clientSocket.ReceiveTimeout = TcpSettings.InfiniteReadTimeout;// DefaultReadTimeout;
                    clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    //byte[] data = message.GetBytes();
                    clientSocket.Send(data, SocketFlags.None);
                    return ReceiveBinary(clientSocket);
                }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //    return new TransStream(ex.Message, "Fault", TransType.State);//binary
            //}
        }

        public static void Send(byte[] data, string ServerIp, int Port, Action<byte[]> action)
        {
            //try
            //{
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.InfiniteReadTimeout;//DefaultReadTimeout;
                clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    //byte[] data = message.GetBytes();
                    clientSocket.Send(data, SocketFlags.None);
                    action.Invoke(ReceiveBinary(clientSocket));
                }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //    action.Invoke(new TransStream(ex.Message, "Fault", TransType.State));//binary
            //}
        }

        public static async Task SendAsync(byte[] data, string ServerIp, int Port, Action<byte[]> action)
        {

            //try
            //{
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.InfiniteReadTimeout;//DefaultReadTimeout;
                    clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    //byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    var dataStream = await ReceiveBinaryAsync(clientSocket);
                    action.Invoke(dataStream);
                }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //    action.Invoke(new TransStream(ex.Message, "Fault", TransType.State));//binary
            //}
        }

        #endregion

        #region static IDataStream

        public static async Task<IDataStream> SendAsync(IDataStream message, string ServerIp, int Port)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.DefaultReadTimeout;
                    clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    return await ReceiveDataStreamAsync(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new TransStream(ex.Message, "Fault", TransType.State);//binary
            }
        }

        public static IDataStream Send(IDataStream message, string ServerIp, int Port)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.DefaultReadTimeout;
                    clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    clientSocket.Send(data, SocketFlags.None);
                    return ReceiveDataStream(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new TransStream(ex.Message, "Fault", TransType.State);//binary
            }
        }

        public static void Send(IDataStream message, string ServerIp, int Port, Action<IDataStream> action)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.DefaultReadTimeout;
                    clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    clientSocket.Send(data, SocketFlags.None);
                    action.Invoke(ReceiveDataStream(clientSocket));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                action.Invoke(new TransStream(ex.Message, "Fault", TransType.State));//binary
            }
        }

        public static async Task SendAsync(IDataStream message, string ServerIp, int Port, Action<IDataStream> action)
        {

            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.ReceiveTimeout = TcpSettings.DefaultReadTimeout;
                    clientSocket.SendTimeout = TcpSettings.DefaultConnectTimeout;
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    var dataStream = await ReceiveDataStreamAsync(clientSocket);
                    action.Invoke(dataStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                action.Invoke(new TransStream(ex.Message, "Fault", TransType.State));//binary
            }
        }

        #endregion

        #region Client Trans sream

        //public async Task<TransStream> SendAsync(TransStream message)
        //{
        //    try
        //    {
        //        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        //        {
        //            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
        //            Console.WriteLine("Connected to server!");

        //            await SendDataAsync(clientSocket, message);
        //            return await ReceiveTransStreamAsync(clientSocket);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        return new TransStream(ex.Message, "Fault", TransType.State);
        //    }
        //}
        private static void SendData(Socket clientSocket, TransStream message)
        {
            //TransBinary tb = new TransBinary(message, "Enqueue", TransType.Text);
            byte[] data = message.ToStream().ToArray();// Encoding.UTF8.GetBytes(message);
            clientSocket.Send(data, SocketFlags.None);
            Console.WriteLine($"Sent: {message}");
        }

        private static async Task SendDataAsync(Socket clientSocket, TransStream message)
        {
            //TransBinary tb = new TransBinary(message, "Enqueue", TransType.Text);
            byte[] data = message.ToStream().ToArray();// Encoding.UTF8.GetBytes(message);
            await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            Console.WriteLine($"Sent: {message}");
        }

        private static async Task<TransStream> ReceiveTransStreamAsync(Socket clientSocket)
        {
            TransStream tb = null;
            byte[] buffer = new byte[8192];
            int bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
            if (bytesRead > 0)
            {
                tb = new TransStream(buffer, 0, bytesRead, TransType.Stream);
                //var cmd = tb.Command;
                //var type = tb.TypeName;
                //if (tb.TransType == TransType.Text)
                //    Console.WriteLine($"Received: {tb.ReadBody<string>()}");
                //else
                //    Console.WriteLine($"Received: {tb.BodyToJson()}");

                //string response =  Encoding.UTF8.GetString(buffer, 0, bytesRead);
                //Console.WriteLine($"📥 Received: {response}");
            }
            if (tb == null)
                return new TransStream("NA", "Fault", TransType.State);
            return tb;
        }

        private static TransStream ReceiveTransStream(Socket clientSocket)
        {
            TransStream tb = null;
            byte[] buffer = new byte[8192];
            int bytesRead = clientSocket.Receive(buffer, SocketFlags.None);
            if (bytesRead > 0)
            {
                tb = new TransStream(buffer, 0, bytesRead, TransType.Stream);
                //var cmd = tb.Command;
                //var type = tb.TypeName;
                //if (tb.TransType == TransType.Text)
                //    Console.WriteLine($"Received: {tb.ReadBody<string>()}");
                //else
                //    Console.WriteLine($"Received: {tb.BodyToJson()}");

                //string response =  Encoding.UTF8.GetString(buffer, 0, bytesRead);
                //Console.WriteLine($"📥 Received: {response}");
            }
            if (tb == null)
                return new TransStream("NA", "Fault", TransType.State);
            return tb;
        }

        #endregion

        #region static Trans Stream

        //public static void Send(TransStream message, string ServerIp, int Port, Action<TransStream> action)
        //{
        //    try
        //    {
        //        //var args= serverAddress.Split(':');
        //        //string ServerIp = args[0];
        //        //int Port = Types.ToInt(args[1]);
        //        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        //        {
        //            clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
        //            Console.WriteLine("Connected to server!");

        //            SendDataAsync(clientSocket, message).ConfigureAwait(false);
        //            var response = ReceiveTransStreamAsync(clientSocket).GetAwaiter().GetResult();
        //            action(response);//.ToTransStream());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        action(new TransStream(ex.Message, "Fault", TransType.State));//.ToTransStream());
        //    }
        //}

        public static async Task SendAsync(TransStream message, string ServerIp, int Port, Action<TransStream> action)
        {
            try
            {
                //var args= serverAddress.Split(':');
                //string ServerIp = args[0];
                //int Port = Types.ToInt(args[1]);
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");

                    await SendDataAsync(clientSocket, message);
                    var response = await ReceiveTransStreamAsync(clientSocket);
                    action(response);//.ToTransStream());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                action(new TransStream(ex.Message, "Fault", TransType.State));//.ToTransStream());
            }
        }

        public static void Send(TransStream message, string ServerIp, int Port, Action<TransStream> action)
        {
            try
            {
                //var args= serverAddress.Split(':');
                //string ServerIp = args[0];
                //int Port = Types.ToInt(args[1]);
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");

                    SendData(clientSocket, message);
                    var response = ReceiveTransStream(clientSocket);
                    action(response);//.ToTransStream());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                action(new TransStream(ex.Message, "Fault", TransType.State));//.ToTransStream());
            }
        }
        #endregion

        #region client IDataStream
        /*
        public async Task<NetStream> SendAsync(IDataStream message)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    return await ReceiveDataStreamAsync(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new NetStream();// new TransBinary(ex.Message, "Fault", TransType.State);
            }
        }

        public async Task<NetStream> SendChunksAsync(IDataStream message)
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                Console.WriteLine("Connected to server!");

                byte[] largeBinaryData = new byte[100000]; // Simulating large binary data
                new Random().NextBytes(largeBinaryData); // Fill with random bytes

                int chunkSize = 4096;
                for (int i = 0; i < largeBinaryData.Length; i += chunkSize)
                {
                    int size = Math.Min(chunkSize, largeBinaryData.Length - i);
                    clientSocket.Send(largeBinaryData, i, size, SocketFlags.None);
                }
                Console.WriteLine("Binary data sent.");
                return await ReceiveDataStreamAsync(clientSocket);
            }
        }

        private static async Task<NetStream> ReceiveDataStreamAsync(Socket clientSocket)
        {
            byte[] buffer = new byte[4096]; // Chunk size
            int bytesRead;
            using (var stream = new System.IO.MemoryStream())
            {
                //while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None); 
                //while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                //{
                //    stream.Write(buffer, 0, bytesRead);
                //    if (bytesRead < buffer.Length) break; // End of message
                //}
                //byte[] receivedData = stream.ToArray();
                //return new NetStream(receivedData);
                return new NetStream(buffer, 0, bytesRead);
            }
        }

        private static NetStream ReceiveDataStream(Socket clientSocket)
        {
            byte[] buffer = new byte[4096]; // Chunk size
            int bytesRead;
            using (var stream = new System.IO.MemoryStream())
            {
                while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    if (bytesRead < buffer.Length) break; // End of message
                }
                byte[] receivedData = stream.ToArray();
                return new NetStream(receivedData);
            }
        }
        */
        #endregion

        #region static IDataStream
        /*
        public static async Task<NetStream> SendAsync(IDataStream message, string ServerIp, int Port)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    return await ReceiveDataStreamAsync(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new NetStream();// new TransBinary(ex.Message, "Fault", TransType.State);
            }
        }

        public static NetStream Send(IDataStream message, string ServerIp, int Port)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    clientSocket.Send(data, SocketFlags.None);
                    return ReceiveDataStream(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new NetStream();// new TransBinary(ex.Message, "Fault", TransType.State);
            }
        }

        public static void Send(IDataStream message, string ServerIp, int Port, Action<NetStream> action)
        {
            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    clientSocket.Send(data, SocketFlags.None);
                    action.Invoke(ReceiveDataStream(clientSocket));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                action.Invoke(new NetStream());// new TransBinary(ex.Message, "Fault", TransType.State);
            }
        }

        public static async Task SendAsync(IDataStream message, string ServerIp, int Port, Action<NetStream> action)
        {

            try
            {
                using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                    Console.WriteLine("Connected to server!");
                    byte[] data = message.GetBytes();
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    var dataStream = await ReceiveDataStreamAsync(clientSocket);
                    action.Invoke(dataStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                action.Invoke(new NetStream());// new TransBinary(ex.Message, "Fault", TransType.State);
            }
        }
        */
        #endregion

        #region client TransBinary
        /*
                public async Task<TransBinary> SendAsync(TransBinary message)
                {
                    try
                    {
                        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                            Console.WriteLine("Connected to server!");

                            await SendDataAsync(clientSocket, message);
                            return await ReceiveDataAsync(clientSocket);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return new TransBinary(ex.Message, "Fault", TransType.State);
                    }
                }

                public async Task<TransBinary> SendChunksAsync(TransBinary message)
                {
                    using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                        Console.WriteLine("Connected to server!");

                        byte[] largeBinaryData = new byte[100000]; // Simulating large binary data
                        new Random().NextBytes(largeBinaryData); // Fill with random bytes

                        int chunkSize = 4096;
                        for (int i = 0; i < largeBinaryData.Length; i += chunkSize)
                        {
                            int size = Math.Min(chunkSize, largeBinaryData.Length - i);
                            clientSocket.Send(largeBinaryData, i, size, SocketFlags.None);
                        }

                        Console.WriteLine("Binary data sent.");
                        return await ReceiveDataAsync(clientSocket);
                    }
                }

                private static async Task SendDataAsync(Socket clientSocket, TransBinary message)
                {
                    //TransBinary tb = new TransBinary(message, "Enqueue", TransType.Text);
                    byte[] data = message.ToStream().ToArray();// Encoding.UTF8.GetBytes(message);
                    await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    Console.WriteLine($"Sent: {message}");
                }

                private static async Task<TransBinary> ReceiveDataAsync(Socket clientSocket)
                {
                    TransBinary tb = null;
                    byte[] buffer = new byte[8192];
                    int bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesRead > 0)
                    {
                        tb = new TransBinary(buffer, 0, bytesRead);
                        //var cmd = tb.Command;
                        //var type = tb.TypeName;
                        //if (tb.TransType == TransType.Text)
                        //    Console.WriteLine($"Received: {tb.ReadBody<string>()}");
                        //else
                        //    Console.WriteLine($"Received: {tb.BodyToJson()}");

                        //string response =  Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        //Console.WriteLine($"📥 Received: {response}");
                    }
                    if (tb == null)
                        return new TransBinary("NA", "Fault", TransType.State);
                    return tb;
                }

                #endregion



                #region static TransBinary

                public static async Task<TransBinary> SendAsync(TransBinary message, string ServerIp, int Port)
                {
                    try
                    {
                        //var args= serverAddress.Split(':');
                        //string ServerIp = args[0];
                        //int Port = Types.ToInt(args[1]);
                        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                            Console.WriteLine("Connected to server!");

                            await SendDataAsync(clientSocket, message);
                            return await ReceiveDataAsync(clientSocket);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return new TransBinary(ex.Message, "Fault", TransType.State);
                    }
                }

                public static TransBinary Send(TransBinary message, string ServerIp, int Port)
                {
                    try
                    {
                        //var args= serverAddress.Split(':');
                        //string ServerIp = args[0];
                        //int Port = Types.ToInt(args[1]);
                        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                            Console.WriteLine("Connected to server!");

                            SendDataAsync(clientSocket, message).ConfigureAwait(false);
                            return ReceiveDataAsync(clientSocket).GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return new TransBinary(ex.Message, "Fault", TransType.State);
                    }
                }

                public static void Send(TransBinary message, string ServerIp, int Port, Action<TransBinary> action)
                {
                    try
                    {
                        //var args= serverAddress.Split(':');
                        //string ServerIp = args[0];
                        //int Port = Types.ToInt(args[1]);
                        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                            Console.WriteLine("Connected to server!");

                            SendDataAsync(clientSocket, message).ConfigureAwait(false);
                            var response = ReceiveDataAsync(clientSocket).GetAwaiter().GetResult();
                            action(response);//.ToTransStream());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        action(new TransBinary(ex.Message, "Fault", TransType.State));//.ToTransStream());
                    }
                }

                public static async Task SendAsync(TransBinary message, string ServerIp, int Port, Action<TransBinary> action)
                {
                    try
                    {
                        //var args= serverAddress.Split(':');
                        //string ServerIp = args[0];
                        //int Port = Types.ToInt(args[1]);
                        using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            await clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ServerIp), Port));
                            Console.WriteLine("Connected to server!");

                            await SendDataAsync(clientSocket, message);
                            var response = await ReceiveDataAsync(clientSocket);
                            action(response);//.ToTransStream());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        action(new TransBinary(ex.Message, "Fault", TransType.State));//.ToTransStream());
                    }
                }
        */
        #endregion

        //public static void Start()
        //{
        //    StartClientAsync().GetAwaiter().GetResult();
        //}
    }

    /// <summary>
    /// Represent a base class for tcp client.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpClient<TRequest> : IDisposable where TRequest : ISerialEntity, ITransformMessage
    {
        #region members
        protected TCP.TcpClient tcpClient = null;
        const int MaxRetry = 3;
        int BufferSize = 4096;
        int Port;
        IPAddress ServerIp;
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
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

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
        protected TcpClient(string hostAddress, int port, int connectTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                //IsAsync = isAsync,
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
        protected TcpClient(string hostAddress, int port, int connectTimeout, int readTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.EnsureReadTimeout(readTimeout),
                //IsAsync = isAsync,
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
        protected TcpClient(string hostAddress, int port, int connectTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
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
        protected TcpClient(string hostAddress, int port, int connectTimeout, int readTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = readTimeout,
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }


        #endregion

        #region IDisposable

        protected void Init()
        {
            BufferSize = Settings.SendBufferSize;
            Port = Settings.Port;
            ServerIp = Settings.HostAddress;
        }

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
        [Obsolete("Use ExecuteAsync insted")]
        protected abstract object ExecuteMessage(NetworkStream stream, TRequest message);//, Type type);

        [Obsolete("Use ExecuteAsync insted")]
        protected abstract void ExecuteOneWay(NetworkStream stream, TRequest message);

        protected abstract TResponse ExecuteMessage<TResponse>(NetworkStream stream, TRequest message);
        [Obsolete("Use ExecuteAsync insted")]
        protected abstract void ExecuteMessage<TResponse>(NetworkStream stream, TRequest message, Action<TResponse> onCompleted);

        //protected virtual async Task ExecuteMessageAsync<TResponse>(NetworkStream stream, TRequest message, Action<TResponse> onCompleted)
        //{
        //    await Task.Run(() =>
        //    {
        //        ExecuteMessage<TResponse>(stream, message, onCompleted);
        //        //onCompleted.Invoke(response);
        //    });
        //}

        //protected virtual async Task ExecuteOneWayAsync(NetworkStream stream, TRequest message)
        //{
        //    await Task.Run(() =>
        //    {
        //        ExecuteOneWay(stream, message);
        //    });
        //}

        #endregion

        #region Client

        public TResponse SendClient<TResponse>(TRequest message) //where TResponse : ISerialEntity, ITransformMessage
        {
            try
            {
                Init();

                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ServerIp, Port);
                    OnInfo("Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        SendStream(stream, message);
                        return ExecuteMessage<TResponse>(stream, message);
                        //return await ReceiveStreamAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error: {ex.Message}");
                return (TResponse)default;//.FromResult<TResponse>(default(TResponse));// await TransStream.WriteStateAsync(-1, "Error: " + ex.Message);
            }
        }

        public async Task<TResponse> SendClientAsync<TResponse>(TRequest message) //where TResponse : ISerialEntity, ITransformMessage
        {
            try
            {
                Init();

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ServerIp, Port);
                    OnInfo("Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        await SendStreamAsync(stream, message);
                        return ExecuteMessage<TResponse>(stream,message);
                        //return await ReceiveStreamAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error: {ex.Message}");
                return await Task.Run(() => default(TResponse));//.FromResult<TResponse>(default(TResponse));// await TransStream.WriteStateAsync(-1, "Error: " + ex.Message);
            }
        }

        public TransStream SendClient(TRequest message)
        {
            try
            {
                Init();

                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ServerIp, Port);
                    OnInfo("Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        SendStream(stream, message);
                        return ReceiveStream(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error: {ex.Message}");
                return TransStream.WriteState(-1, "Error: " + ex.Message);
            }
        }

        public async Task<TransStream> SendClientAsync(TRequest message)
        {
            try
            {
                Init();

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ServerIp, Port);
                    OnInfo("Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        await SendStreamAsync(stream, message);
                        return await ReceiveStreamAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error: {ex.Message}");
                return await TransStream.WriteStateAsync(-1, "Error: " + ex.Message);
            }
        }

        private void SendStream(NetworkStream stream, TRequest message)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
                //// Simulate writing data into the stream
                //byte[] data = Encoding.UTF8.GetBytes("Hello, Server! This is a streamed message.");
                //memoryStream.Write(data, 0, data.Length);
                //memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                // Send a request from client to server
                message.EntityWrite(memoryStream, null);

                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("Sending stream...");
                while ((bytesRead = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    OnInfo($"Sent {bytesRead} bytes...");
                }
                OnInfo("Stream sent successfully!");
            }
        }

        private async Task SendStreamAsync(NetworkStream stream, TRequest message)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
                //// Simulate writing data into the stream
                //byte[] data = Encoding.UTF8.GetBytes("Hello, Server! This is a streamed message.");
                //memoryStream.Write(data, 0, data.Length);
                //memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                // Send a request from client to server
                message.EntityWrite(memoryStream, null);

                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("Sending stream...");
                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"Sent {bytesRead} bytes...");
                }
                OnInfo("Stream sent successfully!");
            }
        }

        private TransStream ReceiveStream(NetworkStream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("Receiving stream...");
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                    OnInfo($"Received {bytesRead} bytes...");
                }

                //string receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                byte[] response = memoryStream.ToArray();
                OnInfo($"Received Stream Data: {response.Length}");
                return new TransStream(response);//, 0, response.Length, TransType.Stream);
            }
        }

        private async Task<TransStream> ReceiveStreamAsync(NetworkStream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("Receiving stream...");
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"Received {bytesRead} bytes...");
                }

                //string receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                byte[] response = memoryStream.ToArray();
                OnInfo($"Received Stream Data: {response.Length}");
                return new TransStream(response);//, 0, response.Length, TransType.Stream);
            }
        }
              

        #endregion

        #region Run

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
        protected virtual void OnFault(string message, Exception ex)
        {
            Console.WriteLine(message);
            Log.Exception(message, ex, true);
        }

        //void ConnectAsync()
        //{
        //    tcpClient = SocketConnector.Connect(Settings.GetEndpoint(), Settings.ConnectTimeout);
        //    tcpClient.SendTimeout = Settings.ConnectTimeout;
        //    tcpClient.SendBufferSize = Settings.SendBufferSize;
        //    tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
        //    tcpClient.ReceiveTimeout = Settings.ReadTimeout;
        //}

        //public async Task StartClientAsync(TRequest message)
        //{
        //    int retry = 0;
        //    while (retry < MaxRetry)
        //    {
        //        try
        //        {
        //            using (TcpClient client = new TcpClient())
        //            {
        //                IPEndPoint ep = new IPEndPoint(Settings.HostAddress, Settings.Port);
        //                await client.ConnectAsync(ep.Address, Settings.Port);
        //                Console.WriteLine("Connected to server!");

        //                NetworkStream stream = client.GetStream();

        //                var response = ExecuteMessage(stream, message);


        //                //byte[] message = Encoding.UTF8.GetBytes("Hello, Server!");
        //                //await stream.WriteAsync(message, 0, message.Length);

        //                //byte[] buffer = new byte[1024];
        //                //int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        //                //Console.WriteLine($"Server response: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
        //            }
        //        }
        //        catch (TimeoutException toex)
        //        {
        //            if (retry >= MaxRetry)
        //            {
        //                Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, Settings.ConnectTimeout, toex.Message);
        //                throw toex;
        //            }
        //            retry++;
        //        }
        //        catch (Exception pex)
        //        {
        //            if (retry >= MaxRetry)
        //            {
        //                Log.Error("TcpClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
        //                throw pex;
        //            }
        //            retry++;
        //        }
        //    }
        //}

        //async Task<bool> ConnectAsync()
        //{
        //    int retry = 0;

        //    IPEndPoint ep = new IPEndPoint(Settings.HostAddress, Settings.Port);
        //    tcpClient = new TCP.TcpClient();
        //    tcpClient.SendTimeout = Settings.ConnectTimeout;
        //    tcpClient.SendBufferSize = Settings.SendBufferSize;
        //    tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
        //    tcpClient.ReceiveTimeout = Settings.ReadTimeout;

        //    while (retry <= MaxRetry)
        //    {

        //        try
        //        {
        //            Task connectTask = tcpClient.ConnectAsync(ep.Address, ep.Port);
        //            Task timeoutTask = Task.Delay(millisecondsDelay: Settings.ConnectTimeout);
        //            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
        //            {
        //                retry++;
        //                if (retry >= MaxRetry)
        //                {
        //                    throw new TimeoutException("Unable to connect to tcp address: " + Settings.HostName);
        //                }
        //                Thread.Sleep(10);
        //            }
        //            else
        //            {
        //                return true;
        //            }
        //        }
        //        catch (TimeoutException toex)
        //        {
        //            if (retry >= MaxRetry)
        //            {
        //                Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, Settings.ConnectTimeout, toex.Message);
        //                throw toex;
        //            }
        //            retry++;
        //        }
        //        catch (Exception pex)
        //        {
        //            if (retry >= MaxRetry)
        //            {
        //                Log.Error("TcpClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
        //                throw pex;
        //            }
        //            retry++;
        //        }
        //    }

        //    return tcpClient.Connected;
        //}

        //bool Connect()
        //{
        //    int retry = 0;

        //    IPEndPoint ep = new IPEndPoint(Settings.HostAddress, Settings.Port);
        //    tcpClient = new TCP.TcpClient();
        //    tcpClient.SendTimeout = Settings.ConnectTimeout;
        //    tcpClient.SendBufferSize = Settings.SendBufferSize;
        //    tcpClient.ReceiveBufferSize = Settings.ReceiveBufferSize;
        //    tcpClient.ReceiveTimeout = Settings.ReadTimeout;

        //    while (retry <= MaxRetry)
        //    {

        //        try
        //        {
        //            tcpClient.Connect(ep);
        //            if (!tcpClient.Connected)
        //            {
        //                retry++;
        //                if (retry >= MaxRetry)
        //                {
        //                    throw new Exception("Unable to connect to tcp address: " + Settings.HostName);
        //                }
        //                Thread.Sleep(10);

        //            }
        //            else
        //            {
        //                return true;
        //            }
        //        }
        //        catch (TimeoutException toex)
        //        {
        //            if (retry >= MaxRetry)
        //            {
        //                Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, Settings.ConnectTimeout, toex.Message);
        //                throw toex;
        //            }
        //            retry++;
        //        }
        //        catch (Exception pex)
        //        {
        //            if (retry >= MaxRetry)
        //            {
        //                Log.Error("TcpClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
        //                throw pex;
        //            }
        //            retry++;
        //        }
        //    }

        //    return tcpClient.Connected;
        //}

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
        public object Execute(TRequest message, bool enableException = false)//Type type,
        {

            object response = null;

            try
            {
                return SendClient(message);//.ConfigureAwait(false);
                //return SendClientAsync(message).ConfigureAwait(false);

                //Connect();

                //return ExecuteMessage(tcpClient.GetStream(), message);

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
                SendClient(message);//.ConfigureAwait(false);
                //SendClientAsync(message).ConfigureAwait(false);

                //Connect();

                //ExecuteOneWay(tcpClient.GetStream(), message);

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
        //[Obsolete("Use ExecuteAsync insted")]
        public TResponse Execute<TResponse>(TRequest message, bool enableException = false)
        {

            TResponse response = default(TResponse);
            try
            {

                //Connect();

                //Console.WriteLine("SendDuplexStream-LocalEndPoint: {0}", tcpClient.Client.LocalEndPoint.ToString());

                if (message.DuplexType.IsDuplex())
                    return SendClient<TResponse>(message);// SendClientAsync<TResponse>(message).Result;//.ConfigureAwait(false); //return ExecuteMessage<TResponse>(tcpClient.GetStream(), message);
                else
                {
                    SendClient<TResponse>(message);// SendClientAsync<TResponse>(message).ConfigureAwait(false);// ExecuteOneWay(tcpClient.GetStream(), message);
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
        //[Obsolete("Use ExecuteAsync insted")]
        public void Execute<TResponse>(TRequest message, Action<TResponse> onCompleted, bool enableException = false)
        {

            TResponse response = default(TResponse);
            try
            {
                //if (Settings.IsAsync)
                //    ConnectAsync().ConfigureAwait(false);
                //else
                //    Connect();

                //Console.WriteLine("SendDuplexStream-LocalEndPoint: {0}", tcpClient.Client.LocalEndPoint.ToString());

                if (message.DuplexType.IsDuplex())
                {
                    //response = ExecuteMessage<TResponse>(tcpClient.GetStream(), message);

                    response = SendClient<TResponse>(message);//.ConfigureAwait(false);

                    //var result = Task.Run(() => SendClientAsync<TResponse>(message));//.ConfigureAwait(false);
                    //response = result.Result; //NOT GOOD
                    onCompleted.Invoke(response);
                }
                else
                {
                    SendClient<TResponse>(message);// SendClientAsync<TResponse>(message).ConfigureAwait(false);
                    //ExecuteOneWay(tcpClient.GetStream(), message);
                    onCompleted.Invoke(default(TResponse));
                }
            }
            catch (ChannelException mex)
            {
                Log.Exception("The tcp client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
                onCompleted.Invoke(response);
            }
            catch (SocketException se)
            {
                Log.Exception("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                onCompleted.Invoke(response);
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The tcp client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                onCompleted.Invoke(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                onCompleted.Invoke(response);
            }
            catch (Exception ex)
            {
                Log.Exception("The tcp client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                onCompleted.Invoke(response);
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

        #region Async

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public async Task ExecuteOutAsync(TRequest message, bool enableException = false)
        {

            try
            {
                await SendClientAsync(message);

                //Settings.IsAsync = true;
                //await ExecuteOneWayAsync(tcpClient.GetStream(), message);
            }
            catch (Exception ex)
            {
                OnFault("The ExecuteOutAsync tcp client throws the error: ", ex);
                if (enableException)
                    throw ex;
            }
        }

        public async Task ExecuteAsync<TResponse>(TRequest message, Action<TResponse> onCompleted, bool enableException = false)
        {

            //await Task.Run(() =>
            //{
            //    Execute<TResponse>(message, onCompleted, enableException);
            //});

            TResponse response = default(TResponse);
            try
            {
                //await ConnectAsync();

                if (message.DuplexType.IsDuplex())
                {
                    response= await SendClientAsync<TResponse>(message);
                    //await ExecuteMessageAsync<TResponse>(tcpClient.GetStream(), message, onCompleted);
                    onCompleted.Invoke(response);
                }
                else
                {
                    await SendClientAsync(message);
                    //await ExecuteOneWayAsync(tcpClient.GetStream(), message);
                    onCompleted.Invoke(default(TResponse));
                }
            }
            catch (ChannelException mex)
            {
                OnFault("The tcp client throws the ChannelException : ", mex);
                if (enableException)
                    throw mex;
                onCompleted.Invoke(response);
            }
            catch (SocketException se)
            {
                OnFault("The tcp client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                onCompleted.Invoke(response);
            }
            catch (TimeoutException toex)
            {
                OnFault("The tcp client throws the TimeoutException : ", toex);
                if (enableException)
                    throw toex;
                onCompleted.Invoke(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The tcp client throws the SerializationException : ", sex);
                if (enableException)
                    throw sex;
                onCompleted.Invoke(response);
            }
            catch (Exception ex)
            {
                OnFault("The tcp client throws the error: ", ex);

                if (enableException)
                    throw ex;

                onCompleted.Invoke(response);
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
    public class TcpStreamClient : TcpClient<MessageStream>//, IDisposable
    {
        static readonly Dictionary<string, TcpStreamClient> ClientsCache = new Dictionary<string, TcpStreamClient>();
        static TcpStreamClient GetClient(string hostName)
        {
            TcpStreamClient client = null;
            if (ClientsCache.TryGetValue(hostName, out client))
            {
                return client;
            }
            client = new TcpStreamClient(hostName);
            if (client == null)
            {
                throw new Exception("Invalid configuration for tcp client with host name:" + hostName);
            }
            ClientsCache[hostName] = client;
            return client;
        }

        #region static send methods


        public static bool Ping(string HostAddress, int Port, int ConnectTimeout = 3000)
        {
           
            TCP.TcpClient tcpClient = null;
            string rawAddress = HostAddress;
            try
            {
                rawAddress = string.Format("{0}:{1}", HostAddress,Port);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(HostAddress), Port);
                tcpClient = new TCP.TcpClient();
                tcpClient.SendTimeout = ConnectTimeout;
                tcpClient.SendBufferSize = TcpSettings.DefaultPingBufferSize;
                tcpClient.ReceiveBufferSize = TcpSettings.DefaultPingBufferSize;
                tcpClient.ReceiveTimeout = TcpSettings.DefaultPingReadTimeout;
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
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (TcpStreamClient client = new TcpStreamClient(hostName))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string HostAddress, int port, int connectTimeout, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout))//, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string HostAddress, int port, int connectTimeout, int readTimeout, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout, readTimeout))//, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static async Task SendDuplexStreamAsync(MessageStream request, string HostAddress, int port, int connectTimeout, int readTimeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout, readTimeout))//, true))
            {
                await client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static async Task SendDuplexStreamAsync(MessageStream request, string HostAddress, int port, int connectTimeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout))//, true))
            {
              await  client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static void SendDuplexStream(MessageStream request, string HostAddress, int port, int connectTimeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout))//, IsAsync))
            {
                client.Execute<TransStream>(request, onCompleted, enableException);
            }
        }

        public static void SendDuplexStream(MessageStream request, string HostAddress, int port, int connectTimeout, int readTimeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout, readTimeout))//, IsAsync))
            {
                client.Execute<TransStream>(request, onCompleted, enableException);
            }
        }

        public static string SendJsonDuplex(string json, string HostAddress, int port, int connectTimeout, int readTimeout, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json,null);
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout, readTimeout))//, IsAsync))
            {
                var response= client.Execute(message, enableException);
                return JsonSerializer.Serialize(response);
            }
        }

        public static object SendJsonDuplex(string json, string HostName, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json, null);
            using (TcpStreamClient client = new TcpStreamClient(HostName))
            {
                return client.Execute(message, enableException);
            }
        }

        public static object SendDuplex(MessageStream request, string HostAddress,int port, int connectTimeout, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout))//, IsAsync))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageStream request, string HostAddress, int port, int connectTimeout, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout))//, IsAsync))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostAddress, int port, int connectTimeout, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.None;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, port, connectTimeout))//, IsAsync))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static object SendDuplex(MessageStream request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostName))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageStream request, string HostName, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (TcpStreamClient client = new TcpStreamClient(HostName))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.None;
            using (TcpStreamClient client = new TcpStreamClient(HostName))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostAddress, int Port, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.None;
            using (TcpStreamClient client = new TcpStreamClient(HostAddress, Port))
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
        public TcpStreamClient(string hostAddress, int port)
            : base(hostAddress, port, TcpSettings.DefaultConnectTimeout, TcpSettings.DefaultReadTimeout)//, false)
        {

        }
        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        public TcpStreamClient(string hostAddress, int port, int connectTimeout)//, bool isAsync)
            : base(hostAddress, port, connectTimeout)//, isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        public TcpStreamClient(string hostAddress, int port,int connectTimeout,int readTimeout)//, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout)//, isAsync)
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
        public TcpStreamClient(string hostAddress, int port, int connectTimeout, int readTimeout, int inBufferSize, int outBufferSize)//, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout, inBufferSize, outBufferSize)//, isAsync)
        {

        }
       
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        public TcpStreamClient(string configHost)
            : base(configHost)
        {
           
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        public TcpStreamClient(TcpSettings settings)
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

            if (message.DuplexType.IsDuplex() == false)
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
           
            if (message.DuplexType.IsDuplex() == false)
            {
                return response;
            }

            // Receive a response from server.
            
            response = message.ReadResponse<TResponse>(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

            return response;
        }

        /// <summary>
        /// ExecuteMessage
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <param name="onCompleted"></param>
        /// <returns></returns>
        protected override void ExecuteMessage<TResponse>(NetworkStream stream, MessageStream message, Action<TResponse> onCompleted)
        {
            TResponse response = ExecuteMessage<TResponse>(stream,message);
            onCompleted.Invoke(response);
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
    /*
    /// <summary>
    /// TcpClientApp
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class TcpClientApp<TRequest> where TRequest : ISerialEntity, ITransformMessage
    {
        #region members
        //protected TCP.TcpClient tcpClient = null;
        const int MaxRetry = 3;
        int BufferSize = 4096;
        private IPAddress ServerIp;// = "127.0.0.1";
        private int Port = 5000;

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
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

        protected virtual void OnInfo(string message)
        {
            Log.Info(message);
        }

        protected virtual void OnFault(string message)
        {
            Log.Error(message);
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpClientApp()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        protected TcpClientApp(string hostAddress, int port, int connectTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                //IsAsync = isAsync,
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
        protected TcpClientApp(string hostAddress, int port, int connectTimeout, int readTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.EnsureReadTimeout(readTimeout),
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort)
            };
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpClientApp(string configHost)
        {

            Settings = TcpClientSettings.GetTcpClientSettings(configHost);//, false);
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpClientApp(TcpSettings settings)
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
        protected TcpClientApp(string hostAddress, int port, int connectTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
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
        protected TcpClientApp(string hostAddress, int port, int connectTimeout, int readTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = readTimeout,
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }


        #endregion

        #region Init //IDisposable

        protected void Init()
        {
            BufferSize = Settings.SendBufferSize;
            Port = Settings.Port;
            ServerIp = Settings.HostAddress;
        }

        //public void Dispose()
        //{
        //    if (tcpClient != null)
        //    {
        //        if (tcpClient.Connected)
        //            tcpClient.Close();
        //        tcpClient = null;
        //    }
        //}
        #endregion

        #region Read/Write

        //protected abstract object ExecuteMessage(NetworkStream stream, TRequest message);//, Type type);

        //protected abstract void ExecuteOneWay(NetworkStream stream, TRequest message);

        //protected abstract TResponse ExecuteMessage<TResponse>(NetworkStream stream, TRequest message);

        //protected abstract void ExecuteMessage<TResponse>(NetworkStream stream, TRequest message, Action<TResponse> onCompleted);

        //protected virtual async Task ExecuteMessageAsync<TResponse>(NetworkStream stream, TRequest message, Action<TResponse> onCompleted)
        //{
        //    await Task.Run(() =>
        //    {
        //        ExecuteMessage<TResponse>(stream, message, onCompleted);
        //        //onCompleted.Invoke(response);
        //    });
        //}

        //protected virtual async Task ExecuteOneWayAsync(NetworkStream stream, TRequest message)
        //{
        //    await Task.Run(() =>
        //    {
        //        ExecuteOneWay(stream, message);
        //    });
        //}

        //public async TransStream Send(TRequest message)
        //{

        //}

        #endregion

        #region Client

        public async Task<TransStream> SendClientAsync(TRequest message)
        {
            try
            {
                Init();

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ServerIp, Port);
                    OnInfo("Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        await SendStreamAsync(stream, message);
                        return await ReceiveStreamAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"Error: {ex.Message}");
                return await TransStream.WriteStateAsync(-1, "Error: " + ex.Message);
            }
        }

        private async Task SendStreamAsync(NetworkStream stream, TRequest message)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
                //// Simulate writing data into the stream
                //byte[] data = Encoding.UTF8.GetBytes("Hello, Server! This is a streamed message.");
                //memoryStream.Write(data, 0, data.Length);
                //memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                // Send a request from client to server
                message.EntityWrite(memoryStream, null);

                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("Sending stream...");
                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"Sent {bytesRead} bytes...");
                }
                OnInfo("Stream sent successfully!");
            }
        }

        private async Task<TransStream> ReceiveStreamAsync(NetworkStream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("Receiving stream...");
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"Received {bytesRead} bytes...");
                }

                //string receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                byte[] response = memoryStream.ToArray();
                OnInfo($"Received Stream Data: {response.Length}");
                return new TransStream(response);//, 0, response.Length, TransType.Stream);
            }
        }

        #endregion

        #region static

        public static async Task<TransStream> SendAsync(MessageStream message, TcpSettings settings)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(settings.HostAddress, settings.Port);
                    Console.WriteLine("Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        await _SendStreamAsync(stream, message, settings.SendBufferSize);
                        return await _ReceiveStreamAsync(stream, settings.SendBufferSize).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return await TransStream.WriteStateAsync(-1, "Error: " + ex.Message);
            }
        }

        private static async Task _SendStreamAsync(NetworkStream stream, MessageStream message, int BufferSize = 4096)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
                //// Simulate writing data into the stream
                //byte[] data = Encoding.UTF8.GetBytes(message);
                //memoryStream.Write(data, 0, data.Length);
                //memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                // Send a request from client to server
                message.EntityWrite(memoryStream, null);

                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                Console.WriteLine("Sending stream...");
                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                    Console.WriteLine($"Sent {bytesRead} bytes...");
                }
                Console.WriteLine("Stream sent successfully!");
            }
        }

        private static async Task<TransStream> _ReceiveStreamAsync(NetworkStream stream, int BufferSize = 4096)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                Console.WriteLine("Receiving stream...");
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    Console.WriteLine($"Received {bytesRead} bytes...");
                }

                byte[] response = memoryStream.ToArray();
                Console.WriteLine($"Received Stream Data: {response.Length}");
                //return new TransStream(response, 0, response.Length, TransType.Stream);
                return await Task.FromResult<TransStream>(new TransStream(response));//, 0, response.Length, TransType.Stream));
            }
        }
        #endregion
    }

    /// <summary>
    /// TcpClientApp
    /// </summary>
    public class TcpClientApp 
    {
        #region members
        //protected TCP.TcpClient tcpClient = null;
        const int MaxRetry = 3;
        int BufferSize = 4096;
        private IPAddress ServerIp;// = "127.0.0.1";
        private int Port = 5000;

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
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

        protected virtual void OnInfo(string message)
        {
            Log.Info(message);
        }

        protected virtual void OnFault(string message)
        {
            Log.Error(message);
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor default
        /// </summary>
        protected TcpClientApp()
        {
            Settings = new TcpSettings();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        protected TcpClientApp(string hostAddress, int port, int connectTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.DefaultReadTimeout,
                //IsAsync = isAsync,
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
        protected TcpClientApp(string hostAddress, int port, int connectTimeout, int readTimeout)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = TcpSettings.EnsureReadTimeout(readTimeout),
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort)
            };
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpClientApp(string configHost)
        {

            Settings = TcpClientSettings.GetTcpClientSettings(configHost);//, false);
        }
        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected TcpClientApp(TcpSettings settings)
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
        protected TcpClientApp(string hostAddress, int port, int connectTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
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
        protected TcpClientApp(string hostAddress, int port, int connectTimeout, int readTimeout, int receiveBufferSize, int sendBufferSize)//, bool isAsync)
        {
            Settings = new TcpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                //IsAsync = isAsync,
                Port = Types.NZero(port, TcpSettings.DefaultPort),
                ConnectTimeout = Math.Max(TcpSettings.DefaultConnectTimeout, connectTimeout),
                ReadTimeout = readTimeout,
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }


        #endregion

        #region Init //IDisposable

        protected void Init()
        {
            BufferSize = Settings.SendBufferSize;
            Port = Settings.Port;
            ServerIp = Settings.HostAddress;
        }

        #endregion

        #region Client

        public async Task<string> SendClientAsync(string message)
        {
            try
            {
                Init();

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ServerIp, Port);
                    OnInfo("🔗 Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        await SendStreamAsync(stream, message);
                        return await ReceiveStreamAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                OnFault($"⚠️ Error: {ex.Message}");
                return "Error: " + ex.Message;
            }
        }

        private async Task SendStreamAsync(NetworkStream stream, string message)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
                //// Simulate writing data into the stream
                byte[] data = Encoding.UTF8.GetBytes(message);
                memoryStream.Write(data, 0, data.Length);
                memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("📤 Sending stream...");
                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"📤 Sent {bytesRead} bytes...");
                }
                OnInfo("✅ Stream sent successfully!");
            }
        }

        private async Task<string> ReceiveStreamAsync(NetworkStream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                OnInfo("📥 Receiving stream...");
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    OnInfo($"📥 Received {bytesRead} bytes...");
                }

                string receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                //byte[] response = memoryStream.ToArray();
                OnInfo($"✅ Received Stream Data: {receivedMessage}");
                return await Task.FromResult<string>(receivedMessage);
                //return receivedMessage;
            }
        }

        #endregion

        #region static

        public static async Task<string> SendAsync(string message, TcpSettings settings)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(settings.HostAddress, settings.Port);
                    Console.WriteLine("🔗 Connected to server!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        await _SendStreamAsync(stream, message, settings.SendBufferSize);
                        return await _ReceiveStreamAsync(stream, settings.SendBufferSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error: {ex.Message}");
                return "Error: " + ex.Message;
            }
        }

        private static async Task _SendStreamAsync(NetworkStream stream, string message, int BufferSize = 4096)
        {

            using (MemoryStream memoryStream = new MemoryStream())
            {
                //// Simulate writing data into the stream
                byte[] data = Encoding.UTF8.GetBytes(message);
                memoryStream.Write(data, 0, data.Length);
                memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position

                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                Console.WriteLine("📤 Sending stream...");
                while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead);
                    Console.WriteLine($"📤 Sent {bytesRead} bytes...");
                }
                Console.WriteLine("✅ Stream sent successfully!");
            }
        }

        private static async Task<string> _ReceiveStreamAsync(NetworkStream stream,int BufferSize=4096)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[BufferSize]; // Optimized buffer size
                int bytesRead;

                Console.WriteLine("📥 Receiving stream...");
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    Console.WriteLine($"📥 Received {bytesRead} bytes...");
                }

                string receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                //byte[] response = memoryStream.ToArray();
                Console.WriteLine($"✅ Received Stream Data: {receivedMessage}");
                return await Task.FromResult<string>(receivedMessage);
                //return receivedMessage;
            }
        }
        #endregion
    }
    */
#if (false)
    /// <summary>
    /// Represent tcp client.
    /// </summary>
    public class TcpMessageClient<T> : TcpClient<T>, IDisposable where T : ITcpMessage<T>
    {
        //static readonly Dictionary<string, T> ClientsCache = new Dictionary<string, T>();
        //static T GetClient(string hostName)
        //{
        //    T client = default(T);
        //    if (ClientsCache.TryGetValue(hostName, out client))
        //    {
        //        return client;
        //    }
        //    client = ActivatorUtil.CreateInstance<T>();// new TcpStreamClient(hostName);
        //    if (client == null)
        //    {
        //        throw new Exception("Invalid configuration for tcp client with host name:" + hostName);
        //    }
        //    client.
        //    ClientsCache[hostName] = client;
        //    return client;
        //}

    #region static send methods


        public static bool Ping(string HostAddress, int Port, int ConnectTimeout = 3000)
        {

            TCP.TcpClient tcpClient = null;
            string rawAddress = HostAddress;
            try
            {
                rawAddress = string.Format("{0}:{1}", HostAddress, Port);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(HostAddress), Port);
                tcpClient = new TCP.TcpClient();
                tcpClient.SendTimeout = ConnectTimeout;
                tcpClient.SendBufferSize = TcpSettings.DefaultPingBufferSize;
                tcpClient.ReceiveBufferSize = TcpSettings.DefaultPingBufferSize;
                tcpClient.ReceiveTimeout = TcpSettings.DefaultPingReadTimeout;
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
        public static TransStream SendDuplexStream(T request, string hostName, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(hostName))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(T request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(T request, string HostAddress, int port, int connectTimeout, int readTimeout, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static async Task SendDuplexStreamAsync(T request, string HostAddress, int port, int connectTimeout, int readTimeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, readTimeout, true))
            {
                await client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static async Task SendDuplexStreamAsync(T request, string HostAddress, int port, int connectTimeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, true))
            {
                await client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static void SendDuplexStream(T request, string HostAddress, int port, int connectTimeout, Action<TransStream> onCompleted, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, IsAsync))
            {
                client.Execute<TransStream>(request, onCompleted, enableException);
            }
        }

        public static void SendDuplexStream(T request, string HostAddress, int port, int connectTimeout, int readTimeout, Action<TransStream> onCompleted, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {
                client.Execute<TransStream>(request, onCompleted, enableException);
            }
        }
        /*
        public static string SendJsonDuplex(string json, string HostAddress, int port, int connectTimeout, int readTimeout, bool IsAsync, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json, null);
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {
                var response = client.Execute(message, enableException);
                return JsonSerializer.Serialize(response);
            }
        }

        public static object SendJsonDuplex(string json, string HostName, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json, null);
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostName))
            {
                return client.Execute(message, enableException);
            }
        }
        */
        public static object SendDuplex(T request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, IsAsync))
            {
                return client.Execute(request, enableException);
            }
        }

        public static TR SendDuplex<TR>(T request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, IsAsync))
            {
                return client.Execute<TR>(request, enableException);
            }
        }

        public static void SendOut(T request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.None;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, port, connectTimeout, IsAsync))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static object SendDuplex(T request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostName))
            {
                return client.Execute(request, enableException);
            }
        }

        public static TR SendDuplex<TR>(T request, string HostName, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostName))
            {
                return client.Execute<TR>(request, enableException);
            }
        }

        public static void SendOut(T request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.None;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostName))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static void SendOut(T request, string HostAddress, int Port, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.DuplexType = DuplexTypes.None;
            using (TcpMessageClient<T> client = new TcpMessageClient<T>(HostAddress, Port))
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
        public TcpMessageClient(string hostAddress, int port)
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
        public TcpMessageClient(string hostAddress, int port, int connectTimeout, bool isAsync)
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
        public TcpMessageClient(string hostAddress, int port, int connectTimeout, int readTimeout, bool isAsync)
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
        public TcpMessageClient(string hostAddress, int port, int connectTimeout, int readTimeout, int inBufferSize, int outBufferSize, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout, inBufferSize, outBufferSize, isAsync)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        public TcpMessageClient(string configHost)
            : base(configHost)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        public TcpMessageClient(TcpSettings settings)
            : base(settings)
        {

        }

    #endregion

    #region override

        protected override void ExecuteOneWay(NetworkStream stream, T message)
        {
            // Send a request from client to server
            message.EntityWrite(stream, null);
        }

        protected override object ExecuteMessage(NetworkStream stream, T message)//, Type type)
        {
            object response = null;

            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.DuplexType.IsDuplex() == false)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadResponse(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize, false);

            return response;
        }

        protected override TResponse ExecuteMessage<TResponse>(NetworkStream stream, T message)
        {
            TResponse response = default(TResponse);

            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.DuplexType.IsDuplex() == false)
            {
                return response;
            }

            // Receive a response from server.

            response = message.ReadResponse<TResponse>(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

            return response;
        }

        /// <summary>
        /// ExecuteMessage
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <param name="onCompleted"></param>
        /// <returns></returns>
        protected override void ExecuteMessage<TResponse>(NetworkStream stream, T message, Action<TResponse> onCompleted)
        {
            TResponse response = ExecuteMessage<TResponse>(stream, message);
            onCompleted.Invoke(response);
        }

        /// <summary>
        /// connect to the tcp channel and execute request.
        /// </summary>
        public new MessageAck Execute(T message, bool enableException = false)
        {
            return Execute<MessageAck>(message, enableException);
        }

    #endregion

    }
#endif
}