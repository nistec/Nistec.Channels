using Nistec.Channels.Http;
using Nistec.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nistec.Channels.Http
{

    public class HttpHeader //: IEntityItem
    {

        public HttpHeader(string userName, string userPass, AuthScheme scheme)
        {
            //UserName = userName;
            //UserPass = userPass;
            //AuthScheme = scheme;
            AuthScheme = scheme;
            Token = HttpHeader.CreateAuthToken(userName, userPass);
            Headers.Add("Authorization", CreateAuthToken(userName, userPass, scheme));
        }
        public HttpHeader(string userName, string userPass, AuthScheme scheme, int accountId)
        {
            //UserName = userName;
            //UserPass = userPass;
            //AuthScheme = scheme;
            AuthScheme = scheme;
            Token = HttpHeader.CreateAuthToken(userName, userPass);
            Headers.Add("Authorization", CreateAuthToken(userName, userPass, scheme));
            AccountId = accountId;
        }

        public AuthScheme AuthScheme { get; protected set; }
        public int AccountId { get; protected set; }
        public string Token { get; protected set; }

        public void Add(string key, string value)
        {
            Headers.Add(key, value);
        }

        public AuthScheme ParseAuthScheme(string authScheme)
        {
            if (authScheme == null)
                return AuthScheme.None;

            switch (authScheme.ToLower())
            {
                case "basic":
                    return AuthScheme.Basic;
                case "bearer":
                    return AuthScheme.Bearer;
                case "co":
                    return AuthScheme.Co;
                case "none":
                default:
                    return AuthScheme.None;
            }
        }

        public AuthScheme GetAuthScheme()
        {
            string token = GetAuthSchemeToken();
            if (string.IsNullOrEmpty(token))
            {
                return  AuthScheme.None;
            }
            var args = token.SplitTrim(" ");
            if (args.Length > 1)
                return ParseAuthScheme(args[0]);
            else
                return AuthScheme.None;
        }

        public string GetAuthToken()
        {
            string token = GetAuthSchemeToken();
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            var args = token.SplitTrim(" ");
            if (args.Length > 1)
                return args[1];
            else
                return args[0];
        }

        public string GetAuthSchemeToken()
        {
            if (Headers == null || Headers.Count == 0)
            {
                return null;
            }
            string token = Headers.Get("Authorization");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            return token;
        }

        public string GetAuthString()
        {
            if (Headers == null || Headers.Count == 0)
            {
                return null;
            }
            string token = Headers.Get("Authorization");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            return $"Authorization: {token}";
        }

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
        public static string CreateAuthHeaderString(string userName, string password, AuthScheme scheme)
        {
            var token = CreateAuthToken(userName, password, scheme);
            return $"Authorization: {token}";
        }

        public static string CreateAuthToken(string userName, string password)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }
        public static string CreateAuthToken(string userName, string password, AuthScheme scheme)
        {
            return scheme.ToString() + " " + CreateAuthToken(userName, password);
        }
        public static string CreateAuthToken(string token, AuthScheme scheme)
        {
            return scheme.ToString() + " " + token;
        }

        public void CreateHeader(System.Net.Http.HttpClient httpClient)
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
        //public AuthScheme AuthScheme { get; set; }
        ////public int AccountId { get; set; }
        //public string UserName { get; set; }
        //public string UserPass { get; set; }

    }
}
