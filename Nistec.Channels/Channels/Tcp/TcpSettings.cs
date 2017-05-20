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

namespace Nistec.Channels.Tcp
{

    public class TcpClientSettings
    {
        static readonly ConcurrentDictionary<string, TcpSettings> TcpClientSettingsCache = new ConcurrentDictionary<string, TcpSettings>();
        
        public static TcpSettings GetTcpClientSettings(string hostName)
        {
            TcpSettings settings = null;
            if (TcpClientSettingsCache.TryGetValue(hostName, out settings))
            {
                return settings;
            }
            settings = new TcpSettings(hostName, false);
            if (settings == null)
            {
                throw new Exception("Invalid configuration for tcp client settings with host name:" + hostName);
            }
            TcpClientSettingsCache[hostName] = settings;
            return settings;
        }
    }

    /// <summary>
    ///Tcp Settings.
    /// </summary>
    /// <example>
    /// tcpClientSettings
    /// <TcpClientSettings>
    ///     <host HostName="localhost" 
    ///     Address="127.0.0.1"
    ///     Port="13000"
    ///     IsAsync="false" 
    ///     SendTimeout="5000" 
    ///     ReadTimeout="1000" 
    ///     ReceiveBufferSize="1024" 
    ///     SendBufferSize="1024"/>
    /// </tcpClientSettings>
    /// tcpServerSettings
    /// <TcpServerSettings>
    ///     <host HostName="localhost" 
    ///     Address="127.0.0.1"
    ///     Port="13000"
    ///     IsAsync="true"  
    ///     SendTimeout="5000" 
    ///     ProcessTimeout="5000" 
    ///     ReadTimeout="1000" 
    ///     ReceiveBufferSize="1024" 
    ///     SendBufferSize="1024" 
    ///     MaxSocketError="50" 
    ///     MaxServerConnections="0" 
    /// </pipeServerSettings>
    /// </example>
    public class TcpSettings
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
        /// DefaultPort
        /// </summary>
        public const int DefaultPort = 13000;
        /// <summary>
        /// DefaultReceiveBufferSize
        /// </summary>
        public const int DefaultReceiveBufferSize = 4096;
        /// <summary>
        /// DefaultSendBufferSize
        /// </summary>
        public const int DefaultSendBufferSize = 4096;
        /// <summary>
        /// DefaultSendTimeout
        /// </summary>
        public const int DefaultSendTimeout = 5000;
        /// <summary>
        /// DefaultProcessTimeout
        /// </summary>
        public const int DefaultProcessTimeout = 5000;
        /// <summary>
        /// DefaultReadTimeout
        /// </summary>
        public const int DefaultReadTimeout = 1000;
        /// <summary>
        /// DefaultMaxSocketError
        /// </summary>
        public const int DefaultMaxSocketError = 50;

        


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
        ///  Get or Set Indicates that the channel can be used for asynchronous reading and writing..
        /// </summary>
        public bool IsAsync { get; set; }
        /// <summary>
        /// Get or Set MaxServerConnections (Only for server side) (Default=1).
        /// </summary>
        public int MaxServerConnections { get; set; }
        /// <summary>
        /// Get or Set ProcessTimeout (Default=5000).
        /// </summary>
        public int ProcessTimeout { get; set; }
        /// <summary>
        /// Get or Set SendTimeout (Default=5000).
        /// </summary>
        public int SendTimeout { get; set; }
        /// <summary>
        /// Get or Set ReadTimeout (Default=5000).
        /// </summary>
        public int ReadTimeout { get; set; }
        /// <summary>
        /// Get or Set ReceiveBufferSize (Default=8192).
        /// </summary>
        public int ReceiveBufferSize { get; set; }
        /// <summary>
        /// Get or Set SendBufferSize (Default=8192).
        /// </summary>
        public int SendBufferSize { get; set; }
        /// <summary>
        /// Get or Set the max socket errors
        /// </summary>
        public int MaxSocketError { get; set; }
        
        /// <summary>
        /// Ensure Host Address
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static string EnsureHostAddress(string host)
        {
            return host == null || host == "" ? "Any" : host == "localhost" ? "127.0.0.1" : host;
        }



        /// <summary>
        /// Default constractor.
        /// </summary>
        public TcpSettings()
        {
            HostName = DefaultHostName;
            Address = DefaultAddress;
            Port=DefaultPort;
            IsAsync = true;
            SendTimeout = DefaultSendTimeout;
            ProcessTimeout = DefaultProcessTimeout;
            ReadTimeout = DefaultReadTimeout;
            ReceiveBufferSize = DefaultReceiveBufferSize;
            SendBufferSize = DefaultSendBufferSize;
            MaxServerConnections = 0;
            MaxSocketError = DefaultMaxSocketError;
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        public TcpSettings(string hostAddress, int port)
            : this()
        {
            HostName = hostAddress;
            Address = EnsureHostAddress(hostAddress);
            Port = port;
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="configHost"></param>
        /// <param name="isServer"></param>
        public TcpSettings(string configHost, bool isServer)
            : this()
        {
            HostName = configHost;
            LoadTcpSttingsInternal(configHost, isServer);
        }

        /// <summary>
        /// TcpSettings
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isServer"></param>
        public TcpSettings(XmlNode node, bool isServer)
        {
            LoadTcpSettings(node, isServer);
        }

        void LoadTcpSettings(XmlNode node, bool isServer)
        {
            if (node == null)
            {
                throw new ArgumentNullException("TcpSettings.XmlNode node");
            }

            XmlTable table = new XmlTable(node);
            HostName = table.GetValue("HostName");
            Address = EnsureHostAddress(table.GetValue("Address"));
            Port = table.Get<int>("Port");
            IsAsync = (bool)table.Get<bool>("IsAsync", true);
            SendTimeout = (int)table.Get<int>("SendTimeout", DefaultSendTimeout);
            ProcessTimeout = (int)table.Get<int>("ProcessTimeout", DefaultProcessTimeout);
            ReadTimeout = (int)table.Get<int>("ReadTimeout", DefaultReadTimeout);
            ReceiveBufferSize = table.Get<int>("ReceiveBufferSize", DefaultReceiveBufferSize);
            SendBufferSize = table.Get<int>("SendBufferSize", DefaultSendBufferSize);
            if (isServer)
            {
                MaxSocketError = table.Get<int>("MaxSocketError", DefaultMaxSocketError);
                MaxServerConnections = table.Get<int>("MaxServerConnections", 1);
            }
        }

        /// <summary>
        /// Load pipe server settings from appConfig using HostName attribute.
        /// </summary>
        /// <param name="configHost"></param>
        public void LoadTcpServerSttings(string configHost)
        {
            LoadTcpSttingsInternal(configHost, true);
        }
        /// <summary>
        /// Load pipe client settings from appConfig using HostName attribute.
        /// </summary>
        /// <param name="configHost"></param>
        public void LoadTcpClientSttings(string configHost)
        {
            LoadTcpSttingsInternal(configHost, false);
        }

        void LoadTcpSttingsInternal(string configHost, bool isServer)
        {
            if (string.IsNullOrEmpty(configHost))
            {
                throw new ArgumentNullException("TcpSettings.LoadTcpSttingsInternal name");
            }

            System.Configuration.Configuration config = NetConfig.GetConfiguration();

            XmlDocument doc = new XmlDocument();
            doc.Load(config.FilePath);

            Netlog.Debug("LoadTcpSttingsInternal : " + config.FilePath);

            string xpath = isServer ? "//TcpServerSettings" : "//TcpClientSettings";

            XmlNode root = doc.SelectSingleNode(xpath);
            XmlNode node = null;
            bool found = false;

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
                throw new ArgumentException("Invalid TcpSettings with HostName:" + configHost);
            }

            LoadTcpSettings(node, isServer);
        }
        /// <summary>
        /// Load Settings from config.
        /// </summary>
        /// <param name="isServer"></param>
        /// <returns></returns>
        public static TcpSettings[] LoadSettings(bool isServer)
        {
            List<TcpSettings> list = new List<TcpSettings>();
            try
            {
                System.Configuration.Configuration config = NetConfig.GetConfiguration();

                XmlDocument doc = new XmlDocument();
                doc.Load(config.FilePath);

                Netlog.Debug("LoadSettings : " + config.FilePath);

                string xpath = isServer ? "//TcpServerSettings" : "//TcpClientSettings";

                XmlNode root = doc.SelectSingleNode(xpath);

                foreach (XmlNode n in root.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Comment)
                        continue;

                    TcpSettings ps = new TcpSettings(n, isServer);
                    list.Add(ps);
                }
                return list.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// Get host adress as <see cref="IPAddress"/>.
        /// </summary>
        public IPAddress HostAddress 
        { 
            get 
            {
                string host=EnsureHostAddress(Address);

                return host == "Any" ? IPAddress.Any : IPAddress.Parse(host); 

            } 
        }
        /// <summary>
        /// Get endpoint using host adress and port.
        /// </summary>
        /// <returns></returns>
        public IPEndPoint GetEndpoint()
        {
            return new IPEndPoint(HostAddress, Port);
        }

        /// <summary>
        /// Get Server Endpoint Using Machine Name
        /// </summary>
        /// <param name="host"></param>
        /// <param name="portOnHost"></param>
        /// <returns></returns>
        public static IPEndPoint GetServerEndpointUsingMachineName(string host, Int32 portOnHost)
        {

            IPEndPoint hostEndPoint = null;
            try
            {
                IPHostEntry theIpHostEntry = Dns.GetHostEntry(host);
                // Address of the host.
                IPAddress[] serverAddressList = theIpHostEntry.AddressList;

                bool gotIpv4Address = false;
                AddressFamily addressFamily;
                Int32 count = -1;
                for (int i = 0; i < serverAddressList.Length; i++)
                {
                    count++;
                    addressFamily = serverAddressList[i].AddressFamily;
                    if (addressFamily == AddressFamily.InterNetwork)
                    {
                        gotIpv4Address = true;
                        i = serverAddressList.Length;
                    }
                }

                if (gotIpv4Address == false)
                {
                    Console.WriteLine("Could not resolve name to IPv4 address. Need IP address. Failure!");
                }
                else
                {
                    Console.WriteLine("Server name resolved to IPv4 address.");
                    // Instantiates the endpoint.
                    hostEndPoint = new IPEndPoint(serverAddressList[count], portOnHost);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine("Could not resolve server address.");
                Console.WriteLine("host = " + host);
            }

            return hostEndPoint;
        }
        /// <summary>
        /// Get Server Endpoint Using Ip Address
        /// </summary>
        /// <param name="host"></param>
        /// <param name="portOnHost"></param>
        /// <returns></returns>
        public static IPEndPoint GetServerEndpointUsingIpAddress(string host, Int32 portOnHost)
        {
            IPEndPoint hostEndPoint = null;
            try
            {
                IPAddress theIpAddress = IPAddress.Parse(host);
                // Instantiates the Endpoint.
                hostEndPoint = new IPEndPoint(theIpAddress, portOnHost);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (FormatException e)
            {
                Console.WriteLine("FormatException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            return hostEndPoint;
        }

    }
}
