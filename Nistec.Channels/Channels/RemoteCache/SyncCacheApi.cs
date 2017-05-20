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
using Nistec.Generic;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Nistec.Channels.RemoteCache
{
   

    /// <summary>
    /// A Session Api
    /// </summary>
    public class SyncCacheApi
    {

        NetProtocol protocol = CacheApi.DefaultProtocol;
        string hostAddress;
        int port;
        int readTimeout;
        bool useConfig;

        public static SyncCacheApi Get(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = CacheSettings.Protocol;
            }
            return new SyncCacheApi() { useConfig = true, protocol = protocol };
        }
        public static SyncCacheApi GetTcp(string hostAddress, int port, int readTimeout)
        {
            return new SyncCacheApi() { useConfig = false, hostAddress = hostAddress, port = port, readTimeout = readTimeout, protocol = NetProtocol.Tcp };
        }
        public static SyncCacheApi GetHttp(string hostAddress, string method, int readTimeout)
        {
            return new SyncCacheApi() { useConfig = false, hostAddress = hostAddress, port = CacheApi.HttpMethodToPort(method), readTimeout = readTimeout, protocol = NetProtocol.Http };
        }
        public static SyncCacheApi GetPipe(string hostAddress, int readTimeout)
        {
            return new SyncCacheApi() { useConfig = false, hostAddress = hostAddress, port = 0, readTimeout = readTimeout, protocol = NetProtocol.Pipe };
        }

        #region internal
        internal string GetKey(string itemName, string[] keys)
        {
            string key = keys == null ? "*" : MessageKey.GetKey(itemName, keys);
            return key;
        }

        internal object Get(string command, string itemName, string[] keys, Type type)
        {
            IMessage message = CacheApi.CreateMessage(command, GetKey(itemName, keys), type, protocol);
            if(useConfig)
                return CacheApi.SendDuplex(message, CacheApi.HostType.Sync, protocol);

            return CacheApi.SendDuplex(message, hostAddress, port, readTimeout, protocol);
        }

        internal T Get<T>(string command, string itemName, string[] keys)
        {
            switch (command)
            {
                case SyncCacheCmd.GetEntity:
                    var stream = (NetStream)Get(command, itemName, keys, typeof(NetStream));
                    if (stream == null)
                        return default(T);
                    stream.Position = 0;
                    return BinarySerializer.DeserializeFromStream<T>((NetStream)stream, SerialContextType.GenericEntityAsIEntityType);
                case SyncCacheCmd.GetEntityKeys:
                    {
                        IMessage msg = CacheApi.CreateMessage(command, itemName, typeof(T), protocol);
                        if (useConfig)
                            return CacheApi.SendDuplex<T>(msg, CacheApi.HostType.Sync, protocol);
                        return CacheApi.SendDuplex<T>(msg, hostAddress, port, readTimeout, protocol);
                    }
                default:
                    {
                        IMessage message = CacheApi.CreateMessage(command, GetKey(itemName, keys), typeof(T), protocol);
                        if (useConfig)
                            return CacheApi.SendDuplex<T>(message, CacheApi.HostType.Sync, protocol);
                        return CacheApi.SendDuplex<T>(message, hostAddress, port, readTimeout, protocol);
                    }
            }

        }

        internal void Do(string command, string key, string[] args)
        {
            IMessage message = CacheApi.CreateMessage(command, key, null, CacheApi.TypeEmpty, args, protocol);
            if (useConfig)
                CacheApi.SendOut(message, CacheApi.HostType.Sync, protocol);
            else
                CacheApi.SendOut(message, hostAddress, port, readTimeout, protocol);

        }
        #endregion

 
        /// <summary>
        /// Get item from sync cache using <see cref="MessageKey"/> an <see cref="Type"/>.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Get(MessageKey info, Type type)
        {
            return Get(SyncCacheCmd.GetSyncItem, info.ItemName, info.ItemKeys, type);
        }

        /// <summary>
        /// Get item from sync cache using arguments.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="keys"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Get(string entityName, string[] keys, Type type)
        {
            return Get(SyncCacheCmd.GetSyncItem, entityName, keys, type);
        }

        /// <summary>
        /// Get item from sync cache using <see cref="MessageKey"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <returns></returns>
        public T Get<T>(MessageKey info)
        {
            return Get<T>(SyncCacheCmd.GetSyncItem, info.ItemName, info.ItemKeys);
        }
        /// <summary>
        /// Get item from sync cache using arguments.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityName"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public T Get<T>(string entityName, string[] keys)
        {
            return Get<T>(SyncCacheCmd.GetSyncItem, entityName, keys);
        }

        /// <summary>
        ///  Get item as <see cref="IDictionary"/> from sync cache using pipe name and item key.
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary GetRecord(string itemName, string[] keys)
        {
            return Get<IDictionary>(SyncCacheCmd.GetRecord, itemName, keys);
        }


        /// <summary>
        /// Get item as <see cref="IDictionary"/> from sync cache using <see cref="MessageKey"/>.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        /// <example><code>
        /// //Get item value from sync cache as Dictionary.
        ///public void GetRecord()
        ///{
        ///    string key = "1";
        ///    var item = SyncCacheApi.GetRecord(MessageKey.Get("contactEntity", new string[] { "1" }));
        ///    if (item == null)
        ///        Console.WriteLine("item not found " + key);
        ///    else
        ///        Console.WriteLine(item["FirstName"]);
        ///}
        /// </code></example>
        public IDictionary GetRecord(MessageKey info)
        {
            return Get<IDictionary>(SyncCacheCmd.GetRecord, info.ItemName, info.ItemKeys);
        }

        /// <summary>
        /// Get item value from cache as json.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="format"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public string GetJson(MessageKey info, JsonFormat format)
        {
            var stream = GetAs(info);
            return CacheApi.ToJson(stream, format);
        }

        /// <summary>
        /// Get item value from cache as json.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="keys"></param>
        /// <param name="format"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public string GetJson(string entityName, string[] keys, JsonFormat format)
        {
            var stream = GetAs(entityName, keys);
            return CacheApi.ToJson(stream, format);
        }

        /// <summary>
        /// Get item as stream from sync cache using <see cref="MessageKey"/>.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public NetStream GetAs(MessageKey info)
        {
            return Get<NetStream>(SyncCacheCmd.GetAs, info.ItemName, info.ItemKeys);
        }

        /// <summary>
        /// Get item as stream from sync cache using arguments.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="keys"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public NetStream GetAs(string entityName, string[] keys)
        {
            return Get<NetStream>(SyncCacheCmd.GetAs, entityName, keys);
        }

        /// <summary>
        /// Get item as entity from sync cache using <see cref="MessageKey"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <returns></returns>
        /// <example><code>
        /// //Get item value from sync cache as Entity.
        ///public void GetEntity()
        ///{
        ///    string key = "1";
        ///    var item = <![CDATA[SyncCacheApi.GetEntity<ContactEntity>(MessageKey.Get("contactEntity", new string[] { "1" }));]]>
        ///    if (item == null)
        ///        Console.WriteLine("item not found " + key);
        ///    else
        ///        Console.WriteLine(item.FirstName);
        ///}
        /// </code></example>
        public T GetEntity<T>(MessageKey info)
        {
            return Get<T>(SyncCacheCmd.GetEntity, info.ItemName, info.ItemKeys);
        }

        /// <summary>
        /// Get item as entity from sync cache using arguments.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public T GetEntity<T>(string entityName, string[] keys)
        {
            return Get<T>(SyncCacheCmd.GetEntity, entityName, keys);
        }
        /// <summary>
        /// Reset all items in sync cache
        /// </summary>
        public void Reset()
        {
            Do(SyncCacheCmd.Reset, "*", null);
        }
        /// <summary>
        /// Refresh all items in sync cache
        /// </summary>
        public void Refresh()
        {
            Do(SyncCacheCmd.Refresh, "*", null);
        }
        /// <summary>
        /// Refresh specified item in sync cache.
        /// </summary>
        /// <param name="syncName"></param>
        /// <example><code>
        /// //Refresh sync item which mean reload sync item from Db.
        ///public void RefreshItem()
        ///{
        ///    SyncCacheApi.Refresh("contactGeneric");
        ///}
        /// </code></example>
        public void Refresh(string syncName)
        {
            Do(SyncCacheCmd.RefreshItem, syncName, null);
        }
        /// <summary>
        /// Refresh specified item in sync cache.
        /// </summary>
        /// <param name="syncName"></param>
        /// <example><code>
        /// //Remove item from sync cache.
        ///public void RemoveItem()
        ///{
        ///    SyncCacheApi.RemoveItem("contactGeneric");
        ///}
        /// </code></example>
        public void RemoveItem(string syncName)
        {
            Do(SyncCacheCmd.RemoveSyncItem, syncName, null);
        }
        /// <summary>
        /// Get entity values as <see cref="EntityStream"/> array from sync cache using entityName.
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public GenericKeyValue GetEntityItems(string entityName)
        {
            return Get<GenericKeyValue>(SyncCacheCmd.GetEntityItems, entityName, null);
        }

        /// <summary>
        /// Get entity keys from sync cache using entityName.
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public string[] GetEntityKeys(string entityName)
        {
            return Get<string[]>(SyncCacheCmd.GetEntityKeys, entityName, null);
        }

        /// <summary>
        /// Get all entity names from sync cache.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllEntityNames()
        {
            return Get<string[]>(SyncCacheCmd.GetAllEntityNames, "*", null);
        }

        /// <summary>
        /// Get entity items report from sync cache using entityName.
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public DataTable GetItemsReport(string entityName)
        {
            return Get<DataTable>(SyncCacheCmd.GetItemsReport, entityName, null);
        }

        /// <summary>
        /// Reply for test.
        /// </summary>
        /// <returns></returns>
        public string Reply(string text)
        {
            return Get<string>(SyncCacheCmd.Reply, text, new string[] { text });
        }
    }
   
}
