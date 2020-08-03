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
using System.Collections.Specialized;


namespace Nistec.Channels.Http
{
    /// <summary>
    /// Represent a base class for tcp client.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    public abstract class HttpClient<TRequest> : IDisposable where TRequest : ITransformMessage
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
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }

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
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        protected HttpClient(string address, int port, string method)
        {
            Settings = new HttpSettings()
            {
                HostName = address,
                Address = address,
                Port = port,
                ConnectTimeout = HttpSettings.DefaultConnectTimeout,
                ReadTimeout = HttpSettings.DefaultReadTimeout,
                //IsDuplex = true,
                Method = method ?? HttpSettings.DefaultMethod
            };
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="address"></param>
        /// <param name="method"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        protected HttpClient(string address, int port, string method, int timeout)
        {
            Settings = new HttpSettings()
            {
                HostName = address,
                Address = address,
                Port = port,
                ConnectTimeout = timeout <= 0 ? HttpSettings.DefaultConnectTimeout : timeout,
                ReadTimeout = timeout <= 0 ? HttpSettings.DefaultReadTimeout : timeout,
                Method = method
            };
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        protected HttpClient(string hostAddress, string method, int timeout)
        {
            string address;
            int port;
            HttpSettings.SplitHostAddress(hostAddress, out address,out port);

            Settings = new HttpSettings()
            {
                HostName = address,
                Address = address,
                Port = port,
                ConnectTimeout = timeout <= 0 ? HttpSettings.DefaultConnectTimeout : timeout,
                ReadTimeout = timeout <= 0 ? HttpSettings.DefaultReadTimeout : timeout,
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
            Log = settings.Log;
        }
       
        #endregion

        #region IDisposable

        public void Dispose()
        {
        }
        #endregion

        #region ExecuteRequest

        //byte[] ExecuteQuery(NameValueCollection data)
        //{
        //    string qs = NameValueArgs GenericArgs.ArgsToQueryString;
        //    byte[] response = HttpRequest.DoRequestBinary(Settings.HostAddress, qs, Settings.Method, RequestContentType.Form, Settings.ConnectTimeout, true);
        //    return response;

        //    //using (var ClientContext = new WebClient())
        //    //{
        //    //    ClientContext.Headers["Content-type"] = "application/x-www-form-urlencoded";
        //    //    response = ClientContext.UploadValues(Settings.HostAddress, Settings.Method, data);
        //    //    Console.WriteLine("Send messsage result:" + response);
        //    //    return response;
        //    //}
        //}


        //string ExecuteRequest(string request, string contentType)
        //{
        //    var ct = HttpRequest.GetContentType(contentType);
        //    string response = HttpRequest.DoRequestString(Settings.HostAddress, request, Settings.Method, ct, Settings.ConnectTimeout, true);
        //    return response;
        //    //using (var ClientContext = new WebClient())
        //    //{
        //    //    ClientContext.Headers["Content-type"] = contentType==null? "application/json": contentType;
        //    //    ClientContext.Encoding = Encoding.UTF8;
        //    //    response = ClientContext.UploadString(Settings.HostAddress, Settings.Method, request);

        //    //    Console.WriteLine("Send messsage result:" + response);

        //    //    return response;
        //    //}
        //}

        //string ExecuteRequest(string request, RequestContentType contentType)
        //{
        //    string response = HttpRequest.DoRequestString(Settings.HostAddress, request, Settings.Method, contentType, Settings.ConnectTimeout, true);
        //    return response;
        //    //using (var ClientContext = new WebClient())
        //    //{
        //    //    ClientContext.Headers["Content-type"] = contentType==null? "application/json": contentType;
        //    //    ClientContext.Encoding = Encoding.UTF8;
        //    //    response = ClientContext.UploadString(Settings.HostAddress, Settings.Method, request);

        //    //    Console.WriteLine("Send messsage result:" + response);

        //    //    return response;
        //    //}
        //}

        //string ExecuteJsonRequest(string jsonRequest)
        //{

        //    string response = HttpRequest.DoRequestString(Settings.HostAddress, jsonRequest, Settings.Method, RequestContentType.Json, Settings.ConnectTimeout, true);
        //    return response;


        //    //string response = null;

        //    //using (var ClientContext = new WebClient())
        //    //{
        //    //    ClientContext.Headers["Content-type"] = "application/json";
        //    //    ClientContext.Encoding = Encoding.UTF8;
        //    //    response = ClientContext.UploadString(Settings.HostAddress, Settings.Method, jsonRequest);

        //    //    Console.WriteLine("Send messsage result:" + response);

        //    //    return response;
        //    //}
        //}

       
        //TransStream ExecuteRequestStream(byte[] request)
        //{

        //    return HttpRequest.DoHttpTransStream(Settings.HostAddress, new NetStream(request), Settings.ConnectTimeout);

        //    //int length = response == null ? 0 : response.Length;

        //    //Console.WriteLine("Send messsage result data length:" + length.ToString());
        //    //if (length > 0)
        //    //    return new TransStream(new NetStream(response), TransformType.Stream, true);//, TransType.Object);//, 0, response.Length);
        //    //else
        //    //    return TransStream.Write("Response null", TransType.Error);

        //    //byte[] response = null;

        //    //using (var ClientContext = new WebClient())
        //    //{
        //    //    ClientContext.Headers["Content-type"] = "application/x-www-form-urlencoded";
        //    //    //ClientContext.Headers["Content-type"] = "application/json";
        //    //    ClientContext.Encoding = Encoding.UTF8;
        //    //    response = ClientContext.UploadData(Settings.HostAddress, Settings.Method, request);

        //    //    int length = response == null ? 0 : response.Length;

        //    //    Console.WriteLine("Send messsage result data length:" + length.ToString());
        //    //    if (length > 0)
        //    //        return TransStream.Write(response, TransType.Object);//, 0, response.Length);
        //    //    else
        //    //        return TransStream.Write("Response null", TransType.Error);
        //    //}
        //}

        #endregion

        #region Read/Write

        /// <summary>
        /// Serialize json request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected abstract NetStream RequestToStream(TRequest message);

        /// <summary>
        /// Serialize json request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected abstract byte[] RequestToBinary(TRequest message);
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
        public void ExecuteOut(TRequest message,  bool enableException = false)//Type type,
        {
            Execute(message, enableException);
        }

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public string ExecuteJson(string jsonRequest, bool enableException = false)
        {

            string response = null;

            try
            {
                response = HttpRequest.DoHttpRequest(Settings.HostAddress, jsonRequest, Settings.Method, RequestContentType.Json, Settings.ConnectTimeout);
                return response;

                //response = ExecuteJsonRequest(jsonRequest);
                //return response;

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
        public byte[] ExecuteQueryBinary(params string[] keyValueArgs)
        {

            byte[] response = null;

            try
            {
                //var args = KeyValueUtil.KeyValueToNameValue(keyValueArgs);
                //string qs = NameValueArgs GenericArgs.ArgsToQueryString;

                string qs =KeyValueUtil.KeyValueToQueryString(keyValueArgs);
                response = HttpRequest.DoHttpBinary(Settings.HostAddress, qs, Settings.Method, RequestContentType.Form, Settings.ConnectTimeout);
                return response;


                //response = ExecuteQuery(args);
                //return response;

            }
            catch (HttpException se)
            {
                Log.Exception("The http client throws SocketException: {0}", se);
                //if (enableException)
                //    throw se;
                return response;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The http client throws the TimeoutException : ", toex, true);
                //if (enableException)
                //    throw toex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The http client throws the error: ", ex, true);

                //if (enableException)
                //    throw ex;

                return response;
            }
        }

        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public NetStream ExecuteQueryStream(params string[] keyValueArgs)
        {

            NetStream response = null;

            try
            {
                //var args = KeyValueUtil.KeyValueToNameValue(keyValueArgs);
                //string qs = NameValueArgs GenericArgs.ArgsToQueryString;

                string qs = KeyValueUtil.KeyValueToQueryString(keyValueArgs);
                response = HttpRequest.DoHttpStream(Settings.HostAddress, qs, Settings.Method, RequestContentType.Form, Settings.ConnectTimeout);
                return response;


                //response = ExecuteQuery(args);
                //return response;

            }
            catch (HttpException se)
            {
                Log.Exception("The http client throws SocketException: {0}", se);
                //if (enableException)
                //    throw se;
                return response;
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The http client throws the TimeoutException : ", toex, true);
                //if (enableException)
                //    throw toex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The http client throws the error: ", ex, true);

                //if (enableException)
                //    throw ex;

                return response;
            }
        }


        /// <summary>
        /// connect to the host and execute request.
        /// </summary>
        public string Execute(string request, RequestContentType contentType, bool enableException = false)
        {

            string response = null;

            try
            {
                response = HttpRequest.DoHttpRequest(Settings.HostAddress, request, Settings.Method, contentType, Settings.ConnectTimeout);
                return response;

                //response = ExecuteRequest(request, contentType);
                //return response;

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
                if (type==typeof(TransStream) || message.TransformType == TransformType.Stream)
                {
                    var brequest = RequestToStream(message);
                    //var streamResponse = ExecuteRequestStream(brequest);
                    var streamResponse = HttpRequest.DoHttpTransStream(Settings.HostAddress, brequest, Settings.ConnectTimeout);

                    if (message.IsDuplex && brequest != null)
                        return streamResponse;// brequest==null? null: streamResponse.ReadValue();
                    else
                        return null;
                }
                else
                {
                    string jsonRequest = RequestToJson(message);
                    var strResponse = HttpRequest.DoRequestString(Settings.HostAddress, jsonRequest, Settings.Method, RequestContentType.Json, Settings.ConnectTimeout, true);
                    //var strResponse = ExecuteJsonRequest(jsonRequest);
                    if (message.IsDuplex)
                        return ReadJsonResponse(strResponse, type);
                    else
                        return null;
                }
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
        public string Execute(TRequest message, bool enableException = false)
        {

            try
            {
                string jsonRequest = RequestToJson(message);
                return HttpRequest.DoRequestString(Settings.HostAddress, jsonRequest, Settings.Method, RequestContentType.Json, Settings.ConnectTimeout, true);
                //return ExecuteJsonRequest(jsonRequest);

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
            return null;
        }

        /// <summary>
        /// connect to the http server and execute request.
        /// </summary>
        public TResponse Execute<TResponse>(TRequest message, bool enableException = false)
        {

            TResponse response = default(TResponse);
          
            try
            {

                if (TransReader.IsTransStream(typeof(TResponse)))// message.TransformType == TransformType.Stream)
                {
                    var brequest = RequestToStream(message);
                    //var streamResponse = ExecuteRequestStream(brequest);
                    var streamResponse = HttpRequest.DoHttpTransStream(Settings.HostAddress, brequest, Settings.ConnectTimeout);
                    if (message.IsDuplex && streamResponse!=null)
                    {
                        return GenericTypes.Cast<TResponse>(streamResponse);

                        //return streamResponse==null? default(TResponse) : streamResponse.ReadValue<TResponse>();
                        //return TransWriter.Write(streamResponse.GetValue<TResponse>();
                    }
                    else
                        return default(TResponse);
                }
                else
                {
                    string jsonRequest = RequestToJson(message);
                    var strResponse = HttpRequest.DoRequestString(Settings.HostAddress, jsonRequest, Settings.Method, RequestContentType.Json, Settings.ConnectTimeout, true);
                    //var strResponse = ExecuteJsonRequest(jsonRequest);
                    if (message.IsDuplex)
                        return ReadJsonResponse<TResponse>(strResponse);
                    else
                        return default(TResponse);
                }
            
                //return ReadJsonResponse<TResponse>(strResponse);
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

        /// <summary>
        /// connect to the http server and execute request.
        /// </summary>
        public void ExecuteAsync<TResponse>(TRequest message, Action<TResponse> onCompleted, bool enableException=false)
        {

            TResponse response = default(TResponse);

            try
            {

                if (TransReader.IsTransStream(typeof(TResponse)))// message.TransformType == TransformType.Stream)
                {
                    var brequest = RequestToStream(message);
                    //var streamResponse = ExecuteRequestStream(brequest);
                    HttpRequest.DoHttpTransStreamAsync(Settings.HostAddress, brequest, Settings.ConnectTimeout,(TransStream streamResponse) =>
                    {
                        if (message.IsDuplex && streamResponse != null)
                        {
                            onCompleted(GenericTypes.Cast<TResponse>(streamResponse));

                            //return streamResponse==null? default(TResponse) : streamResponse.ReadValue<TResponse>();
                            //return TransWriter.Write(streamResponse.GetValue<TResponse>();
                        }
                        else
                            onCompleted(default(TResponse));
                    });

                    
                }
                else
                {
                    string jsonRequest = RequestToJson(message);
                    HttpRequest.DoRequestStringAsync(Settings.HostAddress, jsonRequest, Settings.Method, RequestContentType.Json, Settings.ConnectTimeout, true, (string strResponse) => {
                    //var strResponse = ExecuteJsonRequest(jsonRequest);
                    if (message.IsDuplex)
                        onCompleted(ReadJsonResponse<TResponse>(strResponse));
                    else
                        onCompleted( default(TResponse));

                });
                   
                }

                //return ReadJsonResponse<TResponse>(strResponse);
            }
            catch (SocketException se)
            {
                Log.Exception("The http client throws SocketException: {0}", se);
                if (enableException)
                    throw se;
                onCompleted(response);
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The http client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                onCompleted(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The http client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                onCompleted(response);
            }
            catch (MessageException mex)
            {
                Log.Exception("The tcp client throws the MessageException : ", mex, true);
                if (enableException)
                    throw mex;
                onCompleted(response);
            }
            catch (Exception ex)
            {
                Log.Exception("The http client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                onCompleted(response);
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
            using (HttpClient client = new HttpClient(hostName))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string address, int port, string method, int timeout, bool enableException = false)
        {
            Type type = request.BodyType;
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static void SendDuplexStreamAsync(MessageStream request, string address, int port, string method, int timeout, Action<TransStream> onCompleted, bool enableException = false)
        {
            Type type = request.BodyType;
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted,enableException);
            }
        }
        public static string SendRequest(string request, string address, int port, string method, int timeout, RequestContentType contentType, bool enableException = false)
        {
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.Execute(request, contentType, enableException);
            }
        }
        public static string SendJson(string jsonRequest, string address, int port, string method, int timeout, bool enableException = false)
        {
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.ExecuteJson(jsonRequest, enableException);
            }
        }
        public static byte[] SendQueryBinary(string address, int port, string method, int timeout, params string[] keyValueArgs)
        {
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.ExecuteQueryBinary(keyValueArgs);
            }
        }
        public static NetStream SendQueryStream(string address, int port, string method, int timeout, params string[] keyValueArgs)
        {
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.ExecuteQueryStream(keyValueArgs);
            }
        }
        public static object SendDuplex(MessageStream request, string address, int port, string method, int timeout, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.Execute(request, type, enableException);
            }
        }

        public static T SendDuplex<T>(MessageStream request, string address, int port, string method, int timeout, bool enableException = false)
        {
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string address, int port, string method, int timeout, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (HttpClient client = new HttpClient(address, port, method, timeout))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static object SendDuplex(MessageStream request, string HostName, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(HostName))
            {
                return client.Execute(request, type, enableException);
            }

        }

        public static T SendDuplex<T>(MessageStream request, string HostName, bool enableException = false)
        {
            request.IsDuplex = true;
            using (HttpClient client = new HttpClient(HostName))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageStream request, string HostName, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (HttpClient client = new HttpClient(HostName))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static void SendOut(MessageStream request, string address, int port, string method, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (HttpClient client = new HttpClient(address, port, method))
            {
                client.Execute(request, type, enableException);
            }
        }

        /// <summary>
        /// Send Duplex json
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static string SendDuplexJson(MessageStream request, string hostName, bool enableException = false)
        {
            request.IsDuplex = true;
            request.TransformType = TransformType.Json;

            using (HttpClient client = new HttpClient(hostName))
            {
                return client.ExecuteJson(request.ToJson(), enableException);
            }
        }
        /// <summary>
        /// Send Duplex json
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static void SendOutJson(MessageStream request, string hostName, bool enableException = false)
        {
            request.IsDuplex = false;
            request.TransformType = TransformType.Json;

            using (HttpClient client = new HttpClient(hostName))
            {
                client.ExecuteJson(request.ToJson(), enableException);
            }
        }

        /// <summary>
        /// Send Duplex json
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static string SendDuplexJson(string request, string hostName, bool enableException = false)
        {
            using (HttpClient client = new HttpClient(hostName))
            {
                return client.ExecuteJson(request, enableException);
            }
        }
        /// <summary>
        /// Send Duplex json
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static void SendOutJson(string request, string hostName, bool enableException = false)
        {
            using (HttpClient client = new HttpClient(hostName))
            {
                client.ExecuteJson(request, enableException);
            }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        public HttpClient(string address, int port, string method)
            : base(address, port, method)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        public HttpClient(string address, int port, string method, int timeout)
            : base(address, port, method, timeout)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        public HttpClient(string hostAddress, string method, int timeout)
            : base(hostAddress, method, timeout)
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
        protected override NetStream RequestToStream(MessageStream message)
        {
            return message.ToStream();
        }

        /// <summary>
        /// Serialize json request
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override byte[] RequestToBinary(MessageStream message)
        {
            return message.ToStream().ToArray();
        }

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

#if(false)
        public static string DoSoapRequest(string url, string soapAction, string method, string contentType, string soapBody, int TimeoutSeconds)
        {
            string result = null;

            try
            {
                //Create HttpWebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = method;
                request.ContentType = contentType + "; charset=utf-8";//"text/xml; charset=utf-8";
                request.Timeout = (int)TimeSpan.FromSeconds(TimeoutSeconds).TotalMilliseconds;
                request.KeepAlive = false;
                request.UseDefaultCredentials = true;
                request.Headers["SOAPAction"] = soapAction;

                byte[] bytes = Encoding.UTF8.GetBytes(soapBody);
                request.ContentLength = bytes.Length;

                //Create request stream
                using (Stream OutputStream = request.GetRequestStream())
                {
                    if (!OutputStream.CanWrite)
                    {
                        throw new Exception("Could not wirte to RequestStream");
                    }
                    OutputStream.Write(bytes, 0, bytes.Length);
                }

                //Get response stream
                using (WebResponse resp = request.GetResponse())
                {
                    using (Stream ResponseStream = resp.GetResponseStream())
                    {
                        using (StreamReader readStream =
                                new StreamReader(ResponseStream, Encoding.UTF8))
                        {
                            result = readStream.ReadToEnd();
                        }
                    }
                }

                //result = SoapRequest(url, soapAction, soapBody);
            }
            catch (WebException wex)
            {
                result = "Error: " + wex.Message;
            }
            catch (Exception ex)
            {
                result = "Error: " + ex.Message;
            }
            return result;
        }


        public static string DoHttpRequest(string data, string url, string method, ContentTypes contentType, int timeout)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            string result = null;

            HttpWebRequest request = null;

            string content_type = HttpSettings.GetContentType(contentType);
            if (string.IsNullOrEmpty(method))
                method = "post";
            if (timeout <= 0)
                timeout = HttpSettings.DefaultConnectTimeout;

            string encoding = "utf-8";
            Encoding enc = Encoding.GetEncoding(encoding);

            switch (contentType)
            {
                case ContentTypes.QueryString:
                    data = data.TrimStart('?');
                    break;
                case ContentTypes.Form:
                    data = EncodeRequestData(data);
                    break;
                case ContentTypes.Json:
                    data = data.TrimStart('/');
                    break;
                case ContentTypes.Xml:
                    data = data.TrimStart('/');
                    break;
            }

            string postData = EncodeRequestData(data);


            if (method.ToUpper() == "GET")
            {
                string qs = string.IsNullOrEmpty(data) ? "" : "?" + data;
                request = (HttpWebRequest)WebRequest.Create(url + qs);
                request.Timeout = timeout;

            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.BeginGetRequestStream.Credentials = CredentialCache.DefaultCredentials;

                request.Timeout = timeout;
                request.ContentType = content_type;

                byte[] bytes = enc.GetBytes(data);
                request.ContentLength = bytes.Length;

                //Create request stream
                using (Stream OutputStream = request.GetRequestStream())
                {
                    if (!OutputStream.CanWrite)
                    {
                        throw new Exception("Could not wirte to RequestStream");
                    }
                    OutputStream.Write(bytes, 0, bytes.Length);
                }

            }

            //Get response stream
            using (WebResponse resp = request.GetResponse())
            {
                using (Stream ResponseStream = resp.GetResponseStream())
                {
                    using (StreamReader readStream =
                            new StreamReader(ResponseStream, Encoding.UTF8))
                    {
                        result = readStream.ReadToEnd();
                    }
                }
            }
            return result;
        }

       
        public static string EncodeRequestData(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            string[] args = data.Replace("\r\n", "").Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            int counter = 0;
            foreach (string s in args)
            {
                string[] arg = s.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                if (counter > 0)
                    sb.Append("&");
                sb.Append(arg[0].Trim() + "=" + HttpUtility.UrlEncode(arg[1].Trim()));

                counter++;
            }

            string postData = sb.ToString();

            return postData;
        }

    }
#endif

}