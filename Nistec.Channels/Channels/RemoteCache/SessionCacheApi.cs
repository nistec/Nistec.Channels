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
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Channels.RemoteCache
{
    /// <summary>
    /// A Session Api
    /// </summary>
    public class SessionCacheApi
    {


        NetProtocol protocol = CacheApi.DefaultProtocol;
        string hostAddress;
        int port;
        int readTimeout;
        bool useConfig;

        public static SessionCacheApi Get(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = CacheSettings.Protocol;
            }

            return new SessionCacheApi() { useConfig = true, protocol = protocol };
        }
        public static SessionCacheApi GetTcp(string hostAddress, int port, int readTimeout)
        {
            return new SessionCacheApi() { useConfig = false, hostAddress = hostAddress, port = port, readTimeout = readTimeout, protocol = NetProtocol.Tcp };
        }
        public static SessionCacheApi GetHttp(string hostAddress, string method, int readTimeout)
        {
            return new SessionCacheApi() { useConfig = false, hostAddress = hostAddress, port = CacheApi.HttpMethodToPort(method), readTimeout = readTimeout, protocol = NetProtocol.Http };
        }
        public static SessionCacheApi GetPipe(string hostAddress, int readTimeout)
        {
            return new SessionCacheApi() { useConfig = false, hostAddress = hostAddress, port = 0, readTimeout = readTimeout, protocol = NetProtocol.Pipe };
        }

 
        #region internal send



        internal object Get(string command, string sessionId, string key, Type type)
        {

            IMessage message = CacheApi.CreateMessage(command, key, sessionId, type, null, protocol);
            if(useConfig)
                return CacheApi.SendDuplex(message, CacheApi.HostType.Session, protocol);
            return CacheApi.SendDuplex(message, hostAddress, port, readTimeout, protocol);
        }

        internal T Get<T>(string command, string sessionId, string key)
        {

            IMessage message = CacheApi.CreateMessage(command, key, sessionId, typeof(T), null, protocol);
            if (useConfig)
                return CacheApi.SendDuplex<T>(message, CacheApi.HostType.Session, protocol);
            return CacheApi.SendDuplex<T>(message, hostAddress, port, readTimeout, protocol);

        }

        internal void Do(string command, string sessionId, string key = "*", int expiration = 0, string[] keyValue = null)
        {
            IMessage message = CacheApi.CreateMessage(command, key, sessionId, CacheApi.TypeEmpty, null, protocol);
            message.Expiration = expiration;
            if(useConfig)
                CacheApi.SendOut(message, CacheApi.HostType.Session, protocol);
            else
            CacheApi.SendOut(message, hostAddress, port, readTimeout, protocol);

        }


        internal object Set(string command, string sessionId, string key, object value, int expiration, string[] args)
        {
            if (value == null)
                return KnownCacheState.ArgumentsError;

            IMessage message = CacheApi.CreateMessage(command, key, sessionId, value.GetType(), args, protocol);
            message.SetBody(value);
            message.Expiration = expiration;
            message.Id = sessionId;
            if (useConfig)
                return CacheApi.SendDuplex(message, CacheApi.HostType.Session, protocol);
            return CacheApi.SendDuplex(message, hostAddress, port, readTimeout, protocol);
        }
        #endregion

        /// <summary>
        /// Get item from session cache.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public string Get(string sessionId, string key, string defaultValue = null)
        {
            return Types.NzOr(Get<string>(SessionCmd.GetSessionItem, sessionId, key), defaultValue);
        }

        /// <summary>
        /// Get item value from cache as json.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="format"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public string GetJson(string sessionId, string key, JsonFormat format)
        {
            var stream = Get<NetStream>(SessionCmd.GetSessionItem, sessionId, key);
            return CacheApi.ToJson(stream, format);
        }

        /// <summary>
        /// Get item from session cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public T Get<T>(string sessionId, string key)
        {
            return Get<T>(SessionCmd.GetSessionItem, sessionId, key);
        }

        /// <summary>
        /// Add new session with CacheSettings.SessionTimeout configuration.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="expiration"></param>
        /// <param name="keyValueArgs"></param>
        public void Create(string sessionId, int expiration, string[] keyValueArgs)
        {
            Do(SessionCmd.AddSession, sessionId, "*", expiration, keyValueArgs);
        }


        /// <summary>
        /// Add item to specified session (if session not exists create new one) with CacheSettings.SessionTimeout configuration.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public object Set(string sessionId, string key, object value)
        {
            object o = Set(SessionCmd.AddSessionItem, sessionId, key, value, CacheApi.SessionTimeout, null);
            return Types.ToInt(o);
        }


        /// <summary>
        /// Get all sessions keys in session cache using <see cref="KnownSessionState"/> state.
        /// </summary>
        /// <returns></returns>
        public string[] ReportAllStateKeys(string state)
        {
            return Get<string[]>(SessionCmd.GetAllSessionsStateKeys, "*", state);
        }

        /// <summary>
        ///  Fetch item from specified session cache using session id and item key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Fetch<T>(string sessionId, string key)
        {
            return Get<T>(SessionCmd.FetchSessionItem, sessionId, key);
        }

        /// <summary>
        /// Copy session item to a new place in <see cref="MCache"/> cache.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="targetKey"></param>
        /// <param name="expiration"></param>
        /// <param name="addToCache"></param>
        /// <returns></returns>
        /// <example><code>
        /// //Copy item from session to cache.
        ///public void CopyTo()
        ///{
        ///    string key = "item key 1";
        ///    CacheApi.Session.CopyTo(sessionId, key, key, timeout, true);
        ///}
        /// </code></example>
        public void CopyTo(string sessionId, string key, string targetKey, int expiration, bool addToCache = false)
        {
            Do(SessionCmd.CopyTo, sessionId, key, expiration, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() });
        }
        /// <summary>
        /// Copy session item to a new place in <see cref="MCache"/> cache, and remove the current session item.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="targetKey"></param>
        /// <param name="expiration"></param>
        /// <param name="addToCache"></param>
        /// <returns></returns>
        /// <example><code>
        /// //Fetch item from current session to cache.
        ///public void FetchTo()
        ///{
        ///    string key = "item key 2";
        ///    SessionApi.FetchTo(sessionId, key, key, timeout, true);
        ///}
        /// </code></example>
        public void FetchTo(string sessionId, string key, string targetKey, int expiration, bool addToCache = false)
        {
            Do(SessionCmd.FetchTo, sessionId, key, expiration, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() });
        }

        /// <summary>
        /// Remove session from session cache.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="isAsync"></param>
        /// <example><code>
        ///  //remove session with items.
        ///public void RemoveSession()
        ///{
        ///    SessionApi.RemoveSession(sessionId);
        ///}
        /// </code></example>
        public void RemoveSession(string sessionId)
        {
            Do(SessionCmd.RemoveSession, sessionId, "*", 0, null);
        }
        /// <summary>
        /// Remove all items from specified session.
        /// </summary>
        /// <param name="sessionId"></param>
        public void Clear(string sessionId)
        {
            Do(SessionCmd.ClearSessionItems, sessionId, "*", 0, null);
        }
        /// <summary>
        /// Remove all sessions from session cache.
        /// </summary>
        public void ClearAllSessions()
        {
          Do(SessionCmd.ClearAllSessions, "*", "*", 0, null);
        }

        /// <summary>
        /// Refresh specified session in session cache.
        /// </summary>
        /// <param name="sessionId"></param>
        public void Refresh(string sessionId)
        {
            Do(SessionCmd.SessionRefresh, sessionId, "*", 0, null);
        }
        /// <summary>
        /// Refresh sfcific session in session cache or create a new session bag if not exists.
        /// </summary>
        /// <param name="sessionId"></param>
        public void RefreshOrCreate(string sessionId)
        {
            Do(SessionCmd.RefreshOrCreate, sessionId, "*", 0, null);
        }

        /// <summary>
        /// Remove item from specified session using session id and item key.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <example><code>
        /// //Remove item from current session
        ///public void Remove()
        ///{
        ///    string key = "item key 3";
        ///    bool ok = SessionApi.RemoveItem(sessionId, key);
        ///    Console.WriteLine(ok);
        ///}
        /// </code></example>
        public void Remove(string sessionId, string key)
        {
            Do(SessionCmd.RemoveSessionItem, sessionId, key, 0, null);
        }

        /// <summary>
        /// Add item to specified session in session cache.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeout"></param>
        /// <param name="validateExisting"></param>
        /// <returns></returns>
        /// <example><code>
        /// //Add items to current session.
        ///public void AddItems()
        ///{
        ///     string sessionId = "12345678";
        ///     string userId = "12";
        ///     int timeout = 0;
        ///    SessionApi.Set(sessionId, "item key 1", new EntitySample() { Id = 123, Name = "entity sample 1", Creation = DateTime.Now, Value = "entity item one" }, timeout);
        ///    SessionApi.Set(sessionId, "item key 2", new EntitySample() { Id = 124, Name = "entity sample 2", Creation = DateTime.Now, Value = "entity item second" }, timeout);
        ///    SessionApi.Set(sessionId, "item key 3", new EntitySample() { Id = 125, Name = "entity sample 3", Creation = DateTime.Now, Value = "entity item minute" }, timeout);
        ///}
        /// </code></example>
        public int AddItemExisting(string sessionId, string key, object value, int timeout, bool validateExisting = false)
        {
            string cmd = (validateExisting) ? SessionCmd.AddItemExisting : SessionCmd.AddSessionItem;
            object o = Set(cmd, sessionId, key, value, timeout, null);
            return Types.ToInt(o);
        }
        /// <summary>
        /// Get indicate whether the session cache contains specified item in specific session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(string sessionId, string key)
        {
            return Get<bool>(SessionCmd.Exists, sessionId, key);
        }

        /// <summary>
        /// Get all sessions keys in session cache.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllSessionsKeys()
        {
            return Get<string[]>(SessionCmd.GetAllSessionsKeys, "*", "*");
        }
        /// <summary>
        /// Get all items keys in specified session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public string[] GetSessionsItemsKeys(string sessionId)
        {
            return Get<string[]>(SessionCmd.GetSessionItemsKeys, "*", "*");
        }

        /// <summary>
        /// Reply for test.
        /// </summary>
        /// <returns></returns>
        public string Reply(string text)
        {
            return Get<string>(SessionCmd.Reply, text, text);
        }
    }

}
