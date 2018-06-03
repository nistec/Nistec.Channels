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
using System.Web;
using Nistec.Serialization;



namespace Nistec.Channels.Http
{
    /// <summary>
    /// Represent a base class for tcp client.
    /// </summary>
    public class HttpJsonClient : IDisposable
    {
        static readonly Dictionary<string, HttpJsonClient> ClientsCache = new Dictionary<string, HttpJsonClient>();
        static HttpJsonClient GetClient(string hostName)
        {
            HttpJsonClient client = null;
            if (ClientsCache.TryGetValue(hostName, out client))
            {
                return client;
            }
            client = new HttpJsonClient(hostName);
            if (client == null)
            {
                throw new Exception("Invalid configuration for tcp client with host name:" + hostName);
            }
            ClientsCache[hostName] = client;
            return client;
        }

        #region static send methods

        public static string SendDuplex(string request, string HostAddress,int port, string method, int timeout, bool enableException = false)
        {
            using (HttpJsonClient client = new HttpJsonClient(HostAddress, port, method, timeout))
            {
                return client.Execute(request, enableException);
            }
        }

        public static void SendOut(string request, string HostAddress, int port, string method, int timeout, bool enableException = false)
        {
            using (HttpJsonClient client = new HttpJsonClient(HostAddress, port, method, timeout))
            {
                client.Execute(request, enableException);
            }
        }

        public static string SendDuplex(string request, string hostName, bool enableException = false)
        {
            using (HttpJsonClient client = new HttpJsonClient(hostName))
            {
                return client.Execute(request, enableException);
            }
        }

     
        public static void SendOut(string request, string HostName, bool enableException = false)
        {
            using (HttpJsonClient client = new HttpJsonClient(HostName))
            {
                client.Execute(request, enableException);
            }
        }

        public static void SendOut(string request, string HostAddress, int port, string method, bool enableException = false)
        {
            using (HttpJsonClient client = new HttpJsonClient(HostAddress, port, method))
            {
                client.Execute(request, enableException);
            }
        }

        #endregion

        #region members
        const int MaxRetry = 3;
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
        public const int DefaultPort = 15000;
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
       

        #endregion

        #region settings
        ///// <summary>
        ///// Get or Set <see cref="HttpSettings"/> Settings.
        ///// </summary>
        //public HttpSettings Settings { get; set; }



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
        ///  Get or Set request method.
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// Get or Set MaxServerConnections (Only for server side) (Default=1).
        /// </summary>
        public int MaxServerConnections { get; set; }
        /// <summary>
        /// Get or Set ProcessTimeout (Default=5000).
        /// </summary>
        public int ProcessTimeout { get; set; }
        ///// <summary>
        ///// Get or Set ProcessTimeout (Default=5000).
        ///// </summary>
        //public int ProcessTimeout { get; set; }
        /// <summary>
        /// Get or Set ConnectTimeout (Default=5000).
        /// </summary>
        public int ConnectTimeout { get; set; }

        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }


        /// <summary>
        ///  Get or Set Host Address.
        /// </summary>
        public string HostAddress { get; private set; }

        /// <summary>
        /// Get host adress.
        /// </summary>
        public string GetHostAddress()
        {
            if (Port > 0)
                return Address + ":" + Port.ToString();
            return Address;
        }
        #endregion

        #region ctor

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        protected HttpJsonClient(string hostAddress,int port, string method)
        {
            HostName = hostAddress;
            Address = hostAddress;
            Port = port;
            ConnectTimeout = DefaultReadTimeout;
            ProcessTimeout = DefaultSendTimeout;
            Method = method;
            HostAddress = GetHostAddress();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        protected HttpJsonClient(string hostAddress, int port, string method,int timeout)
        {
            HostName = hostAddress;
            Address = hostAddress;
            Port = port;
            ConnectTimeout = timeout <= 0? DefaultReadTimeout: timeout;
            ProcessTimeout = timeout <= 0 ? DefaultSendTimeout : timeout;
            Method = method;
            HostAddress = GetHostAddress();
        }
        /// <summary>
        /// Initialize a new instance of <see cref="HttpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected HttpJsonClient(string configHost)
        {
            var settings = HttpClientSettings.GetHttpClientSettings(configHost);
            HostName = settings.HostName;
            Address = settings.Address;
            Port = settings.Port;
            ConnectTimeout = settings.ConnectTimeout;
            Method = settings.Method;
            ProcessTimeout = settings.ProcessTimeout;
            HostAddress = GetHostAddress();
        }
        /// <summary>
        /// Initialize a new instance of <see cref="HttpClient"/> with given <see cref="HttpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected HttpJsonClient(HttpSettings settings)
        {
            HostName = settings.HostName;
            Address = settings.Address;
            Port = settings.Port;
            ConnectTimeout = settings.ConnectTimeout;
            Method = settings.Method;
            ProcessTimeout = settings.ProcessTimeout;
            HostAddress = GetHostAddress();
        }
       
        #endregion

        #region IDisposable

        public void Dispose()
        {
        }
        #endregion
      
        #region Run

       
        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public string Execute(string jsonRequest, bool enableException = false)
        {

            string response = null;

            try
            {

                response = HttpRequest.DoHttpRequest(HostAddress, jsonRequest, Method, RequestContentType.Json, ConnectTimeout);
                return response;


                //using (var ClientContext = new WebClient())
                //{
                //    ClientContext.Headers["Content-type"] = "application/json";
                //    ClientContext.Encoding = Encoding.UTF8;
                //    response = ClientContext.UploadString(HostAddress, Method, jsonRequest);

                //    Console.WriteLine("Send messsage result:" + response);

                //    return response;
                //}

            }
            catch (HttpException se)
            {
                Log.Exception("The http client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                return response;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The http client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The http client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
        }


        #endregion

       
    }
   
}