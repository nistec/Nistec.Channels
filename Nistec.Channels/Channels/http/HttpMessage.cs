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

namespace Nistec.Channels.Http
{

    public class HttpRequestInfo
    {
        public string Body { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public Uri Url { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("HttpMethod {0}", HttpMethod));
            sb.AppendLine(string.Format("Url {0}", Url));
            sb.AppendLine(string.Format("ContentType {0}", ContentType));
            sb.AppendLine(string.Format("ContentLength {0}", ContentLength));
            sb.AppendLine(string.Format("Body {0}", Body));
            return sb.ToString();
        }

        public static HttpRequestInfo Read(HttpListenerRequest request)
        {
            var info = new HttpRequestInfo();
            info.HttpMethod = request.HttpMethod;
            info.Url = request.Url;

            if (request.HasEntityBody)
            {
                Encoding encoding = request.ContentEncoding;
                using (var bodyStream = request.InputStream)
                using (var streamReader = new StreamReader(bodyStream, encoding))
                {
                    if (request.ContentType != null)
                        info.ContentType = request.ContentType;

                    info.ContentLength = request.ContentLength64;
                    info.Body = streamReader.ReadToEnd();
                }
            }

            return info;
        }
    }

    public class HttpResponseInfo
    {
        public string Body { get; set; }
        public string ContentEncoding { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("StatusCode {0} StatusDescripton {1}", StatusCode, StatusDescription));
            sb.AppendLine(string.Format("ContentType {0} ContentEncoding {1} ContentLength {2}", ContentType, ContentEncoding, ContentLength));
            sb.AppendLine(string.Format("Body {0}", Body));
            return sb.ToString();
        }
        public static HttpResponseInfo Read(HttpWebResponse response)
        {
            var info = new HttpResponseInfo();
            info.StatusCode = response.StatusCode;
            info.StatusDescription = response.StatusDescription;
            info.ContentEncoding = response.ContentEncoding;
            info.ContentLength = response.ContentLength;
            info.ContentType = response.ContentType;

            using (var bodyStream = response.GetResponseStream())
            using (var streamReader = new StreamReader(bodyStream, Encoding.UTF8))
            {
                info.Body = streamReader.ReadToEnd();
            }

            return info;
        }

        private static void CreateResponse(HttpListenerResponse response, string body)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            byte[] buffer = Encoding.UTF8.GetBytes(body);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    /// <summary>
    /// Represent a message for named tcp communication.
    /// </summary>
    [Serializable]
    public class HttpMessage : MessageStream,IMessage, IDisposable
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
            Key = key;
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
            Key = key;
            Expiration = expiration;
            Id = sessionId;
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

        #endregion

        #region send methods
        /// <summary>
        /// Send duplex message to tcp server using the host name and port arguments.
        /// </summary>
        /// <param name="HostAddress"></param>
        /// <param name="method"></param>
        /// <param name="ReadTimeOut"></param>
        /// <returns></returns>
        public object SendDuplex(string HostAddress,string method, int ReadTimeOut)
        {
            return HttpClient.SendDuplex(this, HostAddress, method,ReadTimeOut);
        }
        /// <summary>
        /// Send duplex message to tcp server using the host name and port arguments.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HostAddress"></param>
        /// <param name="method"></param>
        /// <param name="ReadTimeOut"></param>
        /// <returns></returns>
        public T SendDuplex<T>(string HostAddress, string method, int ReadTimeOut)
        {
            return HttpClient.SendDuplex<T>(this, HostAddress, method, ReadTimeOut);
        }
        /// <summary>
        /// Send one way message to tcp server using the host name and port arguments.
        /// </summary>
        /// <param name="HostAddress"></param>
        /// <param name="method"></param>
        /// <param name="ReadTimeOut"></param>
        public void SendOut(string HostAddress, string method, int ReadTimeOut)
        {
            HttpClient.SendOut(this, HostAddress, method, ReadTimeOut);
        }

        #endregion

        #region Read/Write
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


        internal static HttpMessage ServerReadRequest(HttpRequestInfo request)
        {
            var message = new HttpMessage();
            message.EntityRead(request.Body, null);
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
                Key = dict.Get<string>("Key"),
                Args = dict.Get<GenericNameValue>("Args"),
                BodyStream = dict.Get<NetStream>("Body", null),//, ConvertDescriptor.Implicit),
                Expiration = dict.Get<int>("Expiration", 0),
                IsDuplex = dict.Get<bool>("IsDuplex", true),
                Modified = dict.Get<DateTime>("Modified", DateTime.Now),
                TypeName = dict.Get<string>("TypeName"),
                Id = dict.Get<string>("Id")
            };

            return message;
        }
        #endregion
        public static string SendRequest(string address, string method, string jsonRequest)
        {

            string response = null;


            using (var ClientContext = new WebClient())
            {
                ClientContext.Headers["Content-type"] = "application/json";
                ClientContext.Encoding = Encoding.UTF8;
                response = ClientContext.UploadString(address, method, jsonRequest);

                Console.WriteLine("Send messsage result:" + response);

                return response;
            }
        }
    }
}
