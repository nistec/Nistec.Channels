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

    #region CacheDefaults
    internal class CacheDefaults
    {
        /// <summary>
        /// Get Default Formatter
        /// </summary>
        public static Formatters DefaultFormatter { get { return Formatters.BinarySerializer; } }

        #region pipe
        /// <summary>
        /// Default Cache HostName
        /// </summary>
        public const string DefaultCacheHostName = "nistec_cache";
        /// <summary>
        /// Default Data HostName
        /// </summary>
        public const string DefaultDataCacheHostName = "nistec_cache_data";
        /// <summary>
        /// Default Session HostName
        /// </summary>
        public const string DefaultSessionHostName = "nistec_cache_session";
        /// <summary>
        /// Default Sync HostName
        /// </summary>
        public const string DefaultSyncCacheHostName = "nistec_cache_sync";
        /// <summary>
        /// Default CacheManager HostName
        /// </summary>
        public const string DefaultCacheManagerHostName = "nistec_cache_manager";

        /// <summary>
        /// Default CacheManager HostName
        /// </summary>
        public const string DefaultBundleHostName = "nistec_cache_bundle";

        #endregion

        #region TCP

        /// <summary>
        /// DefaultReadTimeout
        /// </summary>
        public const int DefaultReadTimeout = 1000;

        /// <summary>
        /// Default Sync Address
        /// </summary>
        public const string DefaultBundleAddress = "localhost";
        /// <summary>
        /// DefaultCachePort
        /// </summary>
        public const int DefaultBundlePort = 13000;

        /// <summary>
        /// Default Sync Address
        /// </summary>
        public const string DefaultCacheAddress = "localhost";
        /// <summary>
        /// DefaultCachePort
        /// </summary>
        public const int DefaultCachePort = 13001;

        /// <summary>
        /// Default Data Address
        /// </summary>
        public const string DefaultDataCacheAddress = "localhost";
        /// <summary>
        /// DefaultDataCachePort
        /// </summary>
        public const int DefaultDataCachePort = 13002;

        /// <summary>
        /// Default Session Address
        /// </summary>
        public const string DefaultSessionAddress = "localhost";
        /// <summary>
        /// DefaultSessionPort
        /// </summary>
        public const int DefaultSessionPort = 13003;

        /// <summary>
        /// Default Sync Address
        /// </summary>
        public const string DefaultSyncCacheAddress = "localhost";
        /// <summary>
        /// DefaultSyncCachePort
        /// </summary>
        public const int DefaultSyncCachePort = 13004;

        /// <summary>
        /// Default CacheManager Address
        /// </summary>
        public const string DefaultCacheManagerAddress = "localhost";
        /// <summary>
        /// DefaultCacheManagerPort
        /// </summary>
        public const int DefaultCacheManagerPort = 13005;
        /// <summary>
        /// DefaultTaskTimeout
        /// </summary>
        public const int DefaultTaskTimeout = 240;

        #endregion

        internal const int DefaultSessionTimeout = 30;
        public static NetProtocol DefaultProtocol = NetProtocol.Tcp;

    }
    #endregion

    /// <summary>
    /// Represent the cache api settings as read only.
    /// </summary>
    public class CacheSettings
    {
 
        #region CacheClientSettings

        static string _RemoteCacheHostName = CacheDefaults.DefaultCacheHostName;
        static string _RemoteSyncCacheHostName = CacheDefaults.DefaultSyncCacheHostName;
        static string _RemoteSessionHostName = CacheDefaults.DefaultSessionHostName;
        static string _RemoteDataCacheHostName = CacheDefaults.DefaultDataCacheHostName;
        static string _RemoteCacheManagerHostName = CacheDefaults.DefaultCacheManagerHostName;
        static string _RemoteBundleHostName = CacheDefaults.DefaultBundleHostName;
        static int _SessionTimeout = CacheDefaults.DefaultSessionTimeout;

        /// <summary>RemoteCacheHostName.</summary>
        public static string RemoteCacheHostName { get { return _RemoteCacheHostName; } set { _RemoteCacheHostName = value; } }
        /// <summary>RemoteSyncCacheHostName.</summary>
        public static string RemoteSyncCacheHostName { get { return _RemoteSyncCacheHostName; } set { _RemoteSyncCacheHostName = value; } }
        /// <summary>RemoteSessionHostName.</summary>
        public static string RemoteSessionHostName { get { return _RemoteSessionHostName; } set { _RemoteSessionHostName = value; } }
        /// <summary>RemoteDataCacheHostName.</summary>
        public static string RemoteDataCacheHostName { get { return _RemoteDataCacheHostName; } set { _RemoteDataCacheHostName = value; } }
        /// <summary>RemoteCacheManagerHostName.</summary>
        public static string RemoteCacheManagerHostName { get { return _RemoteCacheManagerHostName; } set { _RemoteCacheManagerHostName = value; } }

        /// <summary>RemoteCacheBundleHostName.</summary>
        public static string RemoteBundleHostName { get { return _RemoteBundleHostName; } set { _RemoteBundleHostName = value; } }

        const bool DefaultIsAsync = false;

        const bool DefaultEnableException = false;

        static bool _IsRemoteAsync = DefaultIsAsync;
        /// <summary>IsRemoteAsync.</summary>
        public static bool IsRemoteAsync
        {
            get
            {
                return _IsRemoteAsync;// NetConfig.Get<bool>("IsRemoteAsync", DefaultIsAsync);
            }
            set { _IsRemoteAsync = value; }
        }

        static bool _EnableRemoteException = DefaultEnableException;

        /// <summary>EnableRemoteException.</summary>
        public static bool EnableRemoteException
        {
            get
            {
                return _EnableRemoteException;// NetConfig.Get<bool>("EnableRemoteException", DefaultEnableException);
            }
            set { _EnableRemoteException = value; }
        }


        static NetProtocol _Protocol = CacheDefaults.DefaultProtocol;

        /// <summary>Protocol.</summary>
        public static NetProtocol Protocol
        {
            get
            {
                return _Protocol;
            }
            set { _Protocol = value; }
        }


        /// <summary>SessionTimeout.</summary>
        public static int SessionTimeout { get { return _SessionTimeout; } set { _SessionTimeout = value; } }



        static NameValueCollection ApiConfig;

        /// <summary>
        /// Get <see cref="CacheConfigClient"/>.
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection GetApiConfig()
        {
            if (ApiConfig == null)
                ApiConfig = ConfigurationManager.GetSection("RemoteCache") as NameValueCollection;
            return ApiConfig;
        }


        static CacheSettings()
        {
            try
            {
                System.Configuration.Configuration config = NetConfig.GetConfiguration();

                XmlDocument doc = new XmlDocument();
                doc.Load(config.FilePath);
                //Netlog.Debug("Load RemoteCache settings : " + config.FilePath);
                string xpath = ".//RemoteCache/CacheApiSettings";
                XmlNode root = doc.SelectSingleNode(xpath);
                LoadItemSettings(root);
            }
            catch (Exception ex)
            {
                string err = ex.Message;
                //Netlog.Error("Load RemoteCache settings error: " + ex.Message);
            }
        }

        static void LoadItemSettings(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("CacheApiSettings.XmlNode node");
            }
            XmlTable table = XmlTable.LoadXmlKeyValue(node, "key", "value");
            //XmlTable table = new XmlTable(node, "key");

            _IsRemoteAsync = table.Get<bool>("IsRemoteAsync", DefaultIsAsync);
            _EnableRemoteException = table.Get<bool>("EnableRemoteException", DefaultEnableException);

            _RemoteCacheHostName = table.Get<string>("RemoteCacheHostName", CacheDefaults.DefaultCacheHostName);
            _RemoteSyncCacheHostName = table.Get<string>("RemoteSyncCacheHostName", CacheDefaults.DefaultSyncCacheHostName);
            _RemoteSessionHostName = table.Get<string>("RemoteSessionHostName", CacheDefaults.DefaultSessionHostName);
            _RemoteDataCacheHostName = table.Get<string>("RemoteDataCacheHostName", CacheDefaults.DefaultDataCacheHostName);
            _RemoteCacheManagerHostName = table.Get<string>("RemoteCacheManagerHostName", CacheDefaults.DefaultCacheManagerHostName);

            _RemoteBundleHostName = table.Get<string>("RemoteBundleHostName", CacheDefaults.DefaultBundleHostName);

            _SessionTimeout = table.Get<int>("SessionTimeout", CacheDefaults.DefaultSessionTimeout);
            _Protocol = table.GetEnum<NetProtocol>("Protocol", CacheDefaults.DefaultProtocol);

        }

        #endregion

    }
   
}
