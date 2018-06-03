﻿//licHeader
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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Collections.Specialized;
using System.Configuration;
using System.Xml;
using Nistec.Generic;
using System.Net;
using System.Net.Sockets;
using Nistec.Logging;

namespace Nistec.Channels.Http
{

    public class HttpClientSettings
    {
        static readonly ConcurrentDictionary<string, HttpSettings> HttpClientSettingsCache = new ConcurrentDictionary<string, HttpSettings>();
        
        public static HttpSettings GetHttpClientSettings(string hostName)
        {
            HttpSettings settings = null;
            if (HttpClientSettingsCache.TryGetValue(hostName, out settings))
            {
                return settings;
            }
            settings = new HttpSettings(hostName, false);
            if (settings == null)
            {
                throw new Exception("Invalid configuration for Http client settings with host name:" + hostName);
            }
            HttpClientSettingsCache[hostName] = settings;
            return settings;
        }
    }

    /// <summary>
    ///Http Settings.
    /// </summary>
    /// <example>
    /// HttpClientSettings
    /// <HttpClientSettings>
    ///     <host HostName="localhost" 
    ///     Address="127.0.0.1"
    ///     Port="13100"
    ///     Method="post"
    ///     ConnectTimeout="5000" 
    ///     ProcessTimeout="5000" 
    /// </HttpClientSettings>
    /// HttpServerSettings
    /// <HttpServerSettings>
    ///     <host HostName="localhost" 
    ///     Address="127.0.0.1"
    ///     Port="13100"
    ///     Method="post"
    ///     ConnectTimeout="5000" 
    ///     ProcessTimeout="5000" 
    ///     MaxErrors="50" 
    ///     MaxServerConnections="0" 
    /// </HttpServerSettings>
    /// </example>
    public class HttpSettings
    {

        /// <summary>
        /// DefaultHostName
        /// </summary>
        public const string DefaultHostName = "localhost";
        /// <summary>
        /// DefaultAddress
        /// </summary>
        public const string DefaultAddress = "127.0.0.1";
        /// <summary>
        /// DefaultMethod
        /// </summary>
        public const string DefaultMethod = "post";
        /// <summary>
        /// DefaultPort
        /// </summary>
        public const int DefaultPort = 13010;
        /// <summary>
        /// DefaultSslPort
        /// </summary>
        public const int DefaultSslPort = 13043;
        /// <summary>
        /// DefaultSendTimeout
        /// </summary>
        public const int DefaultConnectTimeout = 5000;
        /// <summary>
        /// DefaultProcessTimeout
        /// </summary>
        public const int DefaultProcessTimeout = 5000;
        ///// <summary>
        ///// DefaultReadTimeout
        ///// </summary>
        //public const int DefaultReadTimeout = 1000;
        /// <summary>
        /// DefaultMaxSocketError
        /// </summary>
        public const int DefaultMaxErrors = 50;


        /// <summary>
        ///  Get or Set HostName.
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        ///  Get or Set Host Address.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        ///  Get or Set Port.
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        ///  Get or Set SslPort.
        /// </summary>
        public int SslPort { get; set; }
        /// <summary>
        ///  Get or Set request method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///  Get or Set if ssl is enabled.
        /// </summary>
        public bool SslEnabled { get; set; }

        /// <summary>
        /// Get or Set MaxServerConnections (Only for server side) (Default=1).
        /// </summary>
        public int MaxServerConnections { get; set; }
        /// <summary>
        /// Get or Set ProcessTimeout (Default=5000).
        /// </summary>
        public int ProcessTimeout { get; set; }
        /// <summary>
        /// Get or Set ConnectTimeout (Default=5000).
        /// </summary>
        public int ConnectTimeout { get; set; }
        ///// <summary>
        ///// Get or Set ReadTimeout (Default=5000).
        ///// </summary>
        //public int ReadTimeout { get; set; }

        /// <summary>
        /// Get or Set the max errors befor service shut down.
        /// </summary>
        public int MaxErrors { get; set; }
        /// <summary>
        /// Get or Set the max workers of http listener.
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// Ensure Hot Address
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static string EnsureHostAddress(string host)
        {
            return host == null || host == "" ? "Any" : host == "localhost" ? "127.0.0.1" : host;
        }

        /// <summary>
        ///  Get or Set Full Host Address like "site.com:13010".
        /// </summary>
        public string HostAddress { get; private set; }

        /// <summary>
        ///  Get or Set Host Address ssl.
        /// </summary>
        public string SslHostAddress { get; private set; }

        /// <summary>
        /// Get host adress.
        /// </summary>
        public string GetHostAddress()
        {
            if (Port <= 0)
                Port = DefaultPort;
                return Address.TrimEnd('/') + ":" + Port.ToString() + "/";
        }

        public static void SplitHostAddress(string hostAddress,out string address, out int port)
        {
            if(hostAddress==null)
            {
                address = null;
                port = 0;
            }
            string[] args= hostAddress.SplitTrim(':');

            if (args.Length == 1)
            {
                address = args[0];
                port = 0;
            }
            else if (args.Length ==2)
            {
                address = args[0];
                port = Types.ToInt(args[1]);
            }
            else
            {
                address = null;
                port = 0;
            }
        }
        /// <summary>
        /// Get host adress ssl.
        /// </summary>
        public string GetSslHostAddress()
        {
            //if (SslEnabled == false)
            //    return null;
            if (SslPort <= 0)
                Port = DefaultSslPort;
            return Address.TrimEnd('/') + ":" + SslPort.ToString() + "/";
        }

        public bool IsValidHostAddress()
        {
            if (HostAddress == null || HostAddress.Length == 0 || !HostAddress.ToLower().StartsWith("http://"))
            {
                return false;
            }
            return true;
        }

        public bool IsValidSslHostAddress()
        {
            if (!SslEnabled)
                return false;
            if (HostAddress == null || HostAddress.Length == 0 || !HostAddress.ToLower().StartsWith("https://"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Default constractor.
        /// </summary>
        public HttpSettings()
        {
            HostName = DefaultHostName;
            Address = DefaultAddress;
            Port = DefaultPort;
            SslPort = DefaultSslPort;
            SslEnabled = false;
            Method = DefaultMethod;
            ConnectTimeout = DefaultConnectTimeout;
            ProcessTimeout = DefaultProcessTimeout;
            //ReadTimeout = DefaultReadTimeout;
            MaxServerConnections = 0;
            MaxErrors = DefaultMaxErrors;
            HostAddress = GetHostAddress();
            SslHostAddress = GetSslHostAddress();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="method"></param>
        public HttpSettings(string hostAddress,int port, string method)
            : this()
        {
            HostName = hostAddress;
            Address = hostAddress;
            Port = port;
            SslPort = DefaultSslPort;
            SslEnabled = false;
            Method = method;
            HostAddress = GetHostAddress();
            SslHostAddress = GetSslHostAddress();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="configHost"></param>
        /// <param name="isServer"></param>
        public HttpSettings(string configHost, bool isServer)
            : this()
        {
            HostName = configHost;
            LoadHttpSttingsInternal(configHost, isServer);
        }

        /// <summary>
        /// HttpSettings
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isServer"></param>
        public HttpSettings(XmlNode node, bool isServer)
        {
            LoadHttpSettings(node, isServer);
        }

        void LoadHttpSettings(XmlNode node, bool isServer)
        {
            if (node == null)
            {
                throw new ArgumentNullException("HttpSettings.XmlNode node");
            }

            XmlTable table = new XmlTable(node);
            HostName = table.GetValue("HostName");
            Address = table.GetValue("Address");
            Port = table.Get<int>("Port");
            SslPort = table.Get<int>("SslPort");
            SslEnabled = table.Get<bool>("SslEnabled",false);
            Method = table.GetValue("Method");
            ConnectTimeout = (int)table.Get<int>("ConnectTimeout", DefaultConnectTimeout);
            ProcessTimeout = (int)table.Get<int>("ProcessTimeout", DefaultProcessTimeout);
            //ReadTimeout = (int)table.Get<int>("ReadTimeout", DefaultReadTimeout);
            if (isServer)
            {
                MaxErrors = table.Get<int>("MaxErrors", DefaultMaxErrors);
                MaxServerConnections = table.Get<int>("MaxServerConnections", 1);
            }
            HostAddress = GetHostAddress();
            SslHostAddress = GetSslHostAddress();
        }

        /// <summary>
        /// Load pipe server settings from appConfig using HostName attribute.
        /// </summary>
        /// <param name="configHost"></param>
        public void LoadHttpServerSttings(string configHost)
        {
            LoadHttpSttingsInternal(configHost, true);
        }
        /// <summary>
        /// Load pipe client settings from appConfig using HostName attribute.
        /// </summary>
        /// <param name="configHost"></param>
        public void LoadHttpClientSttings(string configHost)
        {
            LoadHttpSttingsInternal(configHost, false);
        }

        void LoadHttpSttingsInternal(string configHost, bool isServer)
        {
            if (string.IsNullOrEmpty(configHost))
            {
                throw new ArgumentNullException("HttpSettings.LoadHttpSttingsInternal name");
            }

            System.Configuration.Configuration config = NetConfig.GetConfiguration();

            XmlDocument doc = new XmlDocument();
            doc.Load(config.FilePath);

            Netlog.Debug("LoadHttpSttingsInternal : " + config.FilePath);

            string xpath = isServer ? "//HttpServerSettings" : "//HttpClientSettings";

            XmlNode root = doc.SelectSingleNode(xpath);
            XmlNode node = null;
            bool found = false;

            if(root==null)
            {
                throw new ArgumentException("Invalid "+ xpath);
            }

            foreach (XmlNode n in root.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment)
                    continue;

                XmlAttribute attr = n.Attributes["HostName"];
                if (attr != null && attr.Value == configHost)
                {
                    node = n;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new ArgumentException("Invalid HttpSettings with HostName:" + configHost);
            }

            LoadHttpSettings(node, isServer);
        }
        /// <summary>
        /// Load Settings from config.
        /// </summary>
        /// <param name="isServer"></param>
        /// <returns></returns>
        public static HttpSettings[] LoadSettings(bool isServer)
        {
            List<HttpSettings> list = new List<HttpSettings>();
            try
            {
                System.Configuration.Configuration config = NetConfig.GetConfiguration();

                XmlDocument doc = new XmlDocument();
                doc.Load(config.FilePath);

                Netlog.Debug("LoadSettings : " + config.FilePath);

                string xpath = isServer ? "//HttpServerSettings" : "//HttpClientSettings";

                XmlNode root = doc.SelectSingleNode(xpath);

                foreach (XmlNode n in root.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Comment)
                        continue;

                    HttpSettings ps = new HttpSettings(n, isServer);
                    list.Add(ps);
                }
                return list.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
