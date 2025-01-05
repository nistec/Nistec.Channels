using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
namespace Nistec.Channels.http
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class Requester
    {
        const int TimeoutSeconds = 20;

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

        static void SetHeaders(HttpClient httpClient, Dictionary<string, string> hd)
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
 
            using (HttpClient httpClient = new HttpClient())
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

            //using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) 
            //using (var request = new HttpRequestMessage(method, address))
            //using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            //using (var response = httpClient.SendAsync(request))
            //{

            //    return response.Result.Content.ReadAsStringAsync();
            //}
        }

        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data)
        {
            using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) })
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = httpClient.SendAsync(request))
            {

                return response.Result.Content.ReadAsStringAsync();
            }
        }

        static Task<string> DoHttpRequest(string address, HttpMethod method, string contentType, string data, CancellationToken ctsTocken)
        {
            using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) })
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = httpClient.SendAsync(request, ctsTocken))
            {

                return response.Result.Content.ReadAsStringAsync();
            }
        }

        static async Task<string> DoHttpRequestAsync(string address, HttpMethod method, string contentType, string data)
        {
            using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) })
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds)))
            using (var request = new HttpRequestMessage(method, address))
            using (request.Content = new StringContent(data, Encoding.UTF8, contentType))
            using (var response = await httpClient.SendAsync(request, cts.Token))
            {

                return await response.Content.ReadAsStringAsync();
            }
        }

        static async Task<string> DoJsonPostRequest(string address, string data)
        {
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(20) })
            using (var content = new StringContent(data, Encoding.UTF8))
            using (HttpResponseMessage response = await client.PostAsync(address, content))
            using (HttpContent result = response.Content)
            {
                return await result.ReadAsStringAsync();
            }

        }

        static async Task<string> DoJsonGetRequest(string address)
        {
            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(20) })
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


        static string DoRequest(string url, string action, string method, string contentType, string postArgs)
        {

            string response = null;

            WebRequest request = null;
            Stream newStream = null;
            Stream receiveStream = null;
            StreamReader readStream = null;
            WebResponse wresponse = null;
            string encoding = "utf-8";
            int timeout = (int)TimeSpan.FromSeconds(TimeoutSeconds).TotalMilliseconds;

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
}
