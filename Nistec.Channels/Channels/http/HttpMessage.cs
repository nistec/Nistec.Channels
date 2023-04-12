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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nistec.Generic;
using Nistec.Runtime;
using Nistec.Serialization;
using System.Collections;
using Nistec.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Specialized;

namespace Nistec.Channels.Http
{
    /// <summary>
    /// Represent a message for named tcp communication.
    /// </summary>
    [Serializable]
    public class HttpMessage : MessageStream, ITransformMessage, IDisposable
    {

        #region ctor

        /// <summary>
        /// Initialize a new instance of tcp message.
        /// </summary>
        public HttpMessage() : base() 
        { 
            Formatter = MessageStream.DefaultFormatter;
            Modified = DateTime.Now;
        }
        /// <summary>
        /// Initialize a new instance of tcp message.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        public HttpMessage(string command, string key, object value, int expiration)
            : this()
        {
            Command = command;
            //Identifier = key;
            CustomId = key;
            Expiration = expiration;
            SetBody(value);
        }
        /// <summary>
        /// Initialize a new instance of tcp message.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        public HttpMessage(string command, string key, object value, int expiration, string sessionId)
            : this()
        {
            Command = command;
            //Identifier = key;
            CustomId = key;
            Expiration = expiration;
            SessionId = sessionId;
            SetBody(value);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Destructor.
        /// </summary>
        ~HttpMessage()
        {
            Dispose(false);
        }
        #endregion

        #region static
        /*
        /// <summary>
        /// Create a new message stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public static MessageStream Create(Stream stream, IBinaryStreamer streamer)
        {
            HttpMessage message = new HttpMessage();
            message.EntityRead(stream, streamer);
            return message;
        }
        */
        #endregion

        #region send methods
        /// <summary>
        /// Send duplex message to tcp server using the host name and port arguments.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public object SendDuplex(string address, int port, string method, int timeout)
        {
            return HttpClient.SendDuplex(this, address, port, method, timeout);
        }
        /// <summary>
        /// Send duplex message to tcp server using the host name and port arguments.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public T SendDuplex<T>(string address, int port, string method, int timeout)
        {
            return HttpClient.SendDuplex<T>(this, address, port, method, timeout);
        }
        /// <summary>
        /// Send one way message to tcp server using the host name and port arguments.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        /// <param name="timeout"></param>
        public void SendOut(string address, int port, string method, int timeout)
        {
            HttpClient.SendOut(this, address, port, method, timeout);
        }

        #endregion

        #region Read/Write http

        internal static HttpMessage ReadRequest(HttpRequestInfo request)
        {
            if (request.BodyStream != null)
            {
                return ParseStream(request.BodyStream);
            }
            else
            {

                var message = new HttpMessage();
                if (request.BodyType == HttpBodyType.QueryString)
                    message.EntityRead(request.QueryString, null);
                else if (request.Body != null)
                    message.EntityRead(request.Body, null);
                else if (request.Url.LocalPath != null && request.Url.LocalPath.Length > 1)
                    message.EntityRead(request.Url.LocalPath.TrimStart('/').TrimEnd('/'), null);

                return message;
            }
        }

        internal static void WriteResponse(HttpListenerContext context, NetStream bResponse)
        {
            var response = context.Response;
            if (bResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.StatusDescription = "No response";
                return;
            }

            int cbResponse = bResponse.iLength;
            byte[] buffer = bResponse.ToArray();



            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();

        }


        #endregion

        #region Read/Write

        //internal static HttpMessage ServerReadRequest(HttpRequestInfo request)
        //{
        //    var message = new HttpMessage();

        //    if (request.BodyType == HttpBodyType.QueryString)
        //        message.EntityRead(request.QueryString, null);
        //    else if(request.BodyType== HttpBodyType.Body)
        //        message.EntityRead(request.Body, null);
        //    else

        //    return message;
        //}

        /*
        /// <summary>
        /// Get <see cref="HttpMessage"/> as <see cref="NetStream"/> Stream.
        /// </summary>
        /// <returns></returns>
        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }
        /// <summary>
        /// Convert stream to <see cref="HttpMessage"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static HttpMessage ParseStream(Stream stream)
        {
            var message = new HttpMessage();
            message.EntityRead(stream, null);
            return message;
        }
       
        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary dict)
        {
            HttpMessage message = new HttpMessage()
            {
                Command = dict.Get<string>("Command"),
                Id = dict.Get<string>("Id"),
                Args = dict.Get<GenericNameValue>("Args"),
                BodyStream = dict.Get<NetStream>("Body", null),//, ConvertDescriptor.Implicit),
                Expiration = dict.Get<int>("Expiration", 0),
                IsDuplex = dict.Get<bool>("IsDuplex", true),
                Modified = dict.Get<DateTime>("Modified", DateTime.Now),
                TypeName = dict.Get<string>("TypeName"),
                Id = dict.Get<string>("Id"),
                ReturnTypeName = dict.Get<string>("ReturnTypeName")
            };

            return message;
        }
        */
        #endregion

        //public static string SendRequest(string address, int port, string method, string jsonRequest)
        //{

        //    string response = null;


        //    using (var ClientContext = new WebClient())
        //    {
        //        ClientContext.Headers["Content-type"] = "application/json";
        //        ClientContext.Encoding = Encoding.UTF8;
        //        response = ClientContext.UploadString(GetHostAddress(address,port), method, jsonRequest);

        //        Console.WriteLine("Send messsage result:" + response);

        //        return response;
        //    }
        //}

        /// <summary>
        /// Get host adress.
        /// </summary>
        public static string GetHostAddress(string address, int port)
        {
            if (port > 0)
                return address + ":" + port.ToString();
            return address;
        }

        internal static HttpMessage ParseStream(Stream stream)
        {
            var message = new HttpMessage() ;
            message.EntityRead(stream, null);
            return message;
        }

    }
}
