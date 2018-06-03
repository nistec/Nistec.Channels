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
using Nistec.Logging;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using TCP = System.Net.Sockets;

namespace Nistec.Channels.Tcp
{
    /// <summary>
    /// Represent json tcp client channel
    /// </summary>
    public class TcpJsonClient : IDisposable
    {
        #region static send methods

        public static string SendDuplex(string request, string hostAddress, int port, int timeout, bool IsAsync=false, bool enableException = true)
        {
            using (TcpJsonClient client = new TcpJsonClient(hostAddress, port, timeout))
            {
                client.IsAsync = IsAsync;
                client.IsDuplex = true;
                return client.Execute(request, enableException);
            }
        }

        public static string SendDuplex(string request, string hostName, bool enableException = true)
        {
            using (TcpJsonClient client = new TcpJsonClient(hostName))
            {
                client.IsDuplex = true;
                return client.Execute(request, enableException);
            }
        }
        public static void SendOut(string request, string hostName, bool enableException = true)
        {
            using (TcpJsonClient client = new TcpJsonClient(hostName))
            {
                client.IsDuplex = false;
                client.Execute(request, enableException);
            }
        }
        //public static T SendDuplex<T>(TcpMessage message, string hostName, int port, int readTimeout, bool IsAsync, bool enableException = true)
        //{
        //    string request = JsonSerializer.Serialize(message);
        //    using (TcpJsonClient client = new TcpJsonClient(hostName, port, readTimeout))
        //    {
        //        client.IsDuplex = true;
        //        string response = client.Execute(request, enableException);
        //        return JsonSerializer.Deserialize<T>(response);
        //    }
        //}

        public static T SendDuplex<T>(string request, string hostName, int port, int readTimeout, bool IsAsync, bool enableException = true)
        {
            using (TcpJsonClient client = new TcpJsonClient(hostName, port, readTimeout))
            {
                client.IsDuplex = true;
                string response = client.Execute(request, enableException);
                return JsonSerializer.Deserialize<T>(response);
            }
        }

        public static void SendOut(string request, string hostName, int port, int readTimeout, bool IsAsync, bool enableException = true)
        {
            using (TcpJsonClient client = new TcpJsonClient(hostName, port, readTimeout))
            {
                client.IsDuplex = false;
                client.Execute(request, enableException);
            }
        }


        #endregion

        #region Default

        /// <summary>
        /// DefaultHostName
        /// </summary>
        public const string DefaultHostName = "localhost";
        /// <summary>
        /// DefaultAddress
        /// </summary>
        public const string DefaultAddress = "127.0.0.1";
        /// <summary>
        /// DefaultPort
        /// </summary>
        public const int DefaultPort = 13000;
        /// <summary>
        /// DefaultReceiveBufferSize
        /// </summary>
        public const int DefaultReceiveBufferSize = 4096;
        /// <summary>
        /// DefaultSendBufferSize
        /// </summary>
        public const int DefaultSendBufferSize = 4096;
        /// <summary>
        /// DefaultSendTimeout
        /// </summary>
        public const int DefaultSendTimeout = 5000;
        /// <summary>
        /// DefaultProcessTimeout
        /// </summary>
        public const int DefaultProcessTimeout = 5000;
        /// <summary>
        /// DefaultReadTimeout
        /// </summary>
        public const int DefaultReadTimeout = 1000;
        /// <summary>
        /// DefaultMaxSocketError
        /// </summary>
        public const int DefaultMaxSocketError = 50;

        #endregion

        #region members
        public bool IsDuplex { get; set; }
        const int MaxRetry = 3;
        #endregion

        #region settings
        /// <summary>
        ///// Get or Set <see cref="TcpSettings"/> Settings.
        ///// </summary>
        //public TcpSettings Settings { get; set; }

        /// <summary>
        ///  Get or Set HostName.
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        ///  Get or Set Host Address.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        ///  Get or Set Port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///  Get or Set Indicates that the channel can be used for asynchronous reading and writing..
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Get or Set ConnectTimeout (Default=5000).
        /// </summary>
        public int ConnectTimeout { get; set; }
        /// <summary>
        /// Get or Set ProcessTimeout (Default=5000).
        /// </summary>
        public int ProcessTimeout { get; set; }
        /// <summary>
        /// Get or Set ReceiveBufferSize (Default=8192).
        /// </summary>
        public int ReceiveBufferSize { get; set; }
        /// <summary>
        /// Get or Set SendBufferSize (Default=8192).
        /// </summary>
        public int SendBufferSize { get; set; }
       


        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="readTimeout"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="sendBufferSize"></param>
        /// <param name="isAsync"></param>
        protected TcpJsonClient(string hostAddress, int port, int readTimeout, int receiveBufferSize = 4096, int sendBufferSize = 4096, bool isAsync = false)
        {
            HostName = hostAddress;
            Address = hostAddress;
            IsAsync = isAsync;
            Port = port <= 0 ? DefaultPort : port;
            ProcessTimeout = readTimeout <= 0 ? DefaultReadTimeout : readTimeout;
            ConnectTimeout = DefaultSendTimeout;
            ReceiveBufferSize = receiveBufferSize <= 0 ? DefaultReceiveBufferSize : receiveBufferSize;
            SendBufferSize = sendBufferSize <= 0 ? DefaultSendBufferSize : sendBufferSize;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected TcpJsonClient(string configHost)
        {
            var settings = TcpClientSettings.GetTcpClientSettings(configHost);
            HostName = settings.HostName;
            Address = settings.Address;
            IsAsync = settings.IsAsync;
            Port = settings.Port;
            ProcessTimeout = settings.ProcessTimeout;
            ConnectTimeout = settings.ConnectTimeout;
            ReceiveBufferSize = settings.ReceiveBufferSize;
            SendBufferSize = settings.SendBufferSize;
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            //if (pipeClientStream != null)
            //{
            //    pipeClientStream.Dispose();
            //    pipeClientStream = null;
            //}
        }
        #endregion

        #region override

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public string Execute(string message, bool enableException = true)
        {

            string response = null;

            try
            {

                using (var client = (IsAsync) ? ConnectAsync() : Connect())
                {
                    var stream = client.GetStream();
                    StringMessage.WriteString(message, stream);
                    if (IsDuplex)
                    {
                        // Receive a response from server.
                        response = StringMessage.ReadString(stream);

                        if (response[0] == '[' && response[response.Length - 1] != ']')
                        {
                            Console.WriteLine("Incorrect json response");
                        }
                    }
                    client.Close();
                }

                return response;

            }
            catch (TCP.SocketException se)
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
        }

 
        #endregion

        #region Connector

        TCP.TcpClient ConnectAsync()
        {
            var tcpClient = SocketConnector.Connect(GetEndpoint(), ConnectTimeout);
            tcpClient.SendTimeout = ConnectTimeout;
            tcpClient.SendBufferSize = SendBufferSize;
            tcpClient.ReceiveBufferSize = ReceiveBufferSize;
            if (tcpClient.Connected)
                return tcpClient;
            else
                return null;
        }

        TCP.TcpClient Connect()
        {

            int retry = 0;

            IPEndPoint ep = new IPEndPoint(HostAddress, Port);

            var tcpClient = new TCP.TcpClient();
            tcpClient.SendTimeout = ConnectTimeout;
            tcpClient.SendBufferSize = SendBufferSize;
            tcpClient.ReceiveBufferSize = ReceiveBufferSize;

            Exception connectEx = null;

            do
            {
                try
                {
                    tcpClient.Connect(ep);
                }
                catch (TimeoutException toex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, ConnectTimeout, toex.Message);
                        connectEx= toex;
                    }
                }
                catch (Exception pex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("TcpClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
                        connectEx= pex;
                    }
                }
                retry++;

            } while (!tcpClient.Connected && retry <= MaxRetry);


            if (!tcpClient.Connected)
            {
                if (connectEx != null)
                    throw connectEx;
                else
                    throw new Exception("Unable to connect to tcp address: " + HostName);
            }

            return tcpClient;

            //tcpClient.SendTimeout = ConnectTimeout;
            //tcpClient.SendBufferSize = SendBufferSize;
            //tcpClient.ReceiveBufferSize = ReceiveBufferSize;

            //while (retry <= MaxRetry)
            //{

            //    try
            //    {
            //        tcpClient.Connect(ep);
            //        if (!tcpClient.Connected)
            //        {
            //            retry++;
            //            if (retry >= MaxRetry)
            //            {
            //                throw new Exception("Unable to connect to tcp address: " + HostName);
            //            }
            //            Thread.Sleep(10);

            //        }
            //        else
            //        {
            //            return tcpClient;
            //        }
            //    }
            //    catch (TimeoutException toex)
            //    {
            //        if (retry >= MaxRetry)
            //        {
            //            Log.Error("TcpClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, ConnectTimeout, toex.Message);
            //            throw toex;
            //        }
            //        retry++;
            //    }
            //    catch (Exception pex)
            //    {
            //        if (retry >= MaxRetry)
            //        {
            //            Log.Error("TcpClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
            //            throw pex;
            //        }
            //        retry++;
            //    }
            //}

            //if (tcpClient!=null && tcpClient.Connected)
            //    return tcpClient;
            //else
            //    return null;
        }
        /// <summary>
        /// Represent a tcp socket connector for tcp client.
        /// </summary>
        internal static class SocketConnector
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

        #endregion

        #region host settings

        /// <summary>
        /// Ensure Host Address
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static string EnsureHostAddress(string host)
        {
            return host == null || host == "" ? "Any" : host == "localhost" ? "127.0.0.1" : host;
        }

        /// <summary>
        /// Get host adress as <see cref="IPAddress"/>.
        /// </summary>
        public IPAddress HostAddress
        {
            get
            {
                string host = EnsureHostAddress(Address);

                return host == "Any" ? IPAddress.Any : IPAddress.Parse(host);

            }
        }
        /// <summary>
        /// Get endpoint using host adress and port.
        /// </summary>
        /// <returns></returns>
        public IPEndPoint GetEndpoint()
        {
            return new IPEndPoint(HostAddress, Port);
        }

        /// <summary>
        /// Get Server Endpoint Using Machine Name
        /// </summary>
        /// <param name="host"></param>
        /// <param name="portOnHost"></param>
        /// <returns></returns>
        public static IPEndPoint GetServerEndpointUsingMachineName(string host, Int32 portOnHost)
        {

            IPEndPoint hostEndPoint = null;
            try
            {
                IPHostEntry theIpHostEntry = Dns.GetHostEntry(host);
                // Address of the host.
                IPAddress[] serverAddressList = theIpHostEntry.AddressList;

                bool gotIpv4Address = false;
                TCP.AddressFamily addressFamily;
                Int32 count = -1;
                for (int i = 0; i < serverAddressList.Length; i++)
                {
                    count++;
                    addressFamily = serverAddressList[i].AddressFamily;
                    if (addressFamily == TCP.AddressFamily.InterNetwork)
                    {
                        gotIpv4Address = true;
                        i = serverAddressList.Length;
                    }
                }

                if (gotIpv4Address == false)
                {
                    Console.WriteLine("Could not resolve name to IPv4 address. Need IP address. Failure!");
                }
                else
                {
                    Console.WriteLine("Server name resolved to IPv4 address.");
                    // Instantiates the endpoint.
                    hostEndPoint = new IPEndPoint(serverAddressList[count], portOnHost);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine("Could not resolve server address.");
                Console.WriteLine("host = " + host);
            }

            return hostEndPoint;
        }
        /// <summary>
        /// Get Server Endpoint Using Ip Address
        /// </summary>
        /// <param name="host"></param>
        /// <param name="portOnHost"></param>
        /// <returns></returns>
        public static IPEndPoint GetServerEndpointUsingIpAddress(string host, Int32 portOnHost)
        {
            IPEndPoint hostEndPoint = null;
            try
            {
                IPAddress theIpAddress = IPAddress.Parse(host);
                // Instantiates the Endpoint.
                hostEndPoint = new IPEndPoint(theIpAddress, portOnHost);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (FormatException e)
            {
                Console.WriteLine("FormatException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            return hostEndPoint;
        }
        #endregion
    }

}
