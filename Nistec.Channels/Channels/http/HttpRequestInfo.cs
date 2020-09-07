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

    public enum HttpBodyType { Body, QueryString, UrlArgs }

    public class HttpRequestInfo
    {
        public RequestContentType RequestContentType { get; set; }
        public NetStream BodyStream { get; set; }
        public string Body { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public Uri Url { get; set; }
        public NameValueCollection QueryString { get; set; }
        public HttpBodyType BodyType { get; private set; }
        public HttpListenerRequest Request { get; private set; }

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
            info.Request = request;
            info.HttpMethod = request.HttpMethod;
            info.Url = request.Url;
            if (request.ContentType != null)
            {
                info.ContentType = request.ContentType;
                info.RequestContentType = HttpRequest.GetContentType(info.ContentType);
            }
            Encoding encoding = request.ContentEncoding;

            if (request.HasEntityBody)
            {
                using (var bodyStream = request.InputStream)
                {
                    info.BodyType = HttpBodyType.Body;

                    switch (info.RequestContentType)
                    {
                        case RequestContentType.Data:
                            info.BodyStream = HttpRequest.ReadStream(bodyStream);
                            break;
                        case RequestContentType.Form:
                            string body = HttpRequest.ReadData(bodyStream, encoding);
                            info.Body = HttpRequest.EncodeQueryString(body);
                            break;
                        default:
                            info.Body = HttpRequest.ReadData(bodyStream, encoding);
                            break;
                    }
                }
            }
            else if (request.QueryString != null && request.QueryString.Count > 0)
            {
                info.QueryString = request.QueryString;
                info.BodyType = HttpBodyType.QueryString;
            }
            else if (request.Url.LocalPath != null && request.Url.LocalPath.Length > 1)
            {
                info.Body = request.Url.LocalPath.TrimStart('/').TrimEnd('/');
                info.BodyType = HttpBodyType.UrlArgs;
            }


            //if (request.HasEntityBody)
            //{

            //    using (var bodyStream = request.InputStream)
            //    using (var streamReader = new StreamReader(bodyStream, encoding))
            //    {
            //        if (request.ContentType != null)
            //            info.ContentType = request.ContentType;

            //        info.ContentLength = request.ContentLength64;
            //        info.Body = streamReader.ReadToEnd();
            //    }
            //    info.BodyType = HttpBodyType.Body;

            //}
            //else if(request.QueryString!=null && request.QueryString.Count>0)
            //{
            //    info.QueryString = request.QueryString;
            //    info.BodyType = HttpBodyType.QueryString;
            //}

            return info;
        }
    }

    public class HttpResponseInfo
    {
        public RequestContentType RequestContentType { get; set; }
        public NetStream BodyStream { get; set; }
        public string Body { get; set; }
        public string ContentEncoding { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public HttpWebResponse Response { get; set; }

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
            info.Response = response;
            info.StatusCode = response.StatusCode;
            info.StatusDescription = response.StatusDescription;
            info.ContentEncoding = response.ContentEncoding;
            info.ContentLength = response.ContentLength;
            info.ContentType = response.ContentType;

            Encoding encoding = Encoding.GetEncoding(response.ContentEncoding);

            if (response.ContentType != null)
            {
                info.ContentType = response.ContentType;
                info.RequestContentType = HttpRequest.GetContentType(info.ContentType);
            }


            using (var bodyStream = response.GetResponseStream())
            {
                switch (info.RequestContentType)
                {
                    case RequestContentType.Data:
                        info.BodyStream = HttpRequest.ReadStream(bodyStream); break;
                    case RequestContentType.Form:
                        string body = HttpRequest.ReadData(bodyStream, encoding);
                        info.Body = HttpRequest.DecodeQueryString(body);
                        break;
                    default:
                        info.Body = HttpRequest.ReadData(bodyStream, encoding); break;
                }
            }



            //using (var bodyStream = response.GetResponseStream())
            //using (var streamReader = new StreamReader(bodyStream, Encoding.UTF8))
            //{
            //    info.Body = streamReader.ReadToEnd();
            //}

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

}
