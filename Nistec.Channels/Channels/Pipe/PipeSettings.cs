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
using Nistec.Logging;

namespace Nistec.Channels
{

    public class PipeClientSettings
    {
        static readonly ConcurrentDictionary<string, PipeSettings> ClientSettingsCache = new ConcurrentDictionary<string, PipeSettings>();

        public static PipeSettings GetPipeClientSettings(string hostName)
        {
            PipeSettings settings = null;
            if (ClientSettingsCache.TryGetValue(hostName, out settings))
            {
                return settings;
            }
            settings = new PipeSettings(hostName,false,true);
            if (settings == null)
            {
                throw new Exception("Invalid configuration for pipe client settings with host name:" + hostName);
            }
            ClientSettingsCache[hostName] = settings;
            return settings;
        }
    }

    /// <summary>
    ///Pipe Settings.
    /// </summary>
    /// <example>
    /// pipeClientSettings
    /// <pipeClientSettings>
    ///     <pipe PipeName="myPipe" 
    ///     PipeDirection="In|Out|InOut" 
    ///     PipeOptions="None|WriteThrough|Asynchronous" 
    ///     VerifyPipe="myPipe" 
    ///     ConnectTimeout="5000" 
    ///     InBufferSize="1024" 
    ///     OutBufferSize="1024"/>
    /// </pipeClientSettings>
    /// pipeServerSettings
    /// <pipeServerSettings>
    ///     <pipe PipeName="myPipe" 
    ///     PipeDirection="In|Out|InOut" 
    ///     PipeOptions="None|WriteThrough|Asynchronous" 
    ///     VerifyPipe="myPipe" 
    ///     ConnectTimeout="5000" 
    ///     InBufferSize="1024" 
    ///     OutBufferSize="1024" 
    ///     MaxServerConnections="5" 
    ///     MaxAllowedServerInstances="255"/>
    /// </pipeServerSettings>
    /// </example>
    public class PipeSettings
    {

        /// <summary>
        /// Unlimited server pipe instances.
        /// </summary>
        public const int PIPE_UNLIMITED_INSTANCES = 255;
        /// <summary>
        /// DefaultInBufferSize
        /// </summary>
        public const int DefaultInBufferSize = 8192;
        /// <summary>
        /// DefaultOutBufferSize
        /// </summary>
        public const int DefaultOutBufferSize = 8192;
        /// <summary>
        /// DefaultConnectTimeout
        /// </summary>
        public const int DefaultConnectTimeout = 5000;
        /// <summary>
        ///  Get or Set PipeName.
        /// </summary>
        public string PipeName { get; set; }
        /// <summary>
        /// Get or Set PipeDirection (Default=InOut).
        /// </summary>
        public PipeDirection PipeDirection { get; set; }
        /// <summary>
        /// Get or Set PipeOptions (Default=None).
        /// </summary>
        public PipeOptions PipeOptions { get; set; }
        /// <summary>
        /// Get or Set MaxServerConnections (Only for server side) (Default=1).
        /// </summary>
        public int MaxServerConnections { get; set; }
        /// <summary>
        /// Get or Set MaxAllowedServerInstances (Only for server side) (Default=255).
        /// </summary>
        public int MaxAllowedServerInstances { get; set; }
        /// <summary>
        /// Get or Set VerifyPipe.
        /// </summary>
        public string VerifyPipe { get; set; }
        /// <summary>
        /// Get or Set ConnectTimeout (Default=5000).
        /// </summary>
        public uint ConnectTimeout { get; set; }
        /// <summary>
        /// Get or Set InBufferSize (Default=8192).
        /// </summary>
        public int InBufferSize { get; set; }
        /// <summary>
        /// Get or Set OutBufferSize (Default=8192).
        /// </summary>
        public int OutBufferSize { get; set; }
        /// <summary>
        /// ServerName constant.
        /// </summary>
        public const string ServerName = ".";
        /// <summary>
        /// Get Full Pipe Name.
        /// </summary>
        public string FullPipeName { get { return @"\\" + ServerName + @"\pipe\" + PipeName; } }


        /// <summary>
        /// Default constractor.
        /// </summary>
        public PipeSettings()
        {
            PipeDirection = System.IO.Pipes.PipeDirection.InOut;
            PipeOptions = System.IO.Pipes.PipeOptions.None;
            MaxServerConnections = 1;
            MaxAllowedServerInstances = 255;
            ConnectTimeout = 5000;
            InBufferSize = DefaultInBufferSize;
            OutBufferSize = DefaultOutBufferSize;
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isServer"></param>
        /// <param name="loadFromSettings"></param>
        public PipeSettings(string name, bool isServer, bool loadFromSettings)
            : this()
        {
            PipeName = name;
            VerifyPipe = name;
            if (loadFromSettings)
            {
                LoadPipeSttingsInternal(name, isServer);
            }
        }
        /// <summary>
        /// PipeSettings
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isServer"></param>
        public PipeSettings(XmlNode node, bool isServer)
        {
            LoadPipeSettings(node, isServer);
        }

        void LoadPipeSettings(XmlNode node, bool isServer)
        {
            if (node == null)
            {
                throw new ArgumentNullException("PipeSettings.XmlNode node");
            }
                       

            XmlTable table = new XmlTable(node);

           
            PipeName = table.GetValue("PipeName");
            PipeDirection = EnumExtension.Parse<PipeDirection>(table.Get<string>("PipeDirection"), PipeDirection.InOut);
            PipeOptions = EnumExtension.Parse<PipeOptions>(table.Get<string>("PipeOptions"), PipeOptions.None);
            VerifyPipe = table.Get<string>("VerifyPipe", PipeName);
            ConnectTimeout = (uint)table.Get<int>("ConnectTimeout", 5000);
            InBufferSize = table.Get<int>("InBufferSize", DefaultInBufferSize);
            OutBufferSize = table.Get<int>("OutBufferSize", DefaultOutBufferSize);
            if (isServer)
            {
                MaxServerConnections = table.Get<int>("MaxServerConnections", 1);
                MaxAllowedServerInstances = table.Get<int>("MaxAllowedServerInstances", NamedPipeServerStream.MaxAllowedServerInstances);
            }

        }

        /// <summary>
        /// Load pipe server settings from appConfig using PipeName attribute.
        /// </summary>
        /// <param name="name"></param>
        public void LoadPipeServerSttings(string name)
        {
            LoadPipeSttingsInternal(name, true);
        }
        /// <summary>
        /// Load pipe client settings from appConfig using PipeName attribute.
        /// </summary>
        /// <param name="name"></param>
        public void LoadPipeClientSttings(string name)
        {
            LoadPipeSttingsInternal(name, false);
        }

        void LoadPipeSttingsInternal(string name, bool isServer)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("PipeSettings.LoadPipeSttingsInternal name");
            }

            System.Configuration.Configuration config = NetConfig.GetConfiguration();

            XmlDocument doc = new XmlDocument();
            doc.Load(config.FilePath);

            Netlog.Debug("LoadPipeSttingsInternal : " + config.FilePath);

            string xpath = isServer ? "//PipeServerSettings" : "//PipeClientSettings";

            XmlNode root = doc.SelectSingleNode(xpath);
            XmlNode node = null;
            bool found = false;

            foreach (XmlNode n in root.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment)
                    continue;

                XmlAttribute attr = n.Attributes["PipeName"];
                if (attr != null && attr.Value == name)
                {
                    node = n;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new ArgumentException("Invalid PipeSettings with PipeName:" + name);
            }

            LoadPipeSettings(node, isServer);

        }

        public static PipeSettings[] LoadSettings(bool isServer)
        {
            List<PipeSettings> list = new List<PipeSettings>();
            try
            {
                System.Configuration.Configuration config = NetConfig.GetConfiguration();

                XmlDocument doc = new XmlDocument();
                doc.Load(config.FilePath);

                Netlog.Debug("LoadSettings : " + config.FilePath);

                string xpath = isServer ? "//PipeServerSettings" : "//PipeClientSettings";

                XmlNode root = doc.SelectSingleNode(xpath);

                foreach (XmlNode n in root.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Comment)
                        continue;

                    PipeSettings ps = new PipeSettings(n, isServer);
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
