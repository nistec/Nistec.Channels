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
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;
using Nistec.Generic;
using Nistec.Runtime;
using Nistec.Serialization;

namespace Nistec.Channels.RemoteCache
{
 
    /// <summary>
    /// Represent the cache api settings as read only.
    /// </summary>
    public class RemoteCacheSettings
    {

        #region Defaults

        /// <summary>
        /// Get Default Formatter
        /// </summary>
        public static Formatters DefaultFormatter { get { return Formatters.BinarySerializer; } }
        public const int DefaultReceiveBufferSize = 4096;
        public const int DefaultSendBufferSize = 4096;

        public const int DefaultConnectTimeout = 5000;
        public const int DefaultProcessTimeout = 5000;
        public const int DefaultSessionTimeout = 30;
        public const int DefaultCacheExpiration = 0;
        public const int DefaultTaskTimeout = 240;

        public const bool DefaultIsAsync = false;
        public const bool DefaultEnableException = false;

        public static NetProtocol DefaultProtocol = NetProtocol.Tcp;

        #endregion

        #region pipe
        /// <summary>
        /// Default Cache HostName
        /// </summary>
        public const string DefaultHostName = "nistec_cache_bundle";
        /// <summary>
        /// Default Data HostName
        /// </summary>
        public const string DefaultPipeName = "nistec_cache_bundle";

        #endregion

        #region TCP

        /// <summary>
        /// Default Sync Address
        /// </summary>
        public const string DefaultTcpAddress = "localhost";
        /// <summary>
        /// DefaultCachePort
        /// </summary>
        public const int DefaultTcpPort = 13001;

        #endregion

        #region Http

        public const string DefaultHttpAddress = "localhost";
        public const int DefaultHttpPort = 13010;
        public const string DefaultHttpMethod = "post";
        public const int DefaultHttpSslPort = 13043;
        #endregion

        #region Properties
        public static string PipeName { get; set; }

        public static int TcpPort { get; set; }
        public static string TcpAddress { get; set; }

        public static int HttpPort { get; set; }
        public static int HttpSslPort { get; set; }
        public static string HttpAddress { get; set; }
        public static string HttpMethod { get; set; }
       
        public static int GetHttpPort()
        {
            if (IsHttpSsl())
                return HttpSslPort;
            else
                return HttpPort;
        }
        public static bool IsHttpSsl()
        {
            if (HttpAddress == null)
                return false;
            return (HttpAddress.ToLower().StartsWith("https://"));
        }

        public static int ConnectTimeout { get; set; }
        public static int ProcessTimeout { get; set; }
        public static int ReceiveBufferSize { get; set; }
        public static int SendBufferSize { get; set; }


        public static int SessionTimeout { get; set; }
        public static int CacheExpiration { get; set; }
        public static bool IsRemoteAsync { get; set; }
        public static bool EnableRemoteException { get; set; }
        public static NetProtocol Protocol { get; set; }
        #endregion

        #region Helper

        internal static string GetHostAddress(string hostAddress, NetProtocol protocol)
        {
            if (hostAddress != null)
                return hostAddress;
            if (protocol == NetProtocol.Http)
                return HttpAddress;
            if (protocol == NetProtocol.Tcp)
                return TcpAddress;
            if (protocol == NetProtocol.Pipe)
                return PipeName;

            return null;
        }

        internal static int GetPort(int port, NetProtocol protocol)
        {
            if (port > 0)
                return port;
            if (protocol == NetProtocol.Http)
                return (IsHttpSsl()) ? DefaultHttpSslPort : DefaultHttpPort;
            if (protocol == NetProtocol.Tcp)
                return DefaultTcpPort;
            return 0;
        }

        internal static string GetHttpMethod(string httpMethod, NetProtocol protocol)
        {
            if (httpMethod != null)
                return httpMethod;
            if (protocol == NetProtocol.Http)
                return HttpMethod;
            return null;

        }

        #endregion

        #region CacheClientSettings

        static NameValueCollection ApiConfig;

         public static NameValueCollection GetApiConfig()
        {
            if (ApiConfig == null)
                ApiConfig = ConfigurationManager.GetSection("RemoteCacheApi") as NameValueCollection;
            return ApiConfig;
        }


        static RemoteCacheSettings()
        {
            try
            {
                System.Configuration.Configuration config = NetConfig.GetConfiguration();

                XmlDocument doc = new XmlDocument();
                doc.Load(config.FilePath);
                string xpath = ".//RemoteCacheApi/ApiSettings";
                XmlNode root = doc.SelectSingleNode(xpath);
                LoadItemSettings(root);
            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
        }

        static void LoadItemSettings(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("CacheApiSettings.XmlNode node");
            }
            XmlTable table = XmlTable.LoadXmlKeyValue(node, "key", "value");

            IsRemoteAsync = table.Get<bool>("IsRemoteAsync", DefaultIsAsync);
            EnableRemoteException = table.Get<bool>("EnableRemoteException", DefaultEnableException);

            Protocol = table.GetEnum<NetProtocol>("Protocol", DefaultProtocol);

            PipeName = table.Get<string>("PipeName", DefaultPipeName);
            TcpPort = table.Get<int>("TcpPort", DefaultTcpPort);
            TcpAddress = table.Get<string>("TcpAddress", DefaultTcpAddress);
            HttpPort = table.Get<int>("HttpPort", DefaultHttpPort);
            HttpSslPort = table.Get<int>("HttpSslPort", DefaultHttpSslPort);
            HttpMethod = table.Get<string>("HttpMethod", DefaultHttpMethod);
            ConnectTimeout = table.Get<int>("ConnectTimeout", DefaultConnectTimeout);
            ProcessTimeout = table.Get<int>("ProcessTimeout", DefaultProcessTimeout);
            ReceiveBufferSize = table.Get<int>("ReceiveBufferSize", DefaultReceiveBufferSize);
            SendBufferSize = table.Get<int>("SendBufferSize", DefaultSendBufferSize);
            SessionTimeout = table.Get<int>("SessionTimeout", DefaultSessionTimeout);
            CacheExpiration = table.Get<int>("CacheExpiration", DefaultCacheExpiration);
        }

        #endregion

    }
   
}
