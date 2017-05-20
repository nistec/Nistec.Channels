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
    /// <typeparam name="TRequest"></typeparam>
    public abstract class HttpClient<TRequest> : IDisposable
    {

        
        #region members
        const int MaxRetry = 3;
        #endregion

        #region settings
        /// <summary>
        /// Get or Set <see cref="HttpSettings"/> Settings.
        /// </summary>
        public HttpSettings Settings { get; set; }
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
        protected HttpClient()
        {
            Settings = new HttpSettings();
        }
        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        protected HttpClient(string hostAddress, string method)
        {
            Settings = new HttpSettings()
            {
                HostName = hostAddress,
                Address = hostAddress,
                ReadTimeout =HttpSettings.DefaultReadTimeout,
                //IsDuplex = true,
                Method =method?? HttpSettings.DefaultMethod
            };
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="method"></param>
        /// <param name="readTimeout"></param>
        protected HttpClient(string hostAddress, string method,int readTimeout)
        {
            Settings = new HttpSettings()
            {
                HostName = hostAddress, 
                Address=hostAddress,
                ReadTimeout=readTimeout,
                Method = method
            };
        }
        /// <summary>
        /// Initialize a new instance of <see cref="HttpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        protected HttpClient(string configHost)
        {

            Settings = HttpClientSettings.GetHttpClientSettings(configHost);//, false);
        }
        /// <summary>
        /// Initialize a new instance of <see cref="HttpClient"/> with given <see cref="HttpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        protected HttpClient(HttpSettings settings)
        {
            Settings = settings;
        }
       
        #endregion

        #region IDisposable

        public void Dispose()
        {
        }
        #endregion

        #region Read/Write

        string ExecuteRequest(string jsonRequest)
        {

            string response = null;

            using (var ClientContext = new WebClient())
            {
                ClientContext.Headers["Content-type"] = "application/json";
                ClientContext.Encoding = Encoding.UTF8;
                response = ClientContext.UploadString(Settings.Address, Settings.Method, jsonRequest);

                Console.WriteLine("Send messsage result:" + response);

                return response;
            }
        }

        #endregion

        #region Read/Write

        /// <summary>
        /// Serialize json request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected abstract string RequestToJson(TRequest message);
        /// <summary>
        /// Deserialize json response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected abstract object ReadJsonResponse(string response, Type type);
        /// <summary>
        /// Deserialize json response
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        protected abstract TResponse ReadJsonResponse<TResponse>(string response);

        #endregion

        #region Run

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
        public string Execute(string jsonRequest, bool enableException = false)
        {

            string response = null;

            try
            {
                response = ExecuteRequest(jsonRequest);
                return response;

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

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public object Execute(TRequest message, Type type, bool enableException = false)
        {

            object response = null;// default(TResponse);

          try
            {

                string jsonRequest = RequestToJson(message);

                var strResponse= ExecuteRequest(jsonRequest);

                return ReadJsonResponse(strResponse, type);

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
            catch (SerializationException sex)
            {
                Log.Exception("The http client throws the SerializationException : ", sex, true);
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
                Log.Exception("The http client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
        }

        /// <summary>
        /// connect to the http server and execute request.
        /// </summary>
        public void Execute(TRequest message, bool enableException = false)
        {

            try
            {
                string jsonRequest = RequestToJson(message);
                ExecuteRequest(jsonRequest);

            }
            catch (SocketException se)
            {
                Log.Exception("The http client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The http client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
            }
            catch (SerializationException sex)
            {
                Log.Exception("The http client throws the SerializationException : ", sex, true);
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
                Log.Exception("The http client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

            }
        }


        /// <summary>
        /// connect to the http server and execute request.
        /// </summary>
        public TResponse Execute<TResponse>(TRequest message, bool enableException = false)
        {

            TResponse response = default(TResponse);
          
            try
            {

                string jsonRequest = RequestToJson(message);
                var strResponse = ExecuteRequest(jsonRequest);
                return ReadJsonResponse<TResponse>(strResponse);

            }
            catch (SocketException se)
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
            catch (SerializationException sex)
            {
                Log.Exception("The http client throws the SerializationException : ", sex, true);
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
                Log.Exception("The http client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
        }

 
        #endregion
    }

 
    /// <summary>
    /// Represent tcp client.
    /// </summary>
    public class HttpClient : HttpClient<MessageStream>, IDisposable
    {

 

        static readonly Dictionary<string, HttpClient> ClientsCache = new Dictionary<string, HttpClient>();
        static HttpClient GetClient(string hostName)
        {
            HttpClient client = null;
            if (ClientsCache.TryGetValue(hostName, out client))
            {
                return client;
            }
            client = new HttpClient(hostName);
            if (client == null)
            {
                throw new Exception("Invalid configuration for tcp client with host name:" + hostName);
            }
            ClientsCache[hostName] = client;
            return client;
        }
       
        #region static send methods

        public static object SendDuplex(HttpMessage request, string HostAddress,string method, int readTimeout, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(HostAddress, method, readTimeout))
            {
                return client.Execute(request, type, enableException);
            }
        }

        public static T SendDuplex<T>(HttpMessage request, string HostAddress, string method, int readTimeout, bool enableException = false)
        {
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(HostAddress, method, readTimeout))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(HttpMessage request, string HostAddress, string method, int readTimeout, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (HttpClient client = new HttpClient(HostAddress, method, readTimeout))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static object SendDuplex(HttpMessage request, string HostName, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(HostName))
            {
                return client.Execute(request, type, enableException);
            }

        }

        public static T SendDuplex<T>(HttpMessage request, string HostName, bool enableException = false)
        {
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(HostName))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(HttpMessage request, string HostName, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (HttpClient client = new HttpClient(HostName))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static void SendOut(HttpMessage request, string HostAddress, string method, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (HttpClient client = new HttpClient(HostAddress, method))
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
        public HttpClient(string hostAddress, string method)
            : base(hostAddress, method)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="method"></param>
        /// <param name="readTimeout"></param>
        public HttpClient(string hostAddress, string method,int readTimeout)
            : base(hostAddress, method, readTimeout)
        {

        }
      
        /// <summary>
        /// Initialize a new instance of <see cref="HttpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        public HttpClient(string configHost)
            : base(configHost)
        {
           
        }

        /// <summary>
        /// Initialize a new instance of <see cref="HttpClient"/> with given <see cref="HttpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        public HttpClient(HttpSettings settings)
            : base(settings)
        {

        }

        #endregion

        #region override

       
        /// <summary>
        /// Serialize json request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override string RequestToJson(MessageStream message)
        {
            return message.EntityWrite(new JsonSerializer(JsonSerializerMode.Write, null));
        }
        /// <summary>
        ///  Deserialize json response
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        protected override TResponse ReadJsonResponse<TResponse>(string response)
        {
            return JsonSerializer.Deserialize<TResponse>(response);
        }
        /// <summary>
        ///  Deserialize json response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override object ReadJsonResponse(string response, Type type)
        {
            return JsonSerializer.Deserialize(response, type);
        }
        #endregion

  
    }
   
}