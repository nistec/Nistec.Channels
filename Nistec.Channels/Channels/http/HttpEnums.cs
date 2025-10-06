using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CS1591

namespace Nistec.Channels.Http
{
    public enum RequestType
    {
        Json,
        Xml,
        Soap,
        Form,
        Multipart
    }
    public enum MethodType
    {
        GET,
        POST,
    }
    public enum AuthScheme
    {
        None,
        Basic,
        Bearer,
        Co
    }
    public enum AuthMetods
    {
        Authorization,
        Authentication
    }

    public static class HttpExtension
    {

        public static AuthScheme Get(this AuthScheme auth, string scheme)
        {
            return ToAuthScheme(scheme);
        }

        public static AuthScheme ToAuthScheme(string scheme)
        {
            switch (scheme.ToLower())
            {
                case "basic":
                    return AuthScheme.Basic;
                case "bBearer":
                    return AuthScheme.Bearer;
                default:
                    return AuthScheme.None;
            }
        }

        public static MethodType ToMethodType(string method)
        {
            switch (method.ToUpper())
            {
                case "GET":
                    return MethodType.GET;
                case "POST":
                default:
                    return MethodType.POST;
            }
        }
        public static RequestType ToRequestType(string requestType)
        {
            switch (requestType.ToLower())
            {
                case "json":
                case "application/json":
                    return RequestType.Json;
                case "xml":
                case "text/xml":
                    return RequestType.Xml;
                case "soap":
                    return RequestType.Soap;
                case "form":
                case "application/x-www-form-urlencoded":
                    return RequestType.Form;
                default:
                    return RequestType.Json;
            }
        }


        public static MethodType Get(this MethodType type, string method)
        {
            return ToMethodType(method);
         }

        public static RequestType Get(this RequestType type, string requestType)
        {
            return ToRequestType(requestType);
        }

    }
}
