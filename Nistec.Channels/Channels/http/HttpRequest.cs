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
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Web.Util;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using Nistec.IO;
using System.Web;
using System.Collections.Specialized;

namespace Nistec.Channels.Http
{

    public enum RequestContentType
    {
        //QueryString,
        Form,
        Json,
        Xml,
        Data
    }

    public class HttpResult
    {
        #region properties

        public string ResponseText
        {
            get; internal set;
        }
        /// <summary>
        /// Get the WebExceptionStatus
        /// </summary>
        public WebExceptionStatus WebExceptionStatus
        {
            get; internal set;
        }
        /// <summary>
        /// Get the HttpStatusCode
        /// </summary>
        public HttpStatusCode HttpStatusCode
        {
            get; internal set;
        }
        /// <summary>
        /// Get the HttpStatus Description
        /// </summary>
        public string HttpStatusDescription
        {
            get; internal set;
        }

        /// <summary>
        /// Get the WebException
        /// </summary>
        public WebException WebException
        {
            get; internal set;
        }

        #endregion
    }

    /// <summary>
    /// HttpRequest
    /// </summary>
    public class HttpRequest
    {

        public const int DefaultTimeout = 100000;

        #region members
        /// <summary>
        /// Http WebRequest
        /// </summary>
        private HttpWebRequest request;
        /// <summary>
        /// postData
        /// </summary>
        private string m_postData;
        /// <summary>
        /// AsyncWorker
        /// </summary>
        public event EventHandler AsyncWorker;
        /// <summary>
        /// ManualReset
        /// </summary>
        public ManualResetEvent ManualReset = new ManualResetEvent(false);

        //Action<string> ResponseAction;

        private string m_url;
        const int wait = 100;

        #endregion

        #region ctor
        /// <summary>
        /// Initialized new instance of HttpRequest class
        /// </summary>
        /// <param name="url"></param>
        /// <param name="contentType"></param>
        public HttpRequest(string url, RequestContentType contentType, int timeout) : this(url, "POST", contentType, timeout)
        {
        }
        /// <summary>
        /// Initialized new instance of HttpRequest class
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="contentType"></param>
        public HttpRequest(string url, string method, RequestContentType contentType, int timeout)
        {
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = GetMethod(method);
            request.Timeout = GetTimeout(timeout);
            m_url = url;
            RequestContentType = contentType;
            ContentType = HttpRequest.GetContentType(contentType);
            CharSet = "utf-8";
        }

        /// <summary>
        /// Initialized new instance of HttpRequest class
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="charSet"></param>
        /// <param name="contentType"></param>
        public HttpRequest(string url, string method, string charSet, RequestContentType contentType, int timeout)
        {
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = GetMethod(method);
            request.Timeout = GetTimeout(timeout);
            m_url = url;
            RequestContentType = contentType;
            ContentType = HttpRequest.GetContentType(contentType, charSet);
            CharSet = charSet;
        }


        #endregion

        #region properties

        /// <summary>
        /// Get HttpWebRequest
        /// </summary>
        public HttpWebRequest HttpWebRequest
        {
            get
            {
                return request;
            }
        }

        public string ResponseText
        {
            get; private set;
        }

        public RequestContentType RequestContentType
        {
            get;
            set;
        }
        /// <summary>
        /// Get or Set CharSet
        /// </summary>
        public string CharSet
        {
            get; set;
        }
        /// <summary>
        /// Get or Set ContentType
        /// </summary>
        public string ContentType
        {
            get; set;
        }
        /// <summary>
        /// Get the WebExceptionStatus
        /// </summary>
        public WebExceptionStatus WebExceptionStatus
        {
            get; private set;
        }
        /// <summary>
        /// Get the HttpStatusCode
        /// </summary>
        public HttpStatusCode HttpStatusCode
        {
            get; private set;
        }
        /// <summary>
        /// Get the HttpStatus Description
        /// </summary>
        public string HttpStatusDescription
        {
            get; private set;
        }

        /// <summary>
        /// Get the WebException
        /// </summary>
        public WebException WebException
        {
            get; private set;
        }

        #endregion

        #region private handles

        private void DoWait(IAsyncResult asyncResult)
        {
            int sumWait = 0;
            while (!asyncResult.IsCompleted)
            {
                Thread.Sleep(wait);
                sumWait += wait;
                if (sumWait > request.Timeout)
                {
                    throw new TimeoutException();
                }
            }
        }
   
        private string HandleWebExcption(WebException webExcp, bool readResponse)
        {
            Console.WriteLine("A WebException has been caught.");
            WebException = webExcp;
            WebExceptionStatus = webExcp.Status;

            // If status is WebExceptionStatus.ProtocolError, 
            //   there has been a protocol error and a WebResponse 
            //   should exist. Display the protocol error.
            if (WebExceptionStatus == WebExceptionStatus.ProtocolError)
            {
                Console.Write("The server returned protocol error ");
                // Get HttpWebResponse so that you can check the HTTP status code.

                //HttpWebResponse httpResponse = null;
                //StreamReader resStream = null;
                try
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response)
                    {
                        HttpStatusCode = httpResponse.StatusCode;
                        HttpStatusDescription = httpResponse.StatusDescription;
                        if (readResponse)
                        {
                            using (StreamReader resStream = new StreamReader(httpResponse.GetResponseStream(), Encoding.GetEncoding(CharSet)))//.UTF8);
                            {
                                return resStream.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("HandleWebExcption error: " + ex.Message);
                }
            }
            else
            {
                HttpStatusCode = HttpStatusCode.ServiceUnavailable;
                HttpStatusDescription = webExcp.Message;
            }
            return null;
        }

        private static HttpResult HandleWebResult(WebException webExcp, bool readResponse)
        {
            Console.WriteLine("A WebException has been caught.");
            //WebException = webExcp;
            var webExceptionStatus = webExcp.Status;
            HttpStatusCode httpStatusCode =  HttpStatusCode.InternalServerError;
            string httpStatusDescription = webExcp.Message;

            // If status is WebExceptionStatus.ProtocolError, 
            //   there has been a protocol error and a WebResponse 
            //   should exist. Display the protocol error.
            if (webExceptionStatus == WebExceptionStatus.ProtocolError)
            {
                Console.Write("The server returned protocol error ");
                // Get HttpWebResponse so that you can check the HTTP status code.

                //HttpWebResponse httpResponse = null;
                //StreamReader resStream = null;
                try
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response)
                    {

                        return new HttpResult()
                        {
                            WebExceptionStatus = webExcp.Status,
                            HttpStatusDescription = httpResponse.StatusDescription,
                            HttpStatusCode = httpResponse.StatusCode
                        };

                        //httpStatusCode = httpResponse.StatusCode;
                        //httpStatusDescription = httpResponse.StatusDescription;

                        //if (readResponse)
                        //{
                        //    using (StreamReader resStream = new StreamReader(httpResponse.GetResponseStream(), Encoding.GetEncoding(CharSet)))//.UTF8);
                        //    {
                        //        //return resStream.ReadToEnd();
                        //        return new HttpResult()
                        //        {
                        //            WebExceptionStatus = webExcp.Status,
                        //            HttpStatusDescription = httpStatusDescription,
                        //            HttpStatusCode = httpStatusCode
                        //        };
                        //    }
                        //}
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("HandleWebExcption error: " + ex.Message);
                    return new HttpResult()
                    {
                        WebExceptionStatus = webExcp.Status,
                        HttpStatusDescription = ex.Message,
                        HttpStatusCode = httpStatusCode
                    };
                }
            }
            else
            {
                return new HttpResult()
                {
                    WebExceptionStatus = webExcp.Status,
                    HttpStatusDescription = httpStatusDescription,
                    HttpStatusCode = httpStatusCode
                };
            }
        }
        private byte[] GetByte(string postData)
        {
            byte[] byteArray = null;

            // Convert the string into a byte array.
            //if (m_CodePageNum > 0)
            //{
            //    byteArray = Encoding.GetEncoding(m_CodePageNum).GetBytes(postData);//.UTF8.GetBytes(postData);
            //    return byteArray;
            //}

            byteArray = Encoding.GetEncoding(CharSet).GetBytes(postData);//.UTF8.GetBytes(postData);
            return byteArray;
        }

        #endregion

        #region Async
        /// <summary>
        /// OnAsyncWorker
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAsyncWorker(EventArgs e)
        {
            if (AsyncWorker != null)
                AsyncWorker(this, e);
        }

        public string DoAsyncRequest(string postData, string charSet = "utf-8", bool enableException = false)
        {

            if (postData == null)
            {
                throw new ArgumentNullException("postData");
            }

            try
            {
                m_postData = postData;
                // Create a new HttpWebRequest object.
                //if (!string.IsNullOrEmpty(codePage))
                //{
                //    m_ContentType += "; charset = " + codePage;
                //    m_CodePage = codePage;
                //}

                // Set the ContentType property. 
                request.ContentType = ContentType;

                // Start the asynchronous operation.    
                var asyncResult = request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);

                OnAsyncWorker(EventArgs.Empty);

                // Keep the main thread from continuing while the asynchronous
                // operation completes. A real world application
                // could do something useful such as updating its user interface. 
                ManualReset.WaitOne(request.Timeout);

                DoWait(asyncResult);

                return ResponseText;
            }
            catch (TimeoutException tex)
            {
                WebExceptionStatus = WebExceptionStatus.Timeout;
                HttpStatusDescription = tex.Message;
                HttpStatusCode = HttpStatusCode.RequestTimeout;
                if (enableException)
                    throw tex;
                return null;
            }
            catch (System.Net.WebException webExcp)
            {
                ResponseText = HandleWebExcption(webExcp, true);
                if (enableException)
                    throw webExcp;
                return null;
            }
            catch (Exception ex)
            {
                WebExceptionStatus = WebExceptionStatus.UnknownError;
                HttpStatusCode = HttpStatusCode.InternalServerError;
                HttpStatusDescription = ex.Message;
                if (enableException)
                    throw ex;
                return null;
            }
        }


        /// <summary>
        /// Async Request
        /// </summary>
        /// <param name="postData"></param>
        /// <param name="charSet"></param>
        /// <returns></returns>
        public void DoAsyncRequest(string postData, Action<string> responseAction, string charSet = "utf-8", bool enableException = false)
        {

            if (postData == null)
            {
                throw new ArgumentNullException("postData");
            }

            if (responseAction == null)
            {
                throw new ArgumentNullException("responseAction");
            }

            //ResponseAction = responseAction;

            try
            {
                m_postData = postData;
                // Create a new HttpWebRequest object.
                //if (!string.IsNullOrEmpty(codePage))
                //{
                //    m_ContentType += "; charset = " + codePage;
                //    m_CodePage = codePage;
                //}

                // Set the ContentType property. 
                request.ContentType = ContentType;

                // Start the asynchronous operation.    
                var asyncResult = request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);

                OnAsyncWorker(EventArgs.Empty);

                // Keep the main thread from continuing while the asynchronous
                // operation completes. A real world application
                // could do something useful such as updating its user interface. 
                ManualReset.WaitOne(request.Timeout);
                DoWait(asyncResult);
                responseAction(ResponseText);

            }
            catch (TimeoutException tex)
            {
                WebExceptionStatus = WebExceptionStatus.Timeout;
                HttpStatusDescription = tex.Message;
                HttpStatusCode = HttpStatusCode.RequestTimeout;
                if (enableException)
                    throw tex;
            }
            catch (System.Net.WebException webExcp)
            {
                ResponseText = HandleWebExcption(webExcp, true);
                if (enableException)
                    throw webExcp;
            }
            catch (Exception ex)
            {
                WebExceptionStatus = WebExceptionStatus.UnknownError;
                HttpStatusCode = HttpStatusCode.InternalServerError;
                HttpStatusDescription = ex.Message;
                if (enableException)
                    throw ex;
            }
        }

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = GetByte(m_postData);

            // Write to the request stream.
            postStream.Write(byteArray, 0, m_postData.Length);
            //postStream.Write(byteArray, 0, m_postData.Length);
            postStream.Close();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
            try
            {
                // End the operation
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult))
                using (Stream streamResponse = response.GetResponseStream())
                using (StreamReader streamRead = new StreamReader(streamResponse))
                {
                    ResponseText = streamRead.ReadToEnd();
                    //Console.WriteLine(ResponseText);
                }
                WebExceptionStatus = WebExceptionStatus.Success;
            }
            catch (TimeoutException tex)
            {
                WebExceptionStatus = WebExceptionStatus.Timeout;
                HttpStatusDescription = tex.Message;
                HttpStatusCode = HttpStatusCode.RequestTimeout;
            }
            catch (WebException wex)
            {
                HandleWebExcption(wex, false);
                //WebException = wex;
                //WebExceptionStatus = wex.Status;
                //HttpStatusDescription = wex.Message;
                //HttpStatusCode = HttpStatusCode.ServiceUnavailable;
            }
            catch (Exception ex)
            {
                WebExceptionStatus = WebExceptionStatus.SendFailure;
                HttpStatusDescription = ex.Message;
                HttpStatusCode = HttpStatusCode.InternalServerError;
            }
            finally
            {
                ManualReset.Set();
            }

            //ResponseAction(ResponseText);
        }


        #endregion

        #region Sync

#if (false)
        /// <summary>
        /// Send HttpWebRequest
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        public string DoRequest(string postData)
        {
            return DoRequest(postData, "utf-8");
        }

        /// <summary>
        /// DoRequest
        /// </summary>
        /// <param name="postData"></param>
        /// <param name="codePage"></param>
        /// <param name="maxRetry"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public string DoRequest(string postData, string codePage, int maxRetry, int delay)
        {
            int retry = 0;
            string result = null;
            do
            {
                try
                {
                    retry++;
                    if (retry > 1)
                    {
                        request = (HttpWebRequest)WebRequest.Create(m_url);
                        HttpStatusCode = HttpStatusCode.OK;
                        HttpStatusDescription = "";
                    }
                    result = DoRequest(postData, codePage);

                    if (WebExceptionStatus == WebExceptionStatus.Success)
                        return result;

                    if (retry < maxRetry)
                    {
                        Thread.Sleep(delay);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            } while (/*exc != null &&*/ retry < maxRetry);

         
            return result;

        }

        /// <summary>
        /// Send HttpWebRequest
        /// </summary>
        /// <param name="postData"></param>
        /// <param name="codePage">codePage</param>
        /// <returns></returns>
        public string DoRequest(string postData, string codePage)
        {

            return DoRequest(m_url, postData, "POST", codePage, ContentType, IsUrlEncoded, 120000);
        }
#endif


        #endregion

        #region static request

        /// <summary>
        /// Send HttpWebRequest
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="codePage">codePage</param>
        /// <returns></returns>
        public static string DoGet(string url, string postData, string codePage)
        {
            Stream receiveStream = null;
            StreamReader readStream = null;
            try
            {

                string qs = string.IsNullOrEmpty(postData) ? "" : "?" + postData;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + qs);
                request.Method = "GET";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Get the stream associated with the response.
                receiveStream = response.GetResponseStream();

                // Pipes the stream to a higher level stream reader with the required encoding format. 
                readStream = new StreamReader(receiveStream, Encoding.GetEncoding(codePage));//.UTF8);

                string result = readStream.ReadToEnd();
                response.Close();

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (receiveStream != null)
                    receiveStream.Close();
                if (readStream != null)
                    readStream.Close();
            }
        }
        
        public static HttpResult DoHttpResult(string url, string data, string method, RequestContentType contentType, int timeout, bool enableException)
        {

            try
            {

                string result = DoHttpRequest(url, data, method, contentType, timeout);

                return new HttpResult()
                {
                    WebExceptionStatus = WebExceptionStatus.Success,
                    HttpStatusDescription = "Ok",
                    HttpStatusCode = HttpStatusCode.OK,
                    ResponseText = result
                };
            }
            catch (TimeoutException tex)
            {
                if (enableException)
                    throw tex;
                return new HttpResult()
                {
                    WebExceptionStatus = WebExceptionStatus.Timeout,
                    HttpStatusDescription = tex.Message,
                    HttpStatusCode = HttpStatusCode.RequestTimeout
                };
            }
            catch (System.Net.WebException webExcp)
            {
                if (enableException)
                    throw webExcp;
                return HandleWebResult(webExcp, false);
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return new HttpResult()
                {
                    WebExceptionStatus = WebExceptionStatus.UnknownError,
                    HttpStatusDescription = ex.Message,
                    HttpStatusCode = HttpStatusCode.InternalServerError
                };
            }
        }

        public static string DoRequestString(string url, string data, string method, RequestContentType contentType, int timeout, bool enableException)
        {

           try
            {

                string result = DoHttpRequest(url, data, method, contentType, timeout);

                return result;
            }
            catch (TimeoutException tex)
            {
                if (enableException)
                    throw tex;
                return null;
            }
            catch (System.Net.WebException webExcp)
            {
                if (enableException)
                    throw webExcp;
                return null;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return null;
            }
        }

        public static void DoRequestStringAsync(string url, string data, string method, RequestContentType contentType, int timeout, bool enableException, Action<string> onCompleted)
        {

            try
            {

                DoHttpRequestAsync(url, data, method, contentType, timeout, onCompleted);
            }
            catch (TimeoutException tex)
            {
                if (enableException)
                    throw tex;
                onCompleted(null);
            }
            catch (System.Net.WebException webExcp)
            {
                if (enableException)
                    throw webExcp;
                onCompleted(null);
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                onCompleted(null);
            }
        }

        public static string DoRequestString(string url, string data, string method, RequestContentType contentType, int timeout, Action<string> OnFault)
        {

            try
            {

                string result = DoHttpRequest(url, data, method, contentType, timeout);
                return result;
            }
            catch (TimeoutException tex)
            {
                if (OnFault != null)
                    OnFault(tex.Message);
                return null;
            }
            catch (System.Net.WebException webExcp)
            {
                if (OnFault != null)
                    OnFault(webExcp.Message);
                return null;
            }
            catch (Exception ex)
            {
                if (OnFault != null)
                    OnFault(ex.Message);
                return null;
            }
        }

        public static byte[] DoRequestBinary(string url, string data, string method, RequestContentType contentType, int timeout, bool enableException)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            try
            {

                var result = DoHttpBinary(url, data, method, contentType, timeout);

                return result;
            }
            catch (TimeoutException tex)
            {
                if (enableException)
                    throw tex;
                return null;
            }
            catch (System.Net.WebException webExcp)
            {
                if (enableException)
                    throw webExcp;
                return null;
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return null;
            }
        }

        public static string DoHttpRequest(string url, string data, string method, RequestContentType contentType, int timeout)
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

            HttpWebRequest request = CreateRequest(url, data, method, contentType, timeout);


            //string content_type = GetContentType(contentType);
            //method = GetMethod(method);
            //if (timeout <= 0)
            //    timeout = GetTimeout(timeout);

            //string encoding = "utf-8";
            //Encoding enc = Encoding.GetEncoding(encoding);

            //string postData = EncodeRequestData(contentType, data);

            //if (method.ToUpper() == "GET")
            //{
            //    string qs = string.IsNullOrEmpty(data) ? "" : "?" + data;
            //    request = (HttpWebRequest)WebRequest.Create(url + qs);
            //    request.Timeout = timeout;
            //}
            //else
            //{
            //    request = (HttpWebRequest)WebRequest.Create(url);
            //    request.Method = "POST";
            //    request.Credentials = CredentialCache.DefaultCredentials;

            //    request.Timeout = timeout;
            //    request.ContentType = content_type;

            //    byte[] bytes = enc.GetBytes(data);
            //    request.ContentLength = bytes.Length;

            //    //Create request stream
            //    using (Stream OutputStream = request.GetRequestStream())
            //    {
            //        if (!OutputStream.CanWrite)
            //        {
            //            throw new Exception("Could not wirte to RequestStream");
            //        }
            //        OutputStream.Write(bytes, 0, bytes.Length);
            //    }

            //}

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

        public static void DoHttpRequestAsync(string url, string data, string method, RequestContentType contentType, int timeout, Action<string> onCompleted)
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

            HttpWebRequest request = CreateRequest(url, data, method, contentType, timeout);


            //string content_type = GetContentType(contentType);
            //method = GetMethod(method);
            //if (timeout <= 0)
            //    timeout = GetTimeout(timeout);

            //string encoding = "utf-8";
            //Encoding enc = Encoding.GetEncoding(encoding);

            //string postData = EncodeRequestData(contentType, data);

            //if (method.ToUpper() == "GET")
            //{
            //    string qs = string.IsNullOrEmpty(data) ? "" : "?" + data;
            //    request = (HttpWebRequest)WebRequest.Create(url + qs);
            //    request.Timeout = timeout;
            //}
            //else
            //{
            //    request = (HttpWebRequest)WebRequest.Create(url);
            //    request.Method = "POST";
            //    request.Credentials = CredentialCache.DefaultCredentials;

            //    request.Timeout = timeout;
            //    request.ContentType = content_type;

            //    byte[] bytes = enc.GetBytes(data);
            //    request.ContentLength = bytes.Length;

            //    //Create request stream
            //    using (Stream OutputStream = request.GetRequestStream())
            //    {
            //        if (!OutputStream.CanWrite)
            //        {
            //            throw new Exception("Could not wirte to RequestStream");
            //        }
            //        OutputStream.Write(bytes, 0, bytes.Length);
            //    }

            //}

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
            onCompleted(result);

        }
        public static byte[] DoHttpBinary(string url, string data, string method, RequestContentType contentType, int timeout)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            byte[] result = null;
            //byte[] buffer = new byte[4096];

            //HttpWebRequest request = null;
            HttpWebRequest request = CreateRequest(url, data, method, contentType, timeout);


            //string content_type = GetContentType(contentType);
            //method = GetMethod(method);
            //if (timeout <= 0)
            //    timeout = GetTimeout(timeout);

            //string encoding = "utf-8";
            //Encoding enc = Encoding.GetEncoding(encoding);

            //string postData = EncodeRequestData(contentType, data);

            //if (method.ToUpper() == "GET")
            //{
            //    string qs = string.IsNullOrEmpty(data) ? "" : "?" + data;
            //    request = (HttpWebRequest)WebRequest.Create(url + qs);
            //    request.Timeout = timeout;

            //}
            //else
            //{
            //    request = (HttpWebRequest)WebRequest.Create(url);
            //    request.Method = "POST";
            //    request.Credentials = CredentialCache.DefaultCredentials;

            //    request.Timeout = timeout;
            //    request.ContentType = content_type;

            //    byte[] bytes = enc.GetBytes(data);
            //    request.ContentLength = bytes.Length;

            //    //Create request stream
            //    using (Stream OutputStream = request.GetRequestStream())
            //    {
            //        if (!OutputStream.CanWrite)
            //        {
            //            throw new Exception("Could not wirte to RequestStream");
            //        }
            //        OutputStream.Write(bytes, 0, bytes.Length);
            //    }

            //}

            //Get response stream
            using (WebResponse resp = request.GetResponse())
            {
                using (Stream ResponseStream = resp.GetResponseStream())
                {
                    result = ReadStreamBinary(ResponseStream);
                }
            }
            return result;

        }

        public static NetStream DoHttpStream(string url, string data, string method, RequestContentType contentType, int timeout)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            NetStream result = null;
            //byte[] buffer = new byte[4096];

            //HttpWebRequest request = null;
            HttpWebRequest request = CreateRequest(url, data, method, contentType, timeout);

            //string content_type = GetContentType(contentType);
            //method = GetMethod(method);
            //if (timeout <= 0)
            //    timeout = GetTimeout(timeout);

            //string encoding = "utf-8";
            //Encoding enc = Encoding.GetEncoding(encoding);

            //string postData = EncodeRequestData(contentType, data);

            //if (method.ToUpper() == "GET")
            //{
            //    string qs = string.IsNullOrEmpty(data) ? "" : "?" + data;
            //    request = (HttpWebRequest)WebRequest.Create(url + qs);
            //    request.Timeout = timeout;

            //}
            //else
            //{
            //    request = (HttpWebRequest)WebRequest.Create(url);
            //    request.Method = "POST";
            //    request.Credentials = CredentialCache.DefaultCredentials;

            //    request.Timeout = timeout;
            //    request.ContentType = content_type;

            //    byte[] bytes = enc.GetBytes(data);
            //    request.ContentLength = bytes.Length;

            //    //Create request stream
            //    using (Stream OutputStream = request.GetRequestStream())
            //    {
            //        if (!OutputStream.CanWrite)
            //        {
            //            throw new Exception("Could not wirte to RequestStream");
            //        }
            //        OutputStream.Write(bytes, 0, bytes.Length);
            //    }

            //}

            //Get response stream
            using (WebResponse resp = request.GetResponse())
            {
                using (Stream ResponseStream = resp.GetResponseStream())
                {
                    result = ReadStream(ResponseStream);
                }
            }
            return result;
        }

        public static byte[] DoHttpData(string url, byte[] data, int timeout)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            byte[] result = null;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Timeout = GetTimeout(timeout);
            request.ContentType = GetContentType(RequestContentType.Data);
            request.ContentLength = data.Length;

            //Create request stream
            using (Stream OutputStream = request.GetRequestStream())
            {
                if (!OutputStream.CanWrite)
                {
                    throw new Exception("Could not wirte to RequestStream");
                }
                OutputStream.Write(data, 0, data.Length);
            }


            //Get response stream
            using (WebResponse resp = request.GetResponse())
            {
                using (Stream ResponseStream = resp.GetResponseStream())
                {
                    result = ReadStreamBinary(ResponseStream);
                }
            }
            return result;

        }

        public static NetStream DoHttpData(string url, NetStream stream, int timeout)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            NetStream result = null;
            byte[] data = stream.ToArray();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Timeout = GetTimeout(timeout);
            request.ContentType = GetContentType(RequestContentType.Data);
            request.ContentLength = data.Length;

            //Create request stream
            using (Stream OutputStream = request.GetRequestStream())
            {
                if (!OutputStream.CanWrite)
                {
                    throw new Exception("Could not wirte to RequestStream");
                }
                OutputStream.Write(data, 0, data.Length);
            }


            //Get response stream
            using (WebResponse resp = request.GetResponse())
            {
                using (Stream ResponseStream = resp.GetResponseStream())
                {
                    result = ReadStream(ResponseStream);
                }
            }
            return result;

        }

        public static TransStream DoHttpTransStream(string url, NetStream stream, int timeout)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            TransStream result = null;

            try
            {
                byte[] data = stream.ToArray();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Timeout = GetTimeout(timeout);
                request.ContentType = GetContentType(RequestContentType.Data);
                request.ContentLength = data.Length;

                //Create request stream
                using (Stream OutputStream = request.GetRequestStream())
                {
                    if (!OutputStream.CanWrite)
                    {
                        throw new Exception("Could not wirte to RequestStream");
                    }
                    OutputStream.Write(data, 0, data.Length);
                }


                //Get response stream
                using (WebResponse resp = request.GetResponse())
                {
                    using (Stream ResponseStream = resp.GetResponseStream())
                    {
                        result = TransStream.CopyFromStream(ResponseStream);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                return TransStream.WriteState(-1, "Response error: " + ex.Message);//, TransType.Error);
            }
        }

        public static void DoHttpTransStreamAsync(string url, NetStream stream, int timeout, Action<TransStream> onCompleted)
        {

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            TransStream result = null;

            try
            {
                byte[] data = stream.ToArray();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Timeout = GetTimeout(timeout);
                request.ContentType = GetContentType(RequestContentType.Data);
                request.ContentLength = data.Length;

                //Create request stream
                using (Stream OutputStream = request.GetRequestStream())
                {
                    if (!OutputStream.CanWrite)
                    {
                        throw new Exception("Could not wirte to RequestStream");
                    }
                    OutputStream.Write(data, 0, data.Length);
                }


                //Get response stream
                using (WebResponse resp = request.GetResponse())
                {
                    using (Stream ResponseStream = resp.GetResponseStream())
                    {
                        result = TransStream.CopyFromStream(ResponseStream);
                    }
                }
                onCompleted(result);
            }
            catch (Exception ex)
            {
                onCompleted(TransStream.WriteState(-1, "Response error: " + ex.Message));//, TransType.Error));
            }
        }

        public static string DoRequestSSL(string url, string postData, string encoding, int timeout, string user, string pass)
        {
            return DoRequestSSL(url, postData, encoding, null, false, timeout, user, pass);
        }

        public static string DoRequestSSL(string url, string postData, string encoding, string contentType, bool isUrlEncoded, int timeout, string user, string pass)
        {

            string response = null;

            WebRequest request = null;
            Stream newStream = null;
            Stream receiveStream = null;
            StreamReader readStream = null;
            WebResponse wresponse = null;

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            if (postData == null)
            {
                throw new ArgumentNullException("postData");
            }
            

            try
            {

                TrustedCertificatePolicy policy = new TrustedCertificatePolicy();

                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;

                Encoding enc = Encoding.GetEncoding(encoding);
                if (isUrlEncoded)
                {
                    postData = System.Web.HttpUtility.UrlEncode(postData, enc);
                }
                request = WebRequest.Create(url);
                request.PreAuthenticate = true;
                request.Credentials = new NetworkCredential(user, pass);
                //request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.Timeout = GetTimeout(timeout);
                request.ContentType = string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;

                byte[] byteArray = enc.GetBytes(postData);// Encoding.GetEncoding("Windows-1255").GetBytes(postData);
                                                          //request.ContentType = "text/xml";

                request.ContentLength = byteArray.Length;
                newStream = request.GetRequestStream();
                newStream.Write(byteArray, 0, byteArray.Length);
                newStream.Close();

                // Get the response.
                wresponse = request.GetResponse();
                receiveStream = wresponse.GetResponseStream();
                readStream = new StreamReader(receiveStream, enc);
                response = readStream.ReadToEnd();

                return response;
            }
            catch (System.Net.WebException webExcp)
            {
                throw webExcp;
            }
            catch (System.IO.IOException ioe)
            {
                throw ioe;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (newStream != null)
                    newStream.Close();
                if (receiveStream != null)
                    receiveStream.Close();
                if (readStream != null)
                    readStream.Close();
            }
        }

        public static bool CheckValidationResult(Object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public class TrustedCertificatePolicy : System.Net.ICertificatePolicy
        {
            public TrustedCertificatePolicy() { }

            public bool CheckValidationResult
            (
                System.Net.ServicePoint sp,
                System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Net.WebRequest request, int problem)
            {
                return true;
            }
        }
        #endregion

        #region satatic helpers

        public static HttpWebRequest CreateRequest(string url, string data, string method, RequestContentType contentType, int timeout, string encoding = "utf-8")
        {
            HttpWebRequest request = null;

            if (method.ToUpper() == "GET")
            {
                string postData = EncodeRequestData(contentType, data);
                string qs = string.IsNullOrEmpty(postData) ? "" : "?" + postData;
                request = (HttpWebRequest)WebRequest.Create(url + qs);
                request.Method = "GET";
                request.Timeout = GetTimeout(timeout);
                request.ContentType = GetContentType(contentType);
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;

                request.Timeout = GetTimeout(timeout);
                request.ContentType = GetContentType(contentType);

                Encoding enc = Encoding.GetEncoding(encoding);

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

            return request;
        }

 
        public static NameValueCollection ParseQueryString(string queryString)
        {
            NameValueCollection nv = HttpUtility.ParseQueryString(queryString);
            return nv;
        }

        public static RequestContentType GetContentType(string contentTypes)
        {
            if (contentTypes == null)
                throw new ArgumentNullException("ContentType");
            contentTypes = contentTypes.ToLower();

            if (contentTypes.StartsWith("application/x-www-form-urlencoded"))
                return RequestContentType.Form;
            else if (contentTypes.StartsWith("application/json"))
                return RequestContentType.Json;
            else if (contentTypes.StartsWith("text/xml"))
                return RequestContentType.Xml;
            else if (contentTypes.StartsWith("multipart/form-data"))
                return RequestContentType.Data;
            else
                throw new NotSupportedException("ContentType: " + contentTypes.ToString());
        }

        public static string GetContentType(RequestContentType contentTypes, string charSet = "utf-8")
        {
            switch (contentTypes)
            {
                case RequestContentType.Form:
                    return "application/x-www-form-urlencoded";
                case RequestContentType.Json:
                    return "application/json";
                case RequestContentType.Xml:
                    return "text/xml" + charSet == null ? "" : ";" + charSet;
                case RequestContentType.Data:
                    return "multipart/form-data";
                default:
                    throw new NotSupportedException("ContentType: " + contentTypes.ToString());
            }
        }

        public static string GetMethod(string method)
        {
            if (method == null)
                return "post";
            if (method.ToLower() == "get")
                return "get";
            return "post";
        }

        public static int GetTimeout(int timeout)
        {
            if (timeout <= 0)
                return DefaultTimeout;
            return timeout;

        }

        public static string EncodeRequestData(RequestContentType contentType, string data)
        {
            if (data == null)
            {
                return null;//  throw new ArgumentNullException("data");
            }
            switch (contentType)
            {
                //case ContentTypes.QueryString:
                //    return data.TrimStart('?');
                case RequestContentType.Form:
                    return EncodeQueryString(data);
                case RequestContentType.Json:
                    return data.TrimStart('/');
                case RequestContentType.Xml:
                    return data.TrimStart('/');
                case RequestContentType.Data:
                    return data;
                default:
                    throw new NotSupportedException("ContentType: " + contentType.ToString());
            }
        }
        public static string EncodeQueryString(string data)
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
                sb.Append(arg[0].Trim() + "=" + System.Web.HttpUtility.UrlEncode(arg[1].Trim()));

                counter++;
            }

            string postData = sb.ToString();

            return postData;
        }
        public static string DecodeQueryString(string data)
        {
            if (data == null)
            {
                return null;
            }
            return System.Web.HttpUtility.UrlDecode(data);
        }

        public static byte[] ReadStreamBinary(Stream stream, int bufferSize = 4096)
        {
            byte[] result = null;
            byte[] buffer = new byte[bufferSize];

            using (MemoryStream memoryStream = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, count);

                } while (count != 0);

                result = memoryStream.ToArray();
            }
            return result;
        }

        public static TransStream ReadTransStream(Stream stream, int bufferSize = 4096)
        {
            byte[] buffer = new byte[bufferSize];

            NetStream netStream = new NetStream();
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    netStream.Write(buffer, 0, count);

                } while (count != 0);
            }
            //return new TransStream(netStream, Runtime.TransformType.Stream, true);
            return new TransStream(netStream);
        }

        public static NetStream ReadStream(Stream stream, int bufferSize = 4096)
        {
            byte[] buffer = new byte[bufferSize];

            NetStream netStream = new NetStream();
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    netStream.Write(buffer, 0, count);

                } while (count != 0);
            }
            return netStream;
        }

        public static string ReadData(Stream bodyStream, Encoding encoding)
        {
            string result = null;
            using (var streamReader = new StreamReader(bodyStream, encoding))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        #endregion

    }

}

