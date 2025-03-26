using Nistec.Generic;
using Nistec.IO;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
//using System.Windows.Threading;
using System.Xml;
#pragma warning disable 1591

namespace Nistec.Channels.Http
{

    
    /// <summary>
    /// Http Requester
    /// </summary>
    public class Requester
    {
        #region demo
        public static async Task<T> RequesterDemo<T>()
        {
            var client = new Requester();
            client.AddHeaderAuth("name", "pass", AuthScheme.Basic);
            var response= await client.InvokePost(RequestType.Json,"https://myt.co.il","{\"data\":\"hello\"}");
            Console.WriteLine(response);
            return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStringAsync());
        }
        //public static async Task<T> RequesterJsonDemo<T>()
        //{
        //    var response = await Requester.PostJson("https://myt.co.il", "{\"data\":\"hello\"}", Requester.GenericAuthHeader("name", "pass"), CancellationToken.None);
        //    return await JsonSerializer.DeserializeAsync<T>(response);
        //}
        //public static async Task<T> RequesterHeadersDemo<T>()
        //{
        //    var headers= GenericNameValue.Create(AuthMetods.Authorization.ToString(), Requester.CreateAuthToken("name", "pass"), "id", "123");
        //    var response = await Requester.PostJson("https://myt.co.il", "{\"data\":\"hello\"}",headers , CancellationToken.None);
        //    return await JsonSerializer.DeserializeAsync<T>(response);
        //}
        #endregion

        #region properties
        //public int Timeout = 5000;

        CancellationTokenSource cancellationTokenSource;
        int ConnectTimeout = 5000;
        //bool CancelEnabled=false;
        public Action<LogLevel,string> Trace;

        
        public string SoapAction { get; set;}
        //public MethodType Method { get; set; }
        //public static MethodType MethodGet(string method)
        //{
        //    switch (method.ToUpper())
        //    {
        //        case "GET":
        //            return MethodType.GET;
        //        case "POST":
        //        default:
        //            return MethodType.POST;
        //    }
        //}
        //public static RequestType RequestTypeGet(string requestType)
        //{
        //    switch (requestType.ToLower())
        //    {
        //        case "json":
        //        case "application/json":
        //            return RequestType.Json;
        //        case "xml":
        //        case "text/xml":
        //            return RequestType.Xml;
        //        case "soap":
        //            return RequestType.Soap;
        //        case "form":
        //        case "application/x-www-form-urlencoded":
        //            return RequestType.Form;
        //        default:
        //            return RequestType.Json;
        //    }
        //}

        //public AuthScheme AuthScheme { get; set; }
        //public static AuthScheme AuthSchemeGet(string scheme)
        //{
        //    switch (scheme.ToLower())
        //    {
        //        case "basic":
        //            return AuthScheme.Basic;
        //        case "bBearer":
        //            return AuthScheme.Bearer;
        //        default:
        //            return AuthScheme.None;
        //    }
        //}

        GenericNameValue _Headers;
        public GenericNameValue Headers
        {

            get
            {
                if (_Headers == null)
                {
                    _Headers = new GenericNameValue();
                }
                return _Headers;
            }
        }

        #endregion

        #region ctor

        public Requester()
        {
            //Method = MethodType.POST;
            //AuthScheme = AuthScheme.None;
            //Headers = new GenericNameValue();
        }
        public Requester(int timeout)//, MethodType methodType = MethodType.POST)
        {
            ConnectTimeout = timeout;
            //Method = methodType;
            //Headers = new GenericNameValue();
        }
        public Requester(int timeout, string soapAction):this()
        {
            ConnectTimeout = timeout;
            SoapAction = soapAction;
            //Headers = new GenericNameValue();
        }

        public Requester(int timeout, string[] keyValueHeaders)
        {
            ConnectTimeout = timeout;
            //Method = methodType;
            //AuthScheme = scheme;
            //Headers = new GenericNameValue();
            AddHeader(keyValueHeaders);
        }
        public Requester(int timeout, string authToken, AuthScheme scheme)
        {
            ConnectTimeout = timeout;
            //Method = methodType;
            //AuthScheme = scheme;
            //Headers = new GenericNameValue();
            AddHeaderAuth(authToken, scheme);
        }
        #endregion

        #region public methods

        protected void OnTrace(LogLevel level, string action)
        {
            if (Trace != null)
                Trace.Invoke(level, action);
        }
        //public void AddHeaderAuth(string key, string value)
        //{
        //    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
        //        Headers.Add("Authorization", CreateAuthToken(key,value));
        //}
        public void AddHeaderAuth(string key, string value, AuthScheme scheme)
        {
            //AuthScheme = scheme;
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                Headers.Add("Authorization", CreateAuthToken(key, value, scheme));
        }
        //public void AddHeaderAuth(string token)
        //{
        //    if (!string.IsNullOrEmpty(token))
        //        Headers.Add("Authorization", token);
        //}
        public void AddHeaderAuth(string token, AuthScheme scheme)
        {
            //AuthScheme = scheme;
            if (!string.IsNullOrEmpty(token))
                Headers.Add("Authorization", CreateAuthToken(token, scheme));
        }
        //public void AddHeaderAuthByScema(string token, string scheme)
        //{
        //    AuthScheme = AuthSchemeGet(scheme);
        //    if (!string.IsNullOrEmpty(token))
        //        Headers.Add("Authorization", token);
        //}

        public void AddHeader(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                Headers.Add(key, value);
        }
        public void AddHeader(string[] keyValueArgs)
        {
            if (keyValueArgs != null && keyValueArgs.Length % 2 == 0)
                Headers.Add(keyValueArgs);
        }
        public void AddHeader(string keyValue, char spliter = ',')
        {
            if (!string.IsNullOrEmpty(keyValue))
            {
                var args = keyValue.SplitTrim(spliter);
                if (args != null && args.Length % 2 == 0)
                    Headers.Add(args);
            }
        }
        public void CancelRequest()
        {
            try
            {
                if (cancellationTokenSource != null && cancellationTokenSource.Token != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    OnTrace(LogLevel.Info, "Request has been canceled by user");
                }
            }
            catch (OperationCanceledException ocex)
            {
                OnTrace( LogLevel.Error, ocex.Message);
            }
        }

        #endregion
        public async Task<HttpResponseMessage> InvokePost(RequestType requestType, string address, string request, string[] keyValueHeaders=null)
        {
            try
            {
                if (keyValueHeaders != null && keyValueHeaders.Length > 0)
                {
                    Headers.Add(keyValueHeaders);
                }
                //string contentType = "application/json";
                switch (requestType)
                {
                    case RequestType.Form:
                        {
                            //var formContent = ParseForm(request);
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                //CancelEnabled = true;
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                return await DoFormRequest(address, request, cancellationTokenSource.Token);
                            }
                        }
                    case RequestType.Multipart:
                        {
                            var dict = QueryStringToDictionary(request, true);
                            var response = MultipartRequest.MultipartFormDataPost(address, dict);
                            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                            {
                                Content = new StringContent(response, Encoding.UTF8)//, "application/json")
                            };
                            //contentType = "multipart/form-data; boundary=--xxx";//"text/xml; charset=utf-8";
                            //txtResponse.Text = DoRequest(txtUrl.Text, txtAction.Text, cbMethod.Text, contentType, txtRequest.Text);
                        }
                    case RequestType.Soap:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                //CancelEnabled = true;
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                return await DoSoapRequest(address, SoapAction, request, cancellationTokenSource.Token);
                            }
                        }
                    case RequestType.Xml:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                return await DoPostRequest(address, "text/xml", request, cancellationTokenSource.Token);
                            }
                        }
                    default:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                return await DoJsonPostRequest(address, request, cancellationTokenSource.Token);
                            }
                        }
                }
            }
            catch (TimeoutException to)
            {
                OnTrace(LogLevel.Warn, "Send messsage timeout: " + to.Message);
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("RequestTimeout", Encoding.UTF8)//, "application/json")
                };
                //return (HttpStatusCode.RequestTimeout, "messsage timeout");
            }
            catch (UnauthorizedAccessException ux)
            {
                OnTrace(LogLevel.Warn, "Send messsage Unauthorized: " + ux.Message);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("messsage Unauthorized", Encoding.UTF8)//, "application/json")
                };
                //result(HttpStatusCode.Unauthorized, "messsage Unauthorized");
            }
            catch (OperationCanceledException ocex)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + ocex.Message);
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("messsage canceled", Encoding.UTF8)//, "application/json")
                };
                //result(HttpStatusCode.Conflict, "messsage canceled");
            }
            //catch (AggregateException ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            //}
            catch (HttpException hx)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + hx.Message);
                return new HttpResponseMessage((HttpStatusCode)hx.GetHttpCode())
                {
                    Content = new StringContent("messsage canceled", Encoding.UTF8)//, "application/json")
                };

                //result((HttpStatusCode)hx.GetHttpCode(), "messsage canceled" + hx.Message);
            }
            catch (Exception ex)
            {
                OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
                return new HttpResponseMessage(HttpStatusCode.ExpectationFailed)
                {
                    Content = new StringContent("messsage error: " + ex.Message, Encoding.UTF8)//, "application /json")
                };
                //result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            }


            //catch (OperationCanceledException ocex)
            //{
            //    OnTrace(LogLevel.Warn, "Send messsage canceled: " + ocex.Message.ToString());
            //    return null;
            //}
            //catch (AggregateException ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    return null;
            //}
            //catch (Exception ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    return null;
            //}
            //finally
            //{
            //    CancelEnabled = false;
            //}
        }

        public async Task<HttpResponseMessage> InvokeGet(RequestType requestType, string address, string[] keyValueHeaders = null)
        {
            try
            {
                if (keyValueHeaders != null && keyValueHeaders.Length > 0)
                {
                    Headers.Add(keyValueHeaders);
                }
                //string contentType = "application/json";
                switch (requestType)
                {
                    case RequestType.Xml:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                return await DoGetRequest(address, "text/xml", cancellationTokenSource.Token);
                            }
                        }
                    default:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                return await DoJsonGetRequest(address, cancellationTokenSource.Token);
                            }
                        }
                }
            }
            catch (TimeoutException to)
            {
                OnTrace(LogLevel.Warn, "Send messsage timeout: " + to.Message);
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("RequestTimeout", Encoding.UTF8)//, "application/json")
                };
                //return (HttpStatusCode.RequestTimeout, "messsage timeout");
            }
            catch (UnauthorizedAccessException ux)
            {
                OnTrace(LogLevel.Warn, "Send messsage Unauthorized: " + ux.Message);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("messsage Unauthorized", Encoding.UTF8)//, "application/json")
                };
                //result(HttpStatusCode.Unauthorized, "messsage Unauthorized");
            }
            catch (OperationCanceledException ocex)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + ocex.Message);
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("messsage canceled", Encoding.UTF8)//, "application/json")
                };
                //result(HttpStatusCode.Conflict, "messsage canceled");
            }
            //catch (AggregateException ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            //}
            catch (HttpException hx)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + hx.Message);
                return new HttpResponseMessage((HttpStatusCode)hx.GetHttpCode())
                {
                    Content = new StringContent("messsage canceled", Encoding.UTF8)//, "application/json")
                };

                //result((HttpStatusCode)hx.GetHttpCode(), "messsage canceled" + hx.Message);
            }
            catch (Exception ex)
            {
                OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
                return new HttpResponseMessage(HttpStatusCode.ExpectationFailed)
                {
                    Content = new StringContent("messsage error: " + ex.Message, Encoding.UTF8)//, "application /json")
                };
                //result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            }


            //catch (OperationCanceledException ocex)
            //{
            //    OnTrace(LogLevel.Warn, "Send messsage canceled: " + ocex.Message.ToString());
            //    return null;
            //}
            //catch (AggregateException ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    return null;
            //}
            //catch (Exception ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    return null;
            //}
            //finally
            //{
            //    CancelEnabled = false;
            //}
        }

        public async Task PostAsync(RequestType requestType, string address, string request, Action<HttpStatusCode, string> result)
        {
            await PostAsync(requestType, address, request, null,result);
        }

        public async Task PostAsync(RequestType requestType, string address, string request, string[] keyValueHeaders, Action<HttpStatusCode, string> result)
        {
            try
            {
                if (keyValueHeaders != null && keyValueHeaders.Length > 0)
                {
                    Headers.Add(keyValueHeaders);
                }
                //string contentType = "application/json";
                switch (requestType)
                {
                    case RequestType.Form:
                        {
                            //var formContent = ParseForm(request);
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                //CancelEnabled = true;
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                await DoFormRequest(address, request, cancellationTokenSource.Token, result);
                            }
                        }
                        break;
                    case RequestType.Multipart:
                        {
                            var dict = QueryStringToDictionary(request, true);
                            await MultipartRequest.MultipartFormDataPost(address, dict, result);
                            //contentType = "multipart/form-data; boundary=--xxx";//"text/xml; charset=utf-8";
                            //txtResponse.Text = DoRequest(txtUrl.Text, txtAction.Text, cbMethod.Text, contentType, txtRequest.Text);
                        }
                        break;
                    case RequestType.Soap:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                //CancelEnabled = true;
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                await DoSoapRequest(address, SoapAction, request, cancellationTokenSource.Token, result);
                            }
                        }
                        break;
                    case RequestType.Xml:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                await DoPostRequest(address, "text/xml", request, cancellationTokenSource.Token, result);
                            }
                        }
                        break;
                    default:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                await DoJsonPostRequest(address, request, cancellationTokenSource.Token, result);
                            }
                            break;
                        }
                }
            }
            catch (TimeoutException to)
            {
                OnTrace(LogLevel.Warn, "Send messsage timeout: " + to.Message);
                result(HttpStatusCode.RequestTimeout, "messsage timeout");
            }
            catch (UnauthorizedAccessException ux)
            {
                OnTrace(LogLevel.Warn, "Send messsage Unauthorized: " + ux.Message);
                result(HttpStatusCode.Unauthorized, "messsage Unauthorized");
            }
            catch (OperationCanceledException ocex)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + ocex.Message);
                result(HttpStatusCode.Conflict, "messsage canceled");
            }
            //catch (AggregateException ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            //}
            catch (HttpException hx)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + hx.Message);
                result((HttpStatusCode)hx.GetHttpCode(), "messsage canceled"+ hx.Message);
            }
            catch (Exception ex)
            {
                OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
                result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            }
            //finally
            //{
            //    CancelEnabled = false;
            //}
        }

        public async Task GetAsync(RequestType requestType, string address, string[] keyValueHeaders, Action<HttpStatusCode, string> result)
        {
            try
            {
                if (keyValueHeaders != null && keyValueHeaders.Length > 0)
                {
                    Headers.Add(keyValueHeaders);
                }
                //string contentType = "application/json";
                switch (requestType)
                {
                    case RequestType.Xml:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                await DoGetRequest(address, "text/xml", cancellationTokenSource.Token, result);
                            }
                        }
                        break;
                    default:
                        {
                            using (cancellationTokenSource = new CancellationTokenSource())
                            {
                                cancellationTokenSource.CancelAfter(ConnectTimeout);
                                //CancelEnabled = true;
                                await DoJsonGetRequest(address, cancellationTokenSource.Token, result);
                            }
                            break;
                        }
                }
            }
            catch (TimeoutException to)
            {
                OnTrace(LogLevel.Warn, "Send messsage timeout: " + to.Message);
                result(HttpStatusCode.RequestTimeout, "messsage timeout");
            }
            catch (UnauthorizedAccessException ux)
            {
                OnTrace(LogLevel.Warn, "Send messsage Unauthorized: " + ux.Message);
                result(HttpStatusCode.Unauthorized, "messsage Unauthorized");
            }
            catch (OperationCanceledException ocex)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + ocex.Message);
                result(HttpStatusCode.Conflict, "messsage canceled");
            }
            //catch (AggregateException ex)
            //{
            //    OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
            //    result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            //}
            catch (HttpException hx)
            {
                OnTrace(LogLevel.Warn, "Send messsage canceled: " + hx.Message);
                result((HttpStatusCode)hx.GetHttpCode(), "messsage canceled" + hx.Message);
            }
            catch (Exception ex)
            {
                OnTrace(LogLevel.Error, "Send messsage error: " + ex.ToString());
                result(HttpStatusCode.ExpectationFailed, "messsage error: " + ex.Message);
            }
            //finally
            //{
            //    CancelEnabled = false;
            //}
        }

        #region Helpers

        //public static GenericNameValue GenericHeader(params string[] keyValue)
        //{
        //    if (keyValue != null && keyValue.Length > 0)
        //    {
        //        return new GenericNameValue(keyValue);
        //    }
        //    return null;
        //}
        public static string[] CreateAuthHeader(string userName, string password, AuthScheme scheme)
        {
            var credetial = CreateAuthToken(userName, password, scheme);
            return new string[] { "Authorization", credetial };
        }
        public static string[] CreateAuthHeader(string token, AuthScheme scheme)
        {
            var credetial = scheme.ToString() + " " + token;
            return new string[] { "Authorization", credetial };
        }
        //public static string[] CreateAuthHeader(string token, string scheme)
        //{
        //    var credetial = scheme + " " + token;
        //    return new string[] { "Authorization", credetial };
        //}

        //static GenericNameValue GenericAuthHeader(string userName, string password)
        //{
        //    var token = userName + ":" + password;
        //    var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        //    return new GenericNameValue(new string[] { "Authorization", credetial });
        //}
        public static string CreateAuthToken(string userName, string password)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }
        public static string CreateAuthToken(string userName, string password, AuthScheme scheme)
        {
            return scheme.ToString() + " " + CreateAuthToken(userName , password);
        }
        public static string CreateAuthToken(string token, AuthScheme scheme)
        {
            return scheme.ToString() + " " + token;
        }

        void CreateHeader(System.Net.Http.HttpClient httpClient)
        {
            if (Headers != null && Headers.Count > 0)
            {
                foreach (var entry in Headers)
                {
                    if (entry.Key == "Authorization")
                    {
                        var scheme = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
                        var token = entry.Value.Substring(scheme.Length).Trim();
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.Trim(), token);
                    }
                    else
                        httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                }
            }
        }

        //void CreateHeader(GenericNameValue kv, System.Net.Http.HttpClient httpClient)
        //{
        //    CreateHeader(kv, httpClient, AuthScheme);

        //    //if (kv != null && kv.Count > 0)
        //    //{

        //    //    foreach (var entry in kv)
        //    //    {
        //    //        if (entry.Key == "Authorization")
        //    //        {
        //    //            var scheme = AuthScheme != AuthScheme.None ? AuthScheme.ToString(): entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
        //    //            var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
        //    //            var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        //    //            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme, credetial);
        //    //        }
        //    //        else
        //    //            httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
        //    //    }
        //    //}
        //}

        //static void CreateHeader(GenericNameValue kv, System.Net.Http.HttpClient httpClient)//, AuthScheme authScheme= AuthScheme.None)
        //{
        //    if (kv != null && kv.Count > 0)
        //    {

        //        foreach (var entry in kv)
        //        {
        //            if (entry.Key == "Authorization")
        //            {
        //                var scheme = authScheme != AuthScheme.None ? authScheme.ToString() : entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
        //                var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
        //                var credetial = token.Contains(':') ? Convert.ToBase64String(Encoding.UTF8.GetBytes(token)) : token;
        //                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme, credetial);
        //            }
        //            else
        //                httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
        //        }
        //    }
        //}

        //static void CreateHeader(string[] keyValueHheaders, System.Net.Http.HttpClient httpClient)
        //{
        //    CreateHeader(GenericNameValue.Create(keyValueHheaders), httpClient);

        //    //var kv = GenericHeader(keyValueHheaders);
        //    //if (kv != null && kv.Count > 0)
        //    //{

        //    //    foreach (var entry in kv)
        //    //    {
        //    //        if (entry.Key == "Authorization")
        //    //        {
        //    //            var scheme = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
        //    //            var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
        //    //            var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        //    //            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.Trim(), credetial);
        //    //        }
        //    //        else
        //    //            httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
        //    //    }
        //    //}
        //}
        /*
       static void CreateHeader(string headers, System.Net.Http.HttpClient httpClient)
       {
           if (string.IsNullOrEmpty(headers))
               return;
           Dictionary<string, string> hd = CreateHeaderDictionary(headers);
           CreateHeader(hd, httpClient);
       }

       static void CreateHeader(Dictionary<string, string> hd, System.Net.Http.HttpClient httpClient)
       {
           if (hd != null && hd.Count > 0)
           {
               foreach (var entry in hd)
               {
                   if (entry.Key == "Authorization")
                   {
                       var scheme = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
                       var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
                       var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
                       httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.Trim(), credetial);
                   }
                   else
                       httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
               }
           }
       }
         */
      static Dictionary<string, string> CreateHeaderDictionary(string headers, char spliter = ':')
       {
           Dictionary<string, string> hd = null;
           if (!string.IsNullOrEmpty(headers))
           {
               hd = new Dictionary<string, string>();
               var args = headers.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
               foreach (var arg in args)
               {
                   var kv = arg.Split(spliter);
                   if (kv.Length == 2)
                       hd[kv[0].Trim()] = kv[1].Trim();
               }
           }
           return hd;
       }
      
        static FormUrlEncodedContent ParseForm(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                throw new ArgumentNullException("Invalid request");
            }
            var dic = CreateHeaderDictionary(request, '=');
            var formContent = new FormUrlEncodedContent(dic.ToList());
            return formContent;
        }

        static string FormatAggrigateException(AggregateException ex, CancellationToken cancelToken)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Send messsage error: " + ex.Message);
            foreach (var inner in ex.InnerExceptions)
            {

                if (inner is TaskCanceledException)
                {
                    if (cancelToken.IsCancellationRequested && ((TaskCanceledException)inner).CancellationToken == cancelToken)
                    {
                        // a real cancellation, triggered by the caller
                        sb.AppendLine("Send messsage canceled by user");
                    }
                    sb.AppendLine(inner.Source + ":" + inner.Message + " (possibly request timeout)");
                    continue;
                }
                // a web request timeout (possibly other things!?)
                sb.AppendLine(inner.Source + ":" + inner.Message);
            }
            return sb.ToString();
        }
        #endregion

        #region methods

        public async Task<HttpResponseMessage> DoPostRequest(string address, string contentType, string data, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    //if (!response.IsSuccessStatusCode)
                    //    throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                    return response.EnsureSuccessStatusCode();//Content.ReadAsStringAsync();
                }
            }
        }
        public async Task<HttpResponseMessage> DoGetRequest(string address, string contentType, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", contentType);
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    //if (!response.IsSuccessStatusCode)
                    //    throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                    return response.EnsureSuccessStatusCode();//content.ReadAsStringAsync();
                }
            }
        }

        public async Task DoPostRequest(string address, string contentType, string data, CancellationToken ctsTocken, Action<HttpStatusCode, string> result)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    if (!response.IsSuccessStatusCode)
                        result(response.StatusCode, response.ReasonPhrase);
                    else
                        result(response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }
        public async Task DoGetRequest(string address, string contentType, CancellationToken ctsTocken, Action<HttpStatusCode, string> result)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", contentType);
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    if (!response.IsSuccessStatusCode)
                        result(response.StatusCode, response.ReasonPhrase);
                    else
                        result(response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }

        public async Task<HttpResponseMessage> DoJsonPostRequest(string address, string data, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    //if (!response.IsSuccessStatusCode)
                    //    throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                    return response.EnsureSuccessStatusCode();//response.Content.ReadAsStringAsync();
                }
            }
        }
        public async Task DoJsonPostRequest(string address, string data, CancellationToken ctsTocken, Action<HttpStatusCode,string> result)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    if (!response.IsSuccessStatusCode)
                        result(response.StatusCode, response.ReasonPhrase);
                    else
                        result(response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }
        public async Task<HttpResponseMessage> DoJsonGetRequest(string address, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    //if (!response.IsSuccessStatusCode)
                    //    throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                    return response.EnsureSuccessStatusCode();//await content.ReadAsStringAsync();
                }
            }
        }
        public async Task DoJsonGetRequest(string address, CancellationToken ctsTocken, Action<HttpStatusCode, string> result)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    if (!response.IsSuccessStatusCode)
                        result(response.StatusCode, response.ReasonPhrase);
                    else
                        result(response.StatusCode, await content.ReadAsStringAsync());
                }
            }
        }

        //from test app

        //public async Task<string> DoJsonGetRequest2(string address, string headers, CancellationToken ctsTocken)
        //{
        //    using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
        //    {
        //        CreateHeader(httpClient);
        //        httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
        //        using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
        //        using (HttpContent content = response.Content)
        //        {
        //            return await content.ReadAsStringAsync();
        //        }
        //    }
        //}
        //public async Task<string> DoJsonGetRequest2(string address, string[] keyValueHeaders, CancellationToken ctsTocken)
        //{
        //    using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
        //    {
        //        CreateHeader(keyValueHeaders, httpClient);
        //        httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
        //        using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
        //        using (HttpContent content = response.Content)
        //        {
        //            return await content.ReadAsStringAsync();
        //        }
        //    }
        //}
        public async Task<HttpResponseMessage> DoFormRequest(string address, string data, CancellationToken ctsTocken)
        {
            var formContent = ParseForm(data);
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
                using (var response = await httpClient.PostAsync(address, formContent, ctsTocken))
                {
                    //if (!response.IsSuccessStatusCode)
                    //    throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                    return response.EnsureSuccessStatusCode();//.Content.ReadAsStringAsync();
                }
            }
        }
        public async Task DoFormRequest(string address, string data, CancellationToken ctsTocken, Action<HttpStatusCode, string> result)
        {
            var formContent = ParseForm(data);
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
                using (var response = await httpClient.PostAsync(address, formContent, ctsTocken))
                {
                    if (!response.IsSuccessStatusCode)
                        result(response.StatusCode, response.ReasonPhrase);
                    else
                        result(response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }

        public async Task<HttpResponseMessage> DoSoapRequest(string address, string soapAction, string data, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, address))
                {
                    //request.Headers.Add("SOAPAction", soapAction);
                    using (HttpContent content = new StringContent(data, Encoding.UTF8, "text/xml"))
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, ctsTocken))
                    {
                        //response.EnsureSuccessStatusCode(); // throws an Exception if 404, 500, etc.
                        //if (!response.IsSuccessStatusCode)
                        //    throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                        return response.EnsureSuccessStatusCode();//await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        public async Task DoSoapRequest(string address, string soapAction, string data, CancellationToken ctsTocken, Action<HttpStatusCode, string> result)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, address))
                {
                    //request.Headers.Add("SOAPAction", soapAction);
                    using (HttpContent content = new StringContent(data, Encoding.UTF8, "text/xml"))
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, ctsTocken))
                    {
                        //response.EnsureSuccessStatusCode(); // throws an Exception if 404, 500, etc.
                        if (!response.IsSuccessStatusCode)
                            result(response.StatusCode, response.ReasonPhrase);
                        else
                            result(response.StatusCode, await content.ReadAsStringAsync());
                    }
                }
            }
        }

        public async Task<Stream> DoRequest(string address, string soapAction, byte[] data, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                httpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, address))
                {
                    //request.Headers.Add("SOAPAction", soapAction);
                    using (HttpContent content = new StreamContent(new NetStream(data)))
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, ctsTocken))
                    {
                        //response.EnsureSuccessStatusCode(); // throws an Exception if 404, 500, etc.
                        if (!response.IsSuccessStatusCode)
                            throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
                        return await response.Content.ReadAsStreamAsync();
                    }
                }
            }
        }


        #endregion

        #region multipart

        public async Task<Stream> PostMultipart(string address, string boundary, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new MultipartFormDataContent(boundary))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    return await response.Content.ReadAsStreamAsync();
                }
            }
        }

        public static string CLeanQueryString(string qs)
        {
            return qs.Replace("&amp;", "&");
        }
        public static Dictionary<string, object> QueryStringToDictionary(string qs, bool enableRegexMatch)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            if (qs == null)
                return null;

            string str = CLeanQueryString(qs);

            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            if (!str.Contains('='))
            {
                return null;
            }

            foreach (string arg in str.Split(new char[] { '&' }))
            {
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                string[] strArray = arg.Split(new char[] { '=' });
                if (strArray.Length == 2)
                {
                    string key = strArray[0];// Regex.Replace("amp;", strArray[0], "");
                    string type = "string";
                    object val = strArray[1];
                    string[] strType = key.Split(new char[] { ':' });
                    if (strType.Length == 2)
                    {
                        key = strType[0];
                        type = strType[1];
                    }
                    else if (enableRegexMatch)
                    {
                        //if(Types.IsNumber(strArray[1]))
                        //if (Regex.IsMatch(strArray[1], @"^(-|)([0-9]\.|[1-9])+[0-9]+$"))
                        //{
                        //    type = "number";
                        //}

                        if (Regex.IsMatch(strArray[1], @"^(-|)[0-9]\.+[0-9]+$"))
                        {
                            type = "double";
                        }
                        else if (Regex.IsMatch(strArray[1], @"^(-|)[1-9]+[0-9]+$"))
                        {
                            type = "long";
                        }
                        //else if (Regex.IsMatch(strArray[1], @"^(-|)[1-9]+$"))
                        //{
                        //    type = "int";
                        //}
                        else if (Regex.IsMatch(strArray[1], @"^(false|true)$", RegexOptions.IgnoreCase))
                        {
                            type = "bool";
                        }
                    }

                    switch (type)
                    {
                        case "int":
                            val = Types.ToInt(val);
                            break;
                        case "long":
                            val = Types.ToLong(val);
                            break;
                        case "float":
                            val = Types.ToFloat(val, 0);
                            break;
                        case "double":
                            val = Types.ToDouble(val, 0);
                            break;
                        case "bool":
                            val = Types.ToBool(val, false);
                            break;
                        case "string":
                        default:

                            break;
                    }
                    dictionary.Add(key, val);
                }

            }

            return dictionary;
        }

        public static class MultipartRequest
        {
            private static readonly Encoding encoding = Encoding.UTF8;
            public static string MultipartFormDataPost(string postUrl, Dictionary<string, object> postParameters)
            {
                Stream receiveStream = null;
                StreamReader readStream = null;

                try
                {
                    string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                    string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                    byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

                    var wresponse = PostForm(postUrl, contentType, formData);

                    Encoding enc = Encoding.GetEncoding("utf-8");

                    receiveStream = wresponse.GetResponseStream();
                    readStream = new StreamReader(receiveStream, enc);
                    var response = readStream.ReadToEnd();

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
                    if (receiveStream != null)
                        receiveStream.Close();
                    if (readStream != null)
                        readStream.Close();
                }

            }

            public static async Task MultipartFormDataPost(string postUrl, Dictionary<string, object> postParameters, Action<HttpStatusCode, string> result)
            {
                Stream receiveStream = null;
                StreamReader readStream = null;

                try
                {
                    string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                    string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                    byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

                    var wresponse = PostForm(postUrl, contentType, formData);

                    Encoding enc = Encoding.GetEncoding("utf-8");

                    receiveStream = wresponse.GetResponseStream();
                    readStream = new StreamReader(receiveStream, enc);
                    var response = await readStream.ReadToEndAsync();

                    result(HttpStatusCode.OK, response);
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
                    if (receiveStream != null)
                        receiveStream.Close();
                    if (readStream != null)
                        readStream.Close();
                }

            }
            private static HttpWebResponse PostForm(string postUrl, string contentType, byte[] formData)
            {
                HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

                if (request == null)
                {
                    throw new NullReferenceException("request is not a http request");
                }

                // Set up the request properties.
                request.Method = "POST";
                request.ContentType = contentType;
                //request.UserAgent = userAgent;
                request.CookieContainer = new CookieContainer();
                request.ContentLength = formData.Length;

                // You could add authentication here as well if needed:
                // request.PreAuthenticate = true;
                // request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
                // request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("username" + ":" + "password")));

                // Send the form data to the request.
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(formData, 0, formData.Length);
                    requestStream.Close();
                }

                return request.GetResponse() as HttpWebResponse;
            }

            private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
            {
                Stream formDataStream = new System.IO.MemoryStream();
                bool needsCLRF = false;

                foreach (var param in postParameters)
                {
                    // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                    // Skip it on the first parameter, add it to subsequent parameters.
                    if (needsCLRF)
                        formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                    needsCLRF = true;

                    if (param.Value is FileParameter)
                    {
                        FileParameter fileToUpload = (FileParameter)param.Value;

                        // Add just the first part of this param, since we will write the file data directly to the Stream
                        string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                            boundary,
                            param.Key,
                            fileToUpload.FileName ?? param.Key,
                            fileToUpload.ContentType ?? "application/octet-stream");

                        formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                        // Write the file data directly to the Stream, rather than serializing it to a string.
                        formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                    }
                    else
                    {
                        string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                            boundary,
                            param.Key,
                            param.Value);
                        formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                    }
                }

                // Add the end of the request.  Start with a newline
                string footer = "\r\n--" + boundary + "--\r\n";
                formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

                // Dump the Stream into a byte[]
                formDataStream.Position = 0;
                byte[] formData = new byte[formDataStream.Length];
                formDataStream.Read(formData, 0, formData.Length);
                formDataStream.Close();

                return formData;
            }

            public class FileParameter
            {
                public byte[] File { get; set; }
                public string FileName { get; set; }
                public string ContentType { get; set; }
                public FileParameter(byte[] file) : this(file, null) { }
                public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
                public FileParameter(byte[] file, string filename, string contenttype)
                {
                    File = file;
                    FileName = filename;
                    ContentType = contenttype;
                }
            }
        }
        #endregion

        #region static fast
        /*
        public static string GetRequest(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static async Task<string> GetRequestAsync(string uri, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                //CreateHeader(headers, httpClient);
                //httpClient.DefaultRequestHeaders.Add("ContentType", contentType);
                using (HttpResponseMessage response = await httpClient.GetAsync(uri))
                using (HttpContent content = response.Content)
                {
                    return await content.ReadAsStringAsync();
                }
            }

            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            ////request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            //using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            //using (Stream stream = response.GetResponseStream())
            //using (StreamReader reader = new StreamReader(stream))
            //{
            //    return await reader.ReadToEndAsync();
            //}
        }

        static async Task<string> PostRequest(string address, string contentType, string data, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout=5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        static async Task<string> GetRequest(string address, string contentType, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", contentType);
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    return await content.ReadAsStringAsync();
                }
            }
        }

        static async Task<string> PostForm(string address, string data, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            var formContent = ParseForm(data);
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded");
                using (var response = await httpClient.PostAsync(address, formContent, ctsTocken))
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        static async Task<string> PostSoap(string address, string soapAction, string data, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                httpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, address))
                {
                    //request.Headers.Add("SOAPAction", soapAction);
                    using (HttpContent content = new StringContent(data, Encoding.UTF8, "text/xml"))
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, ctsTocken))
                    {
                        //response.EnsureSuccessStatusCode(); // throws an Exception if 404, 500, etc.
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }
        static async Task<string> GetSoap(string address, string soapAction, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                httpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    return await content.ReadAsStringAsync();
                }
            }
        }

        static async Task<string> PostJson(string address, string data, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                CreateHeader(headers, httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        static async Task<string> GetJson(string address, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                using (HttpResponseMessage response = await httpClient.GetAsync(address, ctsTocken))
                using (HttpContent content = response.Content)
                {
                    return await content.ReadAsStringAsync();
                }
            }
        }

        static async Task<Stream> PostStream(string address, byte[] data, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new StreamContent(new NetStream(data)))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    return await response.Content.ReadAsStreamAsync();
                }
            }
        }

        private static async Task<Stream> PostMultipart(string address, string boundary, GenericNameValue headers, CancellationToken ctsTocken, int ConnectTimeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(ConnectTimeout) })
            {
                CreateHeader(headers, httpClient);
                using (var request = new HttpRequestMessage(HttpMethod.Post, address))
                using (request.Content = new MultipartFormDataContent(boundary))
                using (var response = await httpClient.SendAsync(request, ctsTocken))
                {
                    return await response.Content.ReadAsStreamAsync();
                }
            }
        }
        */
        #endregion

        #region static
        /*
        public static Task<string> PostJson(string address, string data, string authToken, AuthScheme scheme =  AuthScheme.Basic, int Timeout = 5000)
        {
            HttpMethod method = HttpMethod.Post;
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.ToString(), authToken);
                using (var request = new HttpRequestMessage(method, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = httpClient.SendAsync(request))
                {

                    return response.Result.Content.ReadAsStringAsync();
                }
            }
        }


        public static Task<string> InvokeJson(string address, HttpMethod method, string data, GenericNameValue headers, AuthScheme scheme = AuthScheme.Basic, int Timeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            {
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                if (headers != null && headers.Count > 0)
                {
                    foreach (var entry in headers)
                    {
                        if (entry.Key == "Authorization")
                        {
                            //var scheme = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
                            //var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
                            //var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.ToString(), entry.Value);
                        }
                        else
                            httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    }
                }
                using (var request = new HttpRequestMessage(method, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = httpClient.SendAsync(request))
                {

                    return response.Result.Content.ReadAsStringAsync();
                }
            }
        }

        public static Task<string> Invoke(string address, HttpMethod method, string contentType, string data, int Timeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = httpClient.SendAsync(request))
            {

                return response.Result.Content.ReadAsStringAsync();
            }
        }

        public static Task<string> Invoke(string address, HttpMethod method, string contentType, string data, CancellationToken ctsTocken, int Timeout = 5000)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = httpClient.SendAsync(request, ctsTocken))
            {

                return response.Result.Content.ReadAsStringAsync();
            }
        }

        //public static async Task<string> Invoke(string address, HttpMethod method, string contentType, string data, int Timeout = 5000)
        //{
        //    using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
        //    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Timeout)))
        //    using (var request = new HttpRequestMessage(method, address))
        //    using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
        //    using (var response = await httpClient.SendAsync(request, cts.Token))
        //    {

        //        return await response.Content.ReadAsStringAsync();
        //    }
        //}

        public static async Task<string> InvokePost(string address, string data)
        {
            using (var client = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(20) })
            using (var content = new StringContent(data, Encoding.UTF8))
            using (HttpResponseMessage response = await client.PostAsync(address, content))
            using (HttpContent result = response.Content)
            {
                return await result.ReadAsStringAsync();
            }

        }

        public static async Task<string> InvokeGet(string address)
        {
            using (var client = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(20) })
            using (HttpResponseMessage response = await client.GetAsync(address))
            using (HttpContent content = response.Content)
            {
                return await content.ReadAsStringAsync();
            }
        }

        public static string InvokeSoap(string url, string soapAction, string method, string contentType, string soapBody, int Timeout = 5000)
        {
            string result = null;

            try
            {
                //Create HttpWebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = method;
                request.ContentType = contentType + "; charset=utf-8";//"text/xml; charset=utf-8";
                request.Timeout = (int)TimeSpan.FromMilliseconds(Timeout).TotalMilliseconds;
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
        public static string InvokeForm(string url, string action, string method, string contentType, string postArgs, int Timeout=20)
        {

            string response = null;

            WebRequest request = null;
            Stream newStream = null;
            Stream receiveStream = null;
            StreamReader readStream = null;
            WebResponse wresponse = null;
            string encoding = "utf-8";
            int timeout = (int)TimeSpan.FromMilliseconds(Timeout).TotalMilliseconds;

            try
            {
                Encoding enc = Encoding.GetEncoding(encoding);

                StringBuilder sb = new StringBuilder();
                int counter = 0;
                postArgs = postArgs.Replace("&amp;", "%26");
                string[] args = postArgs.Replace("\r\n", "").Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in args)
                {
                    string[] arg = s.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                    if (counter > 0)
                        sb.Append("&");
                    sb.Append(arg[0].Trim() + "=" + HttpUtility.UrlEncode(arg[1].Trim().Replace("%26", "&")));

                    counter++;
                }

                string postData = sb.ToString();
                //string postData = HttpUtility.UrlEncode(postArgs);

                if (method.ToUpper() == "GET")
                {
                    string qs = string.IsNullOrEmpty(postData) ? "" : "?" + postData;

                    request = WebRequest.Create(url + qs);
                    request.Timeout = timeout <= 0 ? 100000 : timeout;
                    if (!string.IsNullOrEmpty(contentType))
                        request.ContentType = contentType;// string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;

                }
                else
                {
                    request = WebRequest.Create(url);
                    request.Method = "POST";
                    request.Credentials = CredentialCache.DefaultCredentials;

                    request.Timeout = timeout <= 0 ? 100000 : timeout;
                    request.ContentType = string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;

                    byte[] byteArray = enc.GetBytes(postData);
                    request.ContentLength = byteArray.Length;

                    newStream = request.GetRequestStream();
                    newStream.Write(byteArray, 0, byteArray.Length);
                    newStream.Close();

                }


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
        */
        #endregion
    }

#if (false)
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class Requester
    {
        const int Timeout = 5000;

        //public MainWindow()
        //{
        //    InitializeComponent();
        //    cbMethod.SelectedIndex = 0;
        //    cbContentType.SelectedIndex = 0;
        //}

        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="txtHeader"></param>
        /// <param name="txtRequest"></param>
        /// <param name="formatType"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public MessageAck Invoke(string url, string method, string txtHeader, string txtRequest, string formatType, string action )
        {
            //bool isValid = false;
            string txtResponse = "";
            //string txtStatus = "";
            //CancellationToken cancelToken;
            //string formatType = cbContentType.Text;
            try
            {
                //InvalidateVisual();
                //Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);

                if (url.Length == 0)
                    return DoAck(ChannelState.BadRequest, "Invalid Url!");
                
                if (formatType != "Form")
                {
                    if (action.Length == 0)
                        return DoAck(ChannelState.BadRequest, "Invalid Action!");
                    else if (txtRequest.Length == 0)
                        return DoAck(ChannelState.BadRequest, "Invalid Request!");
                }
               

                //txtStatus = "Do request, Please Wait...";
                //InvalidateVisual();
                //Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);

                string contentType = "application/json";
                switch (formatType)
                {
                    case "Form":
                        {
                            contentType = "application/x-www-form-urlencoded";//"text/xml; charset=utf-8";
                            txtResponse = DoRequest(url, action, method, contentType, txtRequest);
                        }
                        break;
                    case "Multipart":
                        {
                            var dict = QueryStringToDictionary(txtRequest, true);
                            txtResponse = MultipartRequest.MultipartFormDataPost(url, dict);
                            //contentType = "multipart/form-data; boundary=--xxx";//"text/xml; charset=utf-8";
                            //txtResponse.Text = DoRequest(txtUrl.Text, txtAction.Text, cbMethod.Text, contentType, txtRequest.Text);
                        }
                        break;
                    case "Soap Xml":
                        {
                            contentType = "text/xml";
                            string response = DoSoapRequest(url, action, method, contentType, txtRequest);
                            txtResponse = PrintXML(response);
                        }
                        break;
                    case "Xml":
                        {
                            contentType = "text/xml";
                            Task<string> response = null;
                            var httpMethod = GetMethod(method);
                            if (httpMethod == HttpMethod.Get)
                                response = DoJsonGetRequest(url);
                            else //if (httpMethod == HttpMethod.Post)
                                response = DoHttpRequest(url, GetMethod(method), contentType, txtRequest, txtHeader);
                            txtResponse = PrintXML(response.Result);
                        }
                        break;
                    default:
                        {
                            contentType = "application/json";
                            var httpMethod = GetMethod(method);
                            Task<string> response = null;
                            if (httpMethod == HttpMethod.Get)
                                response = DoJsonGetRequest(url);
                            else //if (httpMethod == HttpMethod.Post)
                                response = DoHttpRequest(url, GetMethod(method), contentType, txtRequest, txtHeader);

                            if (response == null)

                                txtResponse = "No response from server!";
                            else
                                txtResponse = PrintJson(response.Result);

                        }
                        break;

                }
                //txtStatus = "Completed!";
                return DoResponse(ChannelState.Ok, txtResponse);
            }
            catch (AggregateException aex)
            {
                //txtStatus = "Error!";
                txtResponse = "Send messsage error: " + aex.ToString();//FormatAggrigateException(aex, cancelToken);
                return DoAck(ChannelState.UnexpectedError, txtResponse);
            }
            catch (Exception ex)
            {
                //txtStatus = "Error!";
                txtResponse = "Send messsage error: " + ex.ToString();// FormatException(ex, formatType);

                return DoAck(ChannelState.UnexpectedError, txtResponse);
            }
        }

        public GenericNameValue CreateHeader(params string[] keyValue)
        {
            return new GenericNameValue(keyValue);
        }
        public GenericNameValue CreateHeader(string userName, string password)
        {
            var token = userName+":"+ password;
            var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            return new GenericNameValue(new string[] { "Authorization", credetial });
        }

        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data, string scheme, GenericNameValue headers)
        {
            //Dictionary<string, string> hd = null;
            //if (!string.IsNullOrEmpty(headers))
            //{
            //    hd = new Dictionary<string, string>();
            //    var args = headers.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (var arg in args)
            //    {
            //        var kv = arg.Split(':');
            //        if (kv.Length == 2)
            //            hd[kv[0].Trim()] = kv[1].Trim();
            //    }
            //}
            using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
            {
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                if (headers != null && headers.Count > 0)
                {
                    foreach (var entry in headers)
                    {
                        if (entry.Key == "Authorization")
                        {
                            //var scheme = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
                            //var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
                            //var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.Trim(), entry.Value);
                        }
                        else
                            httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    }
                }
                using (var request = new HttpRequestMessage(method, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = httpClient.SendAsync(request))
                {

                    return response.Result.Content.ReadAsStringAsync();
                }
            }

            //using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) 
            //using (var request = new HttpRequestMessage(method, address))
            //using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            //using (var response = httpClient.SendAsync(request))
            //{

            //    return response.Result.Content.ReadAsStringAsync();
            //}
        }
        /*
        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data, string headers)
        {
            Dictionary<string, string> hd = null;
            if (!string.IsNullOrEmpty(headers))
            {
                hd = new Dictionary<string, string>();
                var args = headers.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var arg in args)
                {
                    var kv = arg.Split(':');
                    if (kv.Length == 2)
                        hd[kv[0].Trim()] = kv[1].Trim();
                }
            }
            using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
            {
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                if (hd != null && hd.Count > 0)
                {
                    foreach (var entry in hd)
                    {
                        if (entry.Key == "Authorization")
                        {
                            var scheme = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Bearer " : "Basic ";
                            var token = entry.Value.Substring(scheme.Length).Trim().Replace("|", ":");
                            var credetial = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme.Trim(), credetial);
                        }
                        else
                            httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    }
                }
                using (var request = new HttpRequestMessage(method, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = httpClient.SendAsync(request))
                {

                    return response.Result.Content.ReadAsStringAsync();
                }
            }

            //using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) 
            //using (var request = new HttpRequestMessage(method, address))
            //using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            //using (var response = httpClient.SendAsync(request))
            //{

            //    return response.Result.Content.ReadAsStringAsync();
            //}
        }
        */

    #region helpers

        static MessageAck DoResponse(ChannelState status, string message, object response = null)
        {
            return MessageAck.DoResponse(status, message, response);
        }
        static MessageAck DoAck(ChannelState status,string message, string identifier=null)
        {
            return MessageAck.DoAck(status, message, identifier);
        }

        static string FormatException(Exception ex, string format)
        {
            if (ex == null)
                return "";
            if (ex.InnerException != null)
                return ex.Message + " Inner: " + ex.InnerException;
            if (format == "Xml")
                return PrintXML(ex.Message);
            return ex.Message;
        }

        static string FormatAggrigateException(AggregateException ex, CancellationToken cancelToken)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Send messsage error: " + ex.Message);
            foreach (var inner in ex.InnerExceptions)
            {

                if (inner is TaskCanceledException)
                {
                    if (cancelToken.IsCancellationRequested && ((TaskCanceledException)inner).CancellationToken == cancelToken)
                    {
                        // a real cancellation, triggered by the caller
                        sb.AppendLine("Send messsage canceled by user");
                    }
                    sb.AppendLine(inner.Source + ":" + inner.Message + " (possibly request timeout)");
                    continue;
                }
                // a web request timeout (possibly other things!?)
                sb.AppendLine(inner.Source + ":" + inner.Message);
            }
            return sb.ToString();
        }

        HttpMethod GetMethod(string method)
        {
            switch (method)
            {
                case "Get":
                    return HttpMethod.Get;
                case "Post":
                default:
                    return HttpMethod.Post;

            }
        }
    #endregion

    #region http request methods

        static Dictionary<string, string> ParseHeader(string headers)
        {
            if (string.IsNullOrEmpty(headers))
                return null;
            var args = headers.Split(new string[] { Environment.NewLine, ";", "|" }, StringSplitOptions.RemoveEmptyEntries);
            return ParseHeader(args);
        }

        static Dictionary<string, string> ParseHeader(string[] headers)
        {
            Dictionary<string, string> hd = null;
            if (headers!=null)
            {
                hd = new Dictionary<string, string>();
                //var args = headers.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var arg in headers)
                {
                    var kv = arg.Split(':');
                    if (kv.Length == 2)
                        hd[kv[0].Trim()] = kv[1].Trim();
                }
            }
            return hd;
        }

        static void SetHeaders(System.Net.Http.HttpClient httpClient, Dictionary<string, string> hd)
        {
            if (hd != null && hd.Count > 0)
            {
                foreach (var entry in hd)
                {
                    if (entry.Key == "Authorization")
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", entry.Value);
                    else
                        httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                }
            }
        }

        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data, string headers)
        {
            Dictionary<string, string> hd = ParseHeader(headers);
 
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                if (hd != null && hd.Count > 0)
                {
                    foreach (var entry in hd)
                    {
                        if (entry.Key == "Authorization")
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", entry.Value);
                        else
                            httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    }
                }
                using (var request = new HttpRequestMessage(method, address))
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using (request.Content = new StringContent(data, Encoding.UTF8, "application/json"))
                using (var response = httpClient.SendAsync(request))
                {

                    return response.Result.Content.ReadAsStringAsync();
                }
            }

            //using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) 
            //using (var request = new HttpRequestMessage(method, address))
            //using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            //using (var response = httpClient.SendAsync(request))
            //{

            //    return response.Result.Content.ReadAsStringAsync();
            //}
        }

        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = httpClient.SendAsync(request))
            {

                return response.Result.Content.ReadAsStringAsync();
            }
        }

        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data, CancellationToken ctsTocken)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = httpClient.SendAsync(request, ctsTocken))
            {

                return response.Result.Content.ReadAsStringAsync();
            }
        }

        static async Task<string> DoHttpRequestAsync(string address, HttpMethod method, string contentType, string data)
        {
            using (var httpClient = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(Timeout) })
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Timeout)))
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = await httpClient.SendAsync(request, cts.Token))
            {

                return await response.Content.ReadAsStringAsync();
            }
        }

        static async Task<string> DoJsonPostRequest(string address, string data)
        {
            using (var client = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(20) })
            using (var content = new StringContent(data, Encoding.UTF8))
            using (HttpResponseMessage response = await client.PostAsync(address, content))
            using (HttpContent result = response.Content)
            {
                return await result.ReadAsStringAsync();
            }

        }

        static async Task<string> DoJsonGetRequest(string address)
        {
            using (HttpClient client = new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMilliseconds(20) })
            using (HttpResponseMessage response = await client.GetAsync(address))
            using (HttpContent content = response.Content)
            {
                return await content.ReadAsStringAsync();
            }
        }

        static string DoSoapRequest(string url, string soapAction, string method, string contentType, string soapBody)
        {
            string result = null;

            try
            {
                //Create HttpWebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = method;
                request.ContentType = contentType + "; charset=utf-8";//"text/xml; charset=utf-8";
                request.Timeout = (int)TimeSpan.FromMilliseconds(Timeout).TotalMilliseconds;
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


        static string DoRequest(string url, string action, string method, string contentType, string postArgs)
        {

            string response = null;

            WebRequest request = null;
            Stream newStream = null;
            Stream receiveStream = null;
            StreamReader readStream = null;
            WebResponse wresponse = null;
            string encoding = "utf-8";
            int timeout = (int)TimeSpan.FromMilliseconds(Timeout).TotalMilliseconds;

            try
            {
                Encoding enc = Encoding.GetEncoding(encoding);

                StringBuilder sb = new StringBuilder();
                int counter = 0;
                postArgs = postArgs.Replace("&amp;", "%26");
                string[] args = postArgs.Replace("\r\n", "").Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in args)
                {
                    string[] arg = s.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                    if (counter > 0)
                        sb.Append("&");
                    sb.Append(arg[0].Trim() + "=" + HttpUtility.UrlEncode(arg[1].Trim().Replace("%26", "&")));

                    counter++;
                }

                string postData = sb.ToString();
                //string postData = HttpUtility.UrlEncode(postArgs);

                if (method.ToUpper() == "GET")
                {
                    string qs = string.IsNullOrEmpty(postData) ? "" : "?" + postData;

                    request = WebRequest.Create(url + qs);
                    request.Timeout = timeout <= 0 ? 100000 : timeout;
                    if (!string.IsNullOrEmpty(contentType))
                        request.ContentType = contentType;// string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;

                }
                else
                {
                    request = WebRequest.Create(url);
                    request.Method = "POST";
                    request.Credentials = CredentialCache.DefaultCredentials;

                    request.Timeout = timeout <= 0 ? 100000 : timeout;
                    request.ContentType = string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;

                    byte[] byteArray = enc.GetBytes(postData);
                    request.ContentLength = byteArray.Length;

                    newStream = request.GetRequestStream();
                    newStream.Write(byteArray, 0, byteArray.Length);
                    newStream.Close();

                }


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

    #endregion

    #region events
        /*
        void OnTypeChanged(string selectedType, string method)
        {
            txtRequest.IsEnabled = selectedType == "Soap Xml" || selectedType == "Form" || selectedType == "Multipart" || method == "POST";

            txtAction.IsEnabled = selectedType == "Soap Xml";
        }

        private void cbMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;

            OnTypeChanged(cbContentType.Text, text);

        }
        private void cbContentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;

            OnTypeChanged(text, cbMethod.Text);
        }
        */
    #endregion

    #region formatter

        internal static string Indent = "   ";

        internal static void AppendIndent(StringBuilder sb, int count)
        {
            for (; count > 0; --count) sb.Append(Indent);
        }

        internal static string PrintJson(string input)
        {
            var output = new StringBuilder();
            int depth = 0;
            int len = input.Length;
            char[] chars = input.ToCharArray();
            for (int i = 0; i < len; ++i)
            {
                char ch = chars[i];

                if (ch == '\"') // found string span
                {
                    bool str = true;
                    while (str)
                    {
                        output.Append(ch);
                        ch = chars[++i];
                        if (ch == '\\')
                        {
                            output.Append(ch);
                            ch = chars[++i];
                        }
                        else if (ch == '\"')
                            str = false;
                    }
                }

                switch (ch)
                {
                    case '{':
                    case '[':
                        output.Append(ch);
                        output.AppendLine();
                        AppendIndent(output, ++depth);
                        break;
                    case '}':
                    case ']':
                        output.AppendLine();
                        AppendIndent(output, --depth);
                        output.Append(ch);
                        break;
                    case ',':
                        output.Append(ch);
                        output.AppendLine();
                        AppendIndent(output, depth);
                        break;
                    case ':':
                        output.Append(" : ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(ch))
                            output.Append(ch);
                        break;
                }
            }

            return output.ToString();
        }

        public static String PrintXML(String XML)
        {

            XmlDocument document = new XmlDocument();

            try
            {
                document.LoadXml(XML);
                using (MemoryStream mStream = new MemoryStream())
                using (XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode))
                {
                    writer.Formatting = Formatting.Indented;

                    // Write the XML into a formatting XmlTextWriter
                    document.WriteContentTo(writer);
                    writer.Flush();
                    mStream.Flush();

                    // Have to rewind the MemoryStream in order to read
                    // its contents.
                    mStream.Position = 0;

                    // Read MemoryStream contents into a StreamReader.
                    StreamReader sReader = new StreamReader(mStream);

                    // Extract the text from the StreamReader.
                    String FormattedXML = sReader.ReadToEnd();

                    return FormattedXML;
                }
            }
            catch (Exception)
            {
                return XML;
            }

        }

    #endregion

    #region multipart

        public static string CLeanQueryString(string qs)
        {
            return qs.Replace("&amp;", "&");
        }

        public static T ConvertTo<T>(object val, T defaultValue = default(T))
        {

            if (val == null)
                return defaultValue;
            int intval;

            if (typeof(T) == typeof(int) && int.TryParse(val.ToString(), out intval))
            {
                return (T)Convert.ChangeType(intval, typeof(T));
            }
            long longval;
            if (typeof(T) == typeof(long) && long.TryParse(val.ToString(), out longval))
            {
                return (T)Convert.ChangeType(longval, typeof(T));
            }
            float floatal;
            if (typeof(T) == typeof(float) && float.TryParse(val.ToString(), out floatal))
            {
                return (T)Convert.ChangeType(floatal, typeof(T));
            }
            bool booltal;
            if (typeof(T) == typeof(bool) && bool.TryParse(val.ToString(), out booltal))
            {
                return (T)Convert.ChangeType(booltal, typeof(T));
            }
            double doubleltal;
            if (typeof(T) == typeof(double) && double.TryParse(val.ToString(), out doubleltal))
            {
                return (T)Convert.ChangeType(doubleltal, typeof(T));
            }

            return defaultValue;

        }

        public static Dictionary<string, object> QueryStringToDictionary(string qs, bool enableRegexMatch)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            if (qs == null)
                return null;

            string str = CLeanQueryString(qs);

            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            if (!str.Contains('='))
            {
                return null;
            }

            foreach (string arg in str.Split(new char[] { '&' }))
            {
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                string[] strArray = arg.Split(new char[] { '=' });
                if (strArray.Length == 2)
                {
                    string key = strArray[0];// Regex.Replace("amp;", strArray[0], "");
                    string type = "string";
                    object val = strArray[1];
                    string[] strType = key.Split(new char[] { ':' });
                    if (strType.Length == 2)
                    {
                        key = strType[0];
                        type = strType[1];
                    }
                    else if (enableRegexMatch)
                    {
                        //if(Types.IsNumber(strArray[1]))
                        //if (Regex.IsMatch(strArray[1], @"^(-|)([0-9]\.|[1-9])+[0-9]+$"))
                        //{
                        //    type = "number";
                        //}

                        if (Regex.IsMatch(strArray[1], @"^(-|)[0-9]\.+[0-9]+$"))
                        {
                            type = "double";
                        }
                        //else if (Regex.IsMatch(strArray[1], @"^(-|)[1-9]+$"))
                        //{
                        //    type = "int";
                        //}
                        else if (Regex.IsMatch(strArray[1], @"^(-|)[1-9]+[0-9]+$"))
                        {
                            type = "long";
                        }
                        else if (Regex.IsMatch(strArray[1], @"^(false|true)$", RegexOptions.IgnoreCase))
                        {
                            type = "bool";
                        }

                    }

                    switch (type)
                    {
                        case "int":
                            val = ConvertTo<int>(val);
                            break;
                        case "long":
                            val = ConvertTo<long>(val);
                            break;
                        case "float":
                            val = ConvertTo<float>(val, 0);
                            break;
                        case "double":
                            val = ConvertTo<double>(val, 0);
                            break;
                        case "bool":
                            val = ConvertTo<bool>(val, false);
                            break;
                        case "string":
                        default:

                            break;
                    }
                    dictionary.Add(key, val);
                }

            }

            return dictionary;
        }


        public static class MultipartRequest
        {
            private static readonly Encoding encoding = Encoding.UTF8;
            public static string MultipartFormDataPost(string postUrl, Dictionary<string, object> postParameters)
            {
                Stream receiveStream = null;
                StreamReader readStream = null;

                try
                {
                    string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                    string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                    byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

                    var wresponse = PostForm(postUrl, contentType, formData);

                    Encoding enc = Encoding.GetEncoding("utf-8");

                    receiveStream = wresponse.GetResponseStream();
                    readStream = new StreamReader(receiveStream, enc);
                    var response = readStream.ReadToEnd();

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
                    if (receiveStream != null)
                        receiveStream.Close();
                    if (readStream != null)
                        readStream.Close();
                }

            }
            private static HttpWebResponse PostForm(string postUrl, string contentType, byte[] formData)
            {
                HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

                if (request == null)
                {
                    throw new NullReferenceException("request is not a http request");
                }

                // Set up the request properties.
                request.Method = "POST";
                request.ContentType = contentType;
                //request.UserAgent = userAgent;
                request.CookieContainer = new CookieContainer();
                request.ContentLength = formData.Length;

                // You could add authentication here as well if needed:
                // request.PreAuthenticate = true;
                // request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
                // request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("username" + ":" + "password")));

                // Send the form data to the request.
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(formData, 0, formData.Length);
                    requestStream.Close();
                }

                return request.GetResponse() as HttpWebResponse;
            }

            private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
            {
                Stream formDataStream = new System.IO.MemoryStream();
                bool needsCLRF = false;

                foreach (var param in postParameters)
                {
                    // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                    // Skip it on the first parameter, add it to subsequent parameters.
                    if (needsCLRF)
                        formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                    needsCLRF = true;

                    if (param.Value is FileParameter)
                    {
                        FileParameter fileToUpload = (FileParameter)param.Value;

                        // Add just the first part of this param, since we will write the file data directly to the Stream
                        string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                            boundary,
                            param.Key,
                            fileToUpload.FileName ?? param.Key,
                            fileToUpload.ContentType ?? "application/octet-stream");

                        formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                        // Write the file data directly to the Stream, rather than serializing it to a string.
                        formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                    }
                    else
                    {
                        string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                            boundary,
                            param.Key,
                            param.Value);
                        formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                    }
                }

                // Add the end of the request.  Start with a newline
                string footer = "\r\n--" + boundary + "--\r\n";
                formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

                // Dump the Stream into a byte[]
                formDataStream.Position = 0;
                byte[] formData = new byte[formDataStream.Length];
                formDataStream.Read(formData, 0, formData.Length);
                formDataStream.Close();

                return formData;
            }

            public class FileParameter
            {
                public byte[] File { get; set; }
                public string FileName { get; set; }
                public string ContentType { get; set; }
                public FileParameter(byte[] file) : this(file, null) { }
                public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
                public FileParameter(byte[] file, string filename, string contenttype)
                {
                    File = file;
                    FileName = filename;
                    ContentType = contenttype;
                }
            }
        }
    #endregion

    }

#endif
}
