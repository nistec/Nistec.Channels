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
using Nistec.Channels.Http;
using Nistec.Channels.Tcp;
using Nistec.Generic;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nistec.Channels.RemoteCache
{
    public enum HostType { Cache, Sync, Session, Data };

    /// <summary>
    /// Represent cache api for client.
    /// </summary>
    public class RemoteCacheApi
    {
        #region members
        //13000-13159

        public const int DefaultTcpPort = 13000;
        public const int DefaultHttpPort = 13010;
        public const string DefaultHttpMethod = "post";
 
        protected NetProtocol protocol = RemoteCacheApi.DefaultProtocol;
        protected string hostAddress;
        protected int port;
        protected string httpMethod;
        protected int timeout;
        protected bool useConfig;
        protected HostType hostType;

        /// <summary>
        /// Get or Set if the client use async method.
        /// </summary>
        public static bool IsRemoteAsync = false;
        /// <summary>
        /// Get or Set if the client will throw exeption on error.
        /// </summary>
        public static bool EnableRemoteException = false;


        static int _DefaultSessionTimeout = RemoteCacheSettings.DefaultSessionTimeout;
        /// <summary>
        /// Get or Set The session timeout in minutes.
        /// </summary>
        public static int DefaultSessionTimeout
        {
            get { return _DefaultSessionTimeout; }
            set { _DefaultSessionTimeout = value < 0 ? RemoteCacheSettings.DefaultSessionTimeout : value; }
        }

        static int _DefaultCacheExpiration = RemoteCacheSettings.DefaultCacheExpiration;
        /// <summary>
        /// Get or Set The cache expiration in minutes.
        /// </summary>
        public static int DefaultCacheExpiration
        {
            get { return _DefaultCacheExpiration; }
            set { _DefaultCacheExpiration = value < 0 ? RemoteCacheSettings.DefaultCacheExpiration : value; }
        }

        internal enum EnumEmpty { NA };
        /// <summary>
        /// Default Protocol
        /// </summary>
        public const NetProtocol DefaultProtocol = NetProtocol.Tcp;

        #endregion

        #region ctor
        static RemoteCacheApi()
        {
            IsRemoteAsync = RemoteCacheSettings.IsRemoteAsync;
            EnableRemoteException = RemoteCacheSettings.EnableRemoteException;
            DefaultSessionTimeout = RemoteCacheSettings.SessionTimeout;
            DefaultCacheExpiration = RemoteCacheSettings.CacheExpiration;
        }
        /// <summary>
        /// Get <see cref="CacheApi"/> api.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="httpMethod"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static CacheApi Get(string hostAddress,int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new CacheApi() { useConfig = false, hostType = HostType.Cache, protocol = protocol, hostAddress=hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod==null? CacheApi.DefaultHttpMethod: httpMethod, CacheExpiration= DefaultCacheExpiration };
        }
        /// <summary>
        /// Get <see cref="CacheApi"/> api.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static CacheApi Get(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new CacheApi() { useConfig = true, hostType = HostType.Cache, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null,protocol), CacheExpiration = DefaultCacheExpiration };
        }
        /// <summary>
        /// Get <see cref="SessionApi"/> api.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="httpMethod"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static SessionApi Session(string hostAddress, int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SessionApi() { useConfig = false, hostType = HostType.Session, protocol = protocol, hostAddress = hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod == null ? CacheApi.DefaultHttpMethod : httpMethod, SessionTimeOut = DefaultSessionTimeout };
        }
        /// <summary>
        /// Get <see cref="SessionApi"/> api.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static SessionApi Session(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SessionApi() { useConfig = true, hostType = HostType.Session, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null, protocol), SessionTimeOut = DefaultSessionTimeout };
        }
        /// <summary>
        /// Get<see cref="SyncApi"/> api.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="httpMethod"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static SyncApi Sync(string hostAddress, int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SyncApi() { useConfig = false, hostType = HostType.Sync, protocol = protocol, hostAddress = hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod == null ? CacheApi.DefaultHttpMethod : httpMethod };
        }
        /// <summary>
        /// Get<see cref="SyncApi"/> api.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static SyncApi Sync(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SyncApi() { useConfig = true, hostType = HostType.Sync, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null, protocol) };
        }


        #endregion

        #region Send internal

        internal T SendDuplexStream<T>(IMessageStream message, Action<string> onFault)
        {
            TransStream ts = SendDuplexStream(message);
            if (ts == null)
            {
                onFault(message.Command + " return null!");
                return default(T);
            }
            return ts.ReadValue<T>(onFault);
        }
        internal object SendDuplexStreamValue(IMessageStream message, Action<string> onFault)
        {
            TransStream ts = SendDuplexStream(message);
            if (ts == null)
            {
                onFault(message.Command + " return null!");
                return null;
            }
            return ts.ReadValue(onFault);
        }
        internal RemoteCacheState SendDuplexState(IMessageStream message)
        {
            TransStream ts = SendDuplexStream(message);
            if (ts == null)
            {
                return RemoteCacheState.InvalidItem;
            }
            return (RemoteCacheState)ts.ReadState();
        }
        internal TransStream SendDuplexStream(IMessageStream message)
        {
            switch (protocol)
            {
                case NetProtocol.Http:
                    return HttpClient.SendDuplexStream(message as HttpMessage, hostAddress, port, httpMethod, timeout,
                            CacheApi.EnableRemoteException);

                case NetProtocol.Pipe:
                    return PipeClient.SendDuplexStream(message as PipeMessage, hostAddress,
                            CacheApi.EnableRemoteException,
                    PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync));

                case NetProtocol.Tcp:
                    break;
            }
            return TcpClient.SendDuplexStream(message as TcpMessage, hostAddress, port, timeout,
                            CacheApi.EnableRemoteException);
        }

        internal void SendOutAsync(IMessageStream message)
        {
            Task.Factory.StartNew(() => SendOut(message));
        }

        internal void SendOut(IMessageStream message)
        {
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        TcpClient.SendOut(message as TcpMessage, hostAddress, port, timeout, false, CacheApi.EnableRemoteException);
                        break;
                    }
                case NetProtocol.Http:
                    {
                        HttpClient.SendOut(message as HttpMessage, hostAddress, port, httpMethod, timeout, CacheApi.EnableRemoteException);
                        break;
                    }
                case NetProtocol.Pipe:
                    {
                        PipeClient.SendOut(message as PipeMessage,
                            hostAddress,
                            CacheApi.EnableRemoteException,
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync));

                        break;
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }

        }

     
        #endregion

        #region helper methods

        public static string StreamToJson(NetStream stream, JsonFormat format)
        {
            if(stream==null)
            {
                throw new ArgumentNullException("stream");
            }
            stream.Position = 0;
            using (BinaryStreamer streamer = new BinaryStreamer(stream))
            {
                var obj = streamer.Decode();
                if (obj == null)
                    return null;
                else
                    return JsonSerializer.Serialize(obj, null, format);
            }
        }

        public static string ToJson(object value, JsonFormat format)
        {
            if (value == null)
            {
                return null;// throw new ArgumentNullException("value");
            }
            if (value is NetStream)
            {
                return StreamToJson((NetStream)value, format);
            }
            return JsonSerializer.Serialize(value, null, format);
        }


        #endregion

        #region Common SendJson

        public string SendJsonDuplex(string command, string key, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, null, 0);
            //string json = JsonSerializer.Serialize(message);
            string response = SendJsonDuplex(message.ToJson(), pretty);
            return response;
        }
        public string SendJsonDuplex(string command, string key, object value, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, value, 0);
            //message.SetBody(value);
            //message.Expiration = expiration;
            //string json = JsonSerializer.Serialize(message);
            string response = SendJsonDuplex(message.ToJson(), pretty);
            return response;
        }
        public string SendJsonDuplex(string command, string key, object value, int expiration, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, value, expiration);
            //message.SetBody(value);
            //message.Expiration = expiration;
            //string json = JsonSerializer.Serialize(message);
            string response = SendJsonDuplex(message.ToJson(), pretty);
            return response;
        }
        public string SendJsonDuplex(string command, string key, object value, int expiration, string sessionId, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, sessionId, value, null, expiration);
            //message.SetBody(value);
            //message.Expiration = expiration;
            string json = JsonSerializer.Serialize(message);
            string response = SendJsonDuplex(json, pretty);
            return response;
        }
        public string SendJsonDuplex(string json, bool pretty = false)
        {
            string response = null;

            switch (protocol)
            {
                case NetProtocol.Pipe:
                    response = PipeJsonClient.SendDuplex(json, hostAddress, PipeOptions.None);
                    break;
                case NetProtocol.Tcp:
                    response = TcpJsonClient.SendDuplex(json, hostAddress, port, timeout, false);
                    break;
                case NetProtocol.Http:
                    response = HttpJsonClient.SendDuplex(json, hostAddress, port, httpMethod, timeout, false);
                    break;
            }

            if (pretty)
            {
                if (response != null)
                    response = JsonSerializer.Print(response);
            }
            return response;

        }
        #endregion

        #region CacheApi
        /// <summary>
        /// Represent a cache api.
        /// </summary>
        public class CacheApi: RemoteCacheApi
        {
            internal CacheApi() { }

            int _CacheExpiration;
            public int CacheExpiration
            {
                get { return _CacheExpiration; }
                set { _CacheExpiration = (value < 0) ? DefaultCacheExpiration : value; }
            }

            protected void OnFault(string message)
            {
                Console.WriteLine("CacheApi Fault: " + message);
            }

            public string ToJson(object obj, JsonFormat format= JsonFormat.None)
            {
                if (obj == null)
                    return null;
                return JsonSerializer.Serialize(obj, null, format);
            }

            #region public cache commands

            /// <summary>
            /// Get or Set cache value by key.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public object this[string key]
            {
                get { return Get(key); }
                set { Set(key, value, CacheExpiration); }
            }

            /// <summary>
            /// Get item value from cache.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <returns></returns>
            public T Get<T>(string key)
            {
                var val = Get(key);
                if (val == null)
                    return default(T);
                return GenericTypes.Convert<T>(val);
            }

            /// <summary>
            /// Get item value from cache.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <param name="defaultValue"></param>
            /// <returns></returns>
            public T Get<T>(string key, T defaultValue)
            {
                var val = Get(key);
                if (val == null)
                    return defaultValue;
                return GenericTypes.Convert<T>(val, defaultValue);
            }

            /// <summary>
            /// Get item value from cache.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public object Get(string key)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Get, key);
                return SendDuplexStreamValue(message, OnFault);
            }

            /// <summary>
            /// Get item value from cache.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public IDictionary<string, object> GetRecord(string key)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.GetRecord, key);
                return SendDuplexStream<IDictionary<string,object>>(message, OnFault);
            }

            /// <summary>
            /// Get item value from cache as json.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="format"></param>
            /// <returns></returns>
            public string GetJson(string key, JsonFormat format)
            {
                var obj = Get(key);
                if (obj == null)
                    return null;
                return JsonSerializer.Serialize(obj, null, format);
            }


            /// <summary>
            /// Remove item from cache
            /// </summary>
            /// <param name="cacheKey"></param>
            public void Remove(string cacheKey)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Remove, cacheKey);
                SendOut(message);
            }

            /// <summary>
            /// Get value from cache as <see cref="NetStream"/>
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <returns></returns>
            public NetStream GetStream(string cacheKey)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Get, cacheKey);
                return SendDuplexStream<NetStream>(message, OnFault);
            }

            /// <summary>
            /// Fetch Value from cache (Cut item from cache)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="cacheKey"></param>
            /// <returns></returns>
            public T Fetch<T>(string cacheKey)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Fetch, cacheKey);
                return SendDuplexStream<T>(message, OnFault);
            }

            /// <summary>
            /// Set a new item to the cache, if this item is exists override it with the new one.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
            public RemoteCacheState Set(string cacheKey, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Set, cacheKey, null, value, null, expiration);
                return SendDuplexState(message);
            }
            /// <summary>
            ///  Set a new item to the cache, if this item is exists override it with the new one.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
            public RemoteCacheState Set(string cacheKey, object value, string sessionId, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Set, cacheKey, sessionId, value, null, expiration);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Add a new item to the cache, only if this item not exists.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
            public RemoteCacheState Add(string cacheKey, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Add, cacheKey, null, value, null, expiration);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Add a new item to the cache, only if this item not exists.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
            public RemoteCacheState Add(string cacheKey, object value, string sessionId, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Add, cacheKey, sessionId, value, null, expiration);
                return SendDuplexState(message);
            }


            /// <summary>
            /// Copy item in cache from source to another destination.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="dest"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
            public void CopyTo(string source, string dest, int expiration)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.CopyTo, dest, expiration);
                message.Args = MessageStream.CreateArgs(KnowsArgs.Source, source, KnowsArgs.Destination, dest);
                SendOut(message);
            }

            /// <summary>
            /// Cut item in cache from source to another destination.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="dest"></param>
            /// <param name="expiration"></param>
            public void CutTo(string source, string dest, int expiration)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.CutTo, dest, expiration);
                message.Args = MessageStream.CreateArgs(KnowsArgs.Source, source, KnowsArgs.Destination, dest);
                SendOut(message);
            }

            /// <summary>
            /// Remove all session items from cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns>return number of items removed from cache.</returns>
            public void RemoveItemsBySession(string sessionId)
            {
                if (sessionId == null)
                    return;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.RemoveItemsBySession, "*");
                message.Label = sessionId;
                SendOut(message);
            }
            /// <summary>
            /// Keep Alive Cache Item.
            /// </summary>
            /// <param name="cacheKey"></param>
            public void KeepAliveItem(string cacheKey)
            {
                if (cacheKey == null)
                    return;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.KeepAliveItem, cacheKey, 0);
                SendOut(message);
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Reply, text);
                return SendDuplexStream<string>(message, OnFault);
            }
            #endregion
        }
        #endregion

        #region SessionApi
        /// <summary>
        /// Represent a session cache api.
        /// </summary>
        public class SessionApi: RemoteCacheApi
        {
            internal SessionApi() { }

            int _SessionTimeOut;
            public int SessionTimeOut
            {
                get { return _SessionTimeOut; }
                set { _SessionTimeOut = (value < 0) ? DefaultSessionTimeout : value; }
            }

            protected void OnFault(string message)
            {
                Console.WriteLine("SessionApi Fault: " + message);
            }

            #region public session commands

            /// <summary>
            /// Get or Set cache session value by key.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public object this[string sessionId, string key]
            {
                get { return Get(sessionId,key); }
                set { Set(sessionId, key, value, SessionTimeOut); }
            }

            /// <summary>
            /// Get value from session cache.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public T Get<T>(string sessionId, string key)
            {
                var val = Get(sessionId, key);
                if (val == null)
                    return default(T);
                return GenericTypes.Convert<T>(val);
            }

            /// <summary>
            /// Get value from session cache.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="defaultValue"></param>
            /// <returns></returns>
            public T Get<T>(string sessionId, string key, T defaultValue)
            {
                var val = Get(sessionId, key);
                if (val == null)
                    return defaultValue;
                return GenericTypes.Convert<T>(val, defaultValue);
            }

            /// <summary>
            /// Get value from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public object Get(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Get, key, sessionId, null, null, 0);
                return SendDuplexStreamValue(message, OnFault);
            }

            /// <summary>
            /// Get item value from cache as json.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="format"></param>
            /// <returns></returns>
            public string GetJson(string sessionId, string key, JsonFormat format)
            {
                var obj = Get(sessionId, key);
                if (obj == null)
                    return null;
                return JsonSerializer.Serialize(obj, null, format);
            }

            /// <summary>
            /// Get item from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public DynamicEntity GetRecord(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetRecord, key, sessionId, null, null, 0);
                return SendDuplexStream<DynamicEntity>(message, OnFault);
            }

 
            /// <summary>
            ///  Fetch item from specified session cache using session id and item key.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public DynamicEntity FetchRecord(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.FetchRecord, key, sessionId, null, null, 0);
                return SendDuplexStream<DynamicEntity>(message, OnFault);
            }
            /// <summary>
            /// Get item from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns></returns>
            public IDictionary<string,object> GetSessionItems(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetSessionItems, null, sessionId, null, null, 0);
                return SendDuplexStream<IDictionary<string,object>>(message, OnFault);
            }

            /// <summary>
            /// Add new session with CacheSettings.SessionTimeout configuration.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <param name="keyValueArgs"></param>
            public RemoteCacheState CreateSession(string sessionId, int expiration, string[] keyValueArgs)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CreateSession, "*", sessionId, null, keyValueArgs, expiration);
                return SendDuplexState(message);
            }


            /// <summary>
            /// Add item to specified session (if session not exists create new one) with CacheSettings.SessionTimeout configuration.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns></returns>
            public RemoteCacheState Add(string sessionId, string key, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;

                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Add, key, sessionId, value, null, expiration);
                return SendDuplexState(message);
            }

            /// <summary>
            /// Add item to specified session (if session not exists create new one) with CacheSettings.SessionTimeout configuration.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns></returns>
            public RemoteCacheState Set(string sessionId, string key, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;

                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Set, key, sessionId, value, null, expiration);
                return SendDuplexState(message);
            }

            /// <summary>
            /// Get all sessions keys in session cache using <see cref="KnownSessionState"/> state.
            /// </summary>
            /// <returns></returns>
            public string[] GetAllSessionsKeysByState(string state)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ViewAllSessionsKeysByState, state);
                return SendDuplexStream<string[]>(message, OnFault);
            }


            /// <summary>
            /// Copy session item to a new place in MCache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="targetKey"></param>
            /// <param name="expiration"></param>
            /// <param name="addToCache"></param>
            /// <returns></returns>
            public RemoteCacheState CopyTo(string sessionId, string key, string targetKey, int expiration, bool addToCache = false)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CopyTo, key, sessionId, null, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() }, expiration);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Copy session item to a new place in MCache, and remove the current session item.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="targetKey"></param>
            /// <param name="expiration"></param>
            /// <param name="addToCache"></param>
            /// <returns></returns>
            public RemoteCacheState CutTo(string sessionId, string key, string targetKey, int expiration, bool addToCache = false)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CutTo, key, sessionId, null, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() }, expiration);
                return SendDuplexState(message);
            }

            /// <summary>
            /// Remove session from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState RemoveSession(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RemoveSession, "*", sessionId, null, null, 0);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Remove all items from specified session.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState Clear(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ClearItems, "*", sessionId, null, null, 0);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Remove all sessions from session cache.
            /// </summary>
            public RemoteCacheState ClearAllSessions()
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ClearAll, "*");
                return SendDuplexState(message);
            }

            /// <summary>
            /// Refresh specified session in session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState Refresh(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Refresh, "*", sessionId, null, null, 0);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Refresh sfcific session in session cache or create a new session bag if not exists.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState RefreshOrCreate(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RefreshOrCreate, "*", sessionId, null, null, 0);
                return SendDuplexState(message);

            }

            /// <summary>
            /// Remove item from specified session using session id and item key.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public RemoteCacheState Remove(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Remove, key, sessionId, null, null, 0);
                return SendDuplexState(message);
            }

 
            /// <summary>
            /// Get indicate whether the session cache contains specified item in specific session.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public bool Exists(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Exists, key, sessionId, null, null, 0);
                return SendDuplexState(message)== RemoteCacheState.Ok;
            }

            /// <summary>
            /// Get all sessions keys in session cache.
            /// </summary>
            /// <returns></returns>
            public string[] GetAllSessionsKeys()
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ViewAllSessionsKeys, "*", "*", null, null, 0);
                return SendDuplexStream<string[]> (message, OnFault);
            }
            /// <summary>
            /// Get all items keys in specified session.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns></returns>
            public string[] GetSessionsItemsKeys(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ViewSessionKeys, "*", sessionId, null, null, 0);
                return SendDuplexStream<string[]>(message, OnFault);
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Reply, text, text, null, null, 0);
                return SendDuplexStream<string>(message, OnFault);
            }
            #endregion
        }
        #endregion

        #region SyncApi
        /// <summary>
        /// Represent a sync cache api.
        /// </summary>
        public class SyncApi:RemoteCacheApi
        {
            internal SyncApi() { }

            #region internal sync command

            //internal string GetKey(string itemName, string[] keys)
            //{
            //    string key = keys == null ? "*" : ComplexArgs.GetInfo(itemName, keys);
            //    return key;
            //}

            #endregion

            protected void OnFault(string message)
            {
                Console.WriteLine("SyncApi Fault: " + message);
            }

            #region  public SyncCacheApi commands
           
             
            /// <summary>
            /// Get item from sync cache using <see cref="ComplexKey"/> an <see cref="Type"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public object Get(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, info.Prefix,info.Suffix, null,null, 0);
                return SendDuplexStreamValue(message, OnFault);
            }

            /// <summary>
            /// Get item from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public object Get(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, entityName, KeySet.Join(keys), null,null, 0);
                return SendDuplexStreamValue(message,OnFault);
            }

            /// <summary>
            /// Get item from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="info"></param>
            /// <returns></returns>
            public T Get<T>(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, info.Prefix, info.Suffix, null, null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, entityName, KeySet.Join(keys), null, null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);
            }

            /// <summary>
            ///  Get item as <see cref="IDictionary"/> from sync cache using pipe name and item key.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public IDictionary GetRecord(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, entityName, KeySet.Join(keys), null, null, 0);
                return SendDuplexStream<IDictionary>(message, OnFault); 
            }


            /// <summary>
            /// Get item as <see cref="IDictionary"/> from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public IDictionary GetRecord(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, info.Prefix, info.Suffix, null, null, 0);
                return SendDuplexStream<IDictionary>(message, OnFault);
            }

            /// <summary>
            /// Get item value from cache as json.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="format"></param>
            /// <returns></returns>
            public string GetJson(ComplexKey info, JsonFormat format)
            {
                var stream = GetAs(info);
                return CacheApi.StreamToJson(stream, format);
            }

            /// <summary>
            /// Get item value from cache as json.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <param name="format"></param>
            /// <returns></returns>
            public string GetJson(string entityName, string[] keys, JsonFormat format)
            {
                var stream = GetAs(entityName, keys);
                return CacheApi.StreamToJson(stream, format);
            }

            /// <summary>
            /// Get item as stream from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public NetStream GetAs(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAs, info.Prefix, info.Suffix, null, null, 0);
                return SendDuplexStream<NetStream>(message, OnFault);
            }

            /// <summary>
            /// Get item as stream from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public NetStream GetAs(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAs, entityName, KeySet.Join(keys), null, null, 0);
                return SendDuplexStream<NetStream>(message, OnFault);
            }

            /// <summary>
            /// Get item as entity from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="info"></param>
            /// <returns></returns>
            public T GetEntity<T>(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, info.Prefix, info.Suffix, null, null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);
            }

            /// <summary>
            /// Get item as entity from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public T GetEntity<T>(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, entityName, KeySet.Join(keys), null, null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);
            }

            public string[] GetEntityPrimaryKey(string entityName)
            {
                if (string.IsNullOrWhiteSpace(entityName))
                {
                    throw new ArgumentNullException("entityName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityPrimaryKey, entityName );
                {
                    return SendDuplexStream<string[]>(message, OnFault);
                }
            }
            public T FindEntity<T>(string entityName, NameValueArgs nameValue)
            {
                if (string.IsNullOrWhiteSpace(entityName))
                {
                    throw new ArgumentNullException("entityName is required");
                }
                if (nameValue == null)
                {
                    throw new ArgumentNullException("nameValue is required");
                }
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.FindEntity, entityName);
                message.Args = nameValue;
                {
                    return SendDuplexStream<T>(message, OnFault);
                }
            }

            /// <summary>
            /// Reset all items in sync cache
            /// </summary>
            public void Reset()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Reset, "*", null, null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Refresh all items in sync cache
            /// </summary>
            public void RefreshAll()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.RefreshAll, "*", null, null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Refresh specified item in sync cache.
            /// </summary>
            /// <param name="syncName"></param>
            public void Refresh(string syncName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Refresh, syncName, null, null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Refresh specified item in sync cache.
            /// </summary>
            /// <param name="syncName"></param>
            public void Remove(string syncName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Remove, syncName, null, null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Get entity count from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public int GetEntityItemsCount(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityItemsCount, entityName, null, 0);
                return SendDuplexStream<int>(message, OnFault);
            }
            /// <summary>
            /// Get entity values as <see cref="GenericKeyValue"/> array from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public GenericKeyValue GetEntityItems(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityItems, entityName, null, 0);
                return SendDuplexStream<GenericKeyValue>(message, OnFault);
            }

            /// <summary>
            /// Get entity keys from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public string[] GetEntityKeys(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityKeys, entityName, null, 0);
                return SendDuplexStream<string[]>(message, OnFault);
            }

            /// <summary>
            /// Get all entity names from sync cache.
            /// </summary>
            /// <returns></returns>
            public string[] GetAllEntityNames()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAllEntityNames, "*", null, 0);
                return SendDuplexStream<string[]>(message, OnFault);
            }

            /// <summary>
            /// Get entity items report from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public DataTable GetItemsReport(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAllEntityNames, entityName, null, 0);
                return SendDuplexStream<DataTable>(message, OnFault);
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Reply, text, text, 0);
                return SendDuplexStream<string>(message, OnFault);
            }

            /// <summary>
            /// Get if sync cache contains item using arguments.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public bool Contains(string entityName, string[] keys)
            {
                if (string.IsNullOrWhiteSpace(entityName))
                {
                    throw new ArgumentNullException("entityName is required");
                }
                if (keys == null)
                {
                    throw new ArgumentNullException("keys is required");
                }
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, entityName, KeySet.Join(keys), null, null, 0);
                {
                    return SendDuplexState(message) == RemoteCacheState.Ok;
                }
            }
            #endregion
        }
        #endregion

        #region DataApi
        /// <summary>
        /// Represent a sync cache api.
        /// </summary>
        public class DataApi : RemoteCacheApi
        {
            internal DataApi() { }

            #region internal sync command

            //internal string GetKey(string itemName, string[] keys)
            //{
            //    string key = keys == null ? "*" : ComplexArgs.GetInfo(itemName, keys);
            //    return key;
            //}

            #endregion

            protected void OnFault(string message)
            {
                Console.WriteLine("SyncApi Fault: " + message);
            }

            #region  public DataCacheApi commands


            /// <summary>
            /// Get item from sync cache using <see cref="ComplexKey"/> an <see cref="Type"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public object Get(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Get, info.ToString(), null, 0);
                return SendDuplexStreamValue(message, OnFault);
            }

            #region items

            /// <summary>
            /// Add Value into specific row and column in local data table  
            /// </summary>
            /// <param name="db">db name</param>
            /// <param name="tableName">table name</param>
            /// <param name="column">column name</param>
            /// <param name="primaryKey">primary Key</param>
            /// <param name="value">value to set</param>
            public void AddValue(string db, string tableName, string column, string primaryKey, object value)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                if (string.IsNullOrWhiteSpace(column))
                {
                    throw new ArgumentNullException("column is required");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value is required");
                }

                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Add, db, BinarySerializer.ConvertToStream(value), 0);
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.Column, column, KnowsArgs.Pk, primaryKey);
                }
                {
                    SendOut(message);
                }
            }

            /// <summary>
            /// Set Value into specific row and column in local data table  
            /// </summary>
            /// <param name="db">db name</param>
            /// <param name="tableName">table name</param>
            /// <param name="column">column name</param>
            /// <param name="primaryKey">primary Key</param>
            /// <param name="value">value to set</param>
            public void SetValue(string db, string tableName, string column, string primaryKey, object value)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                if (string.IsNullOrWhiteSpace(column))
                {
                    throw new ArgumentNullException("column is required");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Set, db, BinarySerializer.ConvertToStream(value), 0);
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.Column, column, KnowsArgs.Pk, primaryKey);
                }
                {
                    SendOut(message);
                }
            }

            /// <summary>
            /// Get single value from storage by filter Expression,if no rows found by filter Expression return null.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="db">db name</param>
            /// <param name="tableName">table name</param>
            /// <param name="column">column name</param>
            /// <param name="primaryKey">primary Key</param>
            /// <returns></returns>
            /// <example><code>
            /// //Get value from data cache.
            ///public void GetValue()
            ///{
            ///    <![CDATA[string val = TcpDataCacheApi.GetValue<string>(db, tableName, "FirstName", "ContactID=1");]]>
            ///    Console.WriteLine(val);
            ///}
            /// </code></example>
            public T GetValue<T>(string db, string tableName, string column, string primaryKey)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                if (string.IsNullOrWhiteSpace(column))
                {
                    throw new ArgumentNullException("column is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Get, db,0 );
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.Column, column, KnowsArgs.Pk, primaryKey);
                }
                {
                    return SendDuplexStream<T>(message, OnFault);
                }
            }

            /// <summary>
            /// Get single value from storage by filter Expression,if no rows found by filter Expression return null.
            /// </summary>
            /// <param name="db">db name</param>
            /// <param name="tableName">table name</param>
            /// <param name="column">column name</param>
            /// <param name="primaryKey">primary Key</param>
            /// <returns>object value</returns>
            public object GetValue(string db, string tableName, string column, string primaryKey)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                if (string.IsNullOrWhiteSpace(column))
                {
                    throw new ArgumentNullException("column is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Get, db, 0);
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.Column, column, KnowsArgs.Pk, primaryKey);
                }
                {
                    return SendDuplexStreamValue(message, OnFault);
                }
            }


            /// <summary>
            /// Get single data row from storage by filter Expression,if no rows found by filter Expression return null.
            /// </summary>
            /// <param name="db">db name</param>
            /// <param name="tableName">table name</param>
            /// <param name="primaryKey">primary Key</param>
            /// <returns>Hashtable object</returns>
            /// <example><code>
            /// //Get item record from data cache as Dictionary.
            ///public void GetRecord()
            ///{
            ///    string key = "1";
            ///    var item = TcpDataCacheApi.GetRow(db, tableName, "ContactID=1");
            ///    if (item == null)
            ///        Console.WriteLine("item not found " + key);
            ///    else
            ///        Console.WriteLine(item["FirstName"]);
            ///}
            /// </code></example>
            public IDictionary GetRecord(string db, string tableName, string primaryKey)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetRecord, db, 0);
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.TableName, tableName, KnowsArgs.Pk, primaryKey);
                }
                {
                    return SendDuplexStream<IDictionary>(message, OnFault);
                }
            }

            ///// <summary>
            ///// Get DbTable from storage by table name.
            ///// </summary>
            ///// <param name="db">db name</param>
            ///// <param name="tableName">table name</param>
            ///// <returns>DataTable</returns>
            //public DbTable GetTable(string db, string tableName)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetTable, db, 0);
            //    {
            //        message.Args = MessageStream.CreateArgs(KnowsArgs.TableName, tableName);
            //    }
            //    {
            //        return SendDuplexStream<DbTable>(message, OnFault);
            //    }
            //}


            /// <summary>
            /// Remove data table  from storage
            /// </summary>
            /// <param name="db">db name</param>
            /// <param name="tableName">table name</param>
            /// <example><code>
            /// //Remove data table from data cache.
            ///public void RemoveItem()
            ///{
            ///    TcpDataCacheApi.RemoveTable(db, tableName);
            ///}
            /// </code></example>
            public void RemoveTable(string db, string tableName)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.RemoveTable, db, tableName,null,null, 0, TransformType.Object, false);
                {
                    SendOut(message);
                }
            }

            ///// <summary>
            ///// GetItemProperties
            ///// </summary>
            ///// <param name="db"></param>
            ///// <param name="tableName"></param>
            ///// <returns></returns>
            //public DataCacheItem GetDataItem(string db, string tableName)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetDataItem, ComplexKey.Get(db, tableName).ToString(), 0);
            //    {
            //        return SendDuplexStream<DataCacheItem>(message, OnFault);
            //    }
            //}


            /// <summary>
            /// Add Remoting Data Item to cache
            /// </summary>
            /// <param name="db"></param>
            /// <param name="dt"></param>
            /// <param name="tableName"></param>
            /// <param name="mappingName"></param>
            /// <param name="primaryKey"></param>
            public RemoteCacheState AddTable(string db, DataTable dt, string tableName, string mappingName, string[] primaryKey)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                if (dt == null)
                {
                    throw new ArgumentNullException("dt is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.AddTable, db, BinarySerializer.ConvertToStream(dt), 0);
                {
                    //message.TypeName = typeof(DataTable).FullName;
                    //message.Detail = primaryKey.JoinTrim();
                    message.Args = NameValueArgs.Get(KnowsArgs.TableName, tableName, KnowsArgs.MappingName, mappingName, KnowsArgs.Pk, primaryKey.JoinTrim());
                }
                {
                    return SendDuplexState(message);
                }
            }


            /// <summary>
            /// Set Remoting Data Item to cache
            /// </summary>
            /// <param name="db"></param>
            /// <param name="dt"></param>
            /// <param name="tableName"></param>
            /// <param name="mappingName"></param>
            /// <param name="primaryKey"></param>
            public RemoteCacheState SetTable(string db, DataTable dt, string tableName, string mappingName, string[] primaryKey)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                if (dt == null)
                {
                    throw new ArgumentNullException("dt is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.SetTable, db, BinarySerializer.ConvertToStream(dt), 0);
                {
                    //message.TypeName = typeof(DbTable).FullName;
                    //message.Detail = primaryKey.JoinTrim();
                    message.Args = NameValueArgs.Get(KnowsArgs.TableName, tableName, KnowsArgs.MappingName, mappingName, KnowsArgs.Pk, primaryKey.JoinTrim());
                }
                {
                    return SendDuplexState(message);
                }
            }

            ///// <summary>
            ///// Add Remoting Data Item to cache include SyncTables.
            ///// </summary>
            ///// <param name="db"></param>
            ///// <param name="dt"></param>
            ///// <param name="tableName"></param>
            ///// <param name="mappingName"></param>
            ///// <param name="sourceName"></param>
            ///// <param name="syncType"></param>
            ///// <param name="ts"></param>
            //public void AddTableWithSync(string db, DataTable dt, string tableName, string mappingName, string[] sourceName, SyncType syncType, TimeSpan ts)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(mappingName))
            //    {
            //        throw new ArgumentNullException("mappingName is required");
            //    }
            //    if (sourceName == null)
            //    {
            //        throw new ArgumentNullException("sourceName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.AddTableWithSync, db, BinarySerializer.ConvertToStream(dt), 0);
            //    {
            //        message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.MappingName, mappingName, KnowsArgs.SourceName, GenericNameValue.JoinArg(sourceName), KnowsArgs.SyncType, ((int)syncType).ToString(), KnowsArgs.SyncTime, ts.ToString());
            //    }
            //    {
            //        SendOut(message);
            //    }
            //}

            ///// <summary>
            ///// Add Remoting Data Item to cache include SyncTables.
            ///// </summary>
            ///// <param name="db"></param>
            ///// <param name="dt"></param>
            ///// <param name="tableName"></param>
            ///// <param name="mappingName"></param>
            ///// <param name="syncType"></param>
            ///// <param name="ts"></param>
            //public void AddTableWithSync(string db, DataTable dt, string tableName, string mappingName, Nistec.Caching.SyncType syncType, TimeSpan ts)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(mappingName))
            //    {
            //        throw new ArgumentNullException("mappingName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.AddTableWithSync, db, BinarySerializer.ConvertToStream(dt), 0);
            //    {
            //        message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.MappingName, mappingName, KnowsArgs.SourceName, mappingName, KnowsArgs.SyncType, ((int)syncType).ToString(), KnowsArgs.SyncTime, ts.ToString());
            //    }
            //    {
            //        SendOut(message);
            //    }
            //}

            ///// <summary>
            ///// Add Item to SyncTables
            ///// </summary>
            ///// <param name="db"></param>
            ///// <param name="tableName"></param>
            ///// <param name="mappingName"></param>
            ///// <param name="syncType"></param>
            ///// <param name="ts"></param>
            //public void AddSyncItem(string db, string tableName, string mappingName, Nistec.Caching.SyncType syncType, TimeSpan ts)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(mappingName))
            //    {
            //        throw new ArgumentNullException("mappingName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.AddSyncItem, db, 0);
            //    {
            //        message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.MappingName, mappingName, KnowsArgs.SourceName, mappingName, KnowsArgs.SyncType, ((int)syncType).ToString(), KnowsArgs.SyncTime, ts.ToString());
            //    }
            //    {
            //        SendOut(message);
            //    }
            //}

            ///// <summary>
            ///// Add Item to SyncTables
            ///// </summary>
            ///// <param name="db"></param>
            ///// <param name="tableName"></param>
            ///// <param name="mappingName"></param>
            ///// <param name="sourceName"></param>
            ///// <param name="syncType"></param>
            ///// <param name="ts"></param>
            //public void AddSyncItem(string db, string tableName, string mappingName, string[] sourceName, Nistec.Caching.SyncType syncType, TimeSpan ts)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(mappingName))
            //    {
            //        throw new ArgumentNullException("mappingName is required");
            //    }
            //    if (sourceName == null)
            //    {
            //        throw new ArgumentNullException("sourceName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.AddSyncItem, db, 0);
            //    {
            //        message.Args = MessageStream.CreateArgs(KnowsArgs.ConnectionKey, db, KnowsArgs.TableName, tableName, KnowsArgs.MappingName, mappingName, KnowsArgs.SourceName, GenericNameValue.JoinArg(sourceName), KnowsArgs.SyncType, ((int)syncType).ToString(), KnowsArgs.SyncTime, ts.ToString());
            //    }
            //    {
            //        SendOut(message);
            //    }
            //}
            #endregion


            /// <summary>
            /// Reply for test
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ArgumentNullException("text is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Reply, text, 0);
                {
                    return SendDuplexStream<string>(message, OnFault);
                }
            }
            /// <summary>
            /// Reset all items in sync cache
            /// </summary>
            /// <param name="db"></param>
            public void Reset(string db)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Reset, db, 0);
                {
                    SendOut(message);
                }
            }

            /// <summary>
            /// Refresh specified item in sync cache.
            /// </summary>
            /// <param name="db"></param>
            /// <param name="tableName"></param>
            public void Refresh(string db, string tableName)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Refresh, db, tableName, null,null, 0, TransformType.Object, false);
                {
                    SendOut(message);
                }
            }

            ///// <summary>
            ///// Get entity values array from sync cache using entityName.
            ///// </summary>
            ///// <param name="db"></param>
            ///// <param name="tableName"></param>
            ///// <returns></returns>
            //public KeyValuePair<string, GenericEntity>[] GetEntityItems(string db, string tableName)
            //{
            //    if (string.IsNullOrWhiteSpace(db))
            //    {
            //        throw new ArgumentNullException("db is required");
            //    }
            //    if (string.IsNullOrWhiteSpace(tableName))
            //    {
            //        throw new ArgumentNullException("tableName is required");
            //    }
            //    IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetEntityItems, ComplexKey.Get(db, tableName).ToString(), 0);
            //    {
            //        return SendDuplexStream<KeyValuePair<string, GenericEntity>[]>(message, OnFault);
            //    }
            //}

            /// <summary>
            /// Get entity count from sync cache using entityName.
            /// </summary>
            /// <param name="db"></param>
            /// <param name="tableName"></param>
            /// <returns></returns>
            public int GetEntityItemsCount(string db, string tableName)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetEntityItemsCount, db, tableName,null, null, 0);
                {
                    return SendDuplexStream<int>(message, OnFault);
                }
            }

            /// <summary>
            /// Get entity keys from sync cache using entityName.
            /// </summary>
            /// <param name="db"></param>
            /// <param name="tableName"></param>
            /// <returns></returns>
            public ICollection<string> GetEntityKeys(string db, string tableName)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetEntityKeys, db, tableName, null, null, 0);
                {
                    return SendDuplexStream<ICollection<string>>(message, OnFault);
                }
            }

            /// <summary>
            /// Get all entity names from sync cache.
            /// </summary>
            /// <param name="db"></param>
            /// <returns></returns>
            public IEnumerable<string> GetAllEntityNames(string db)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetAllEntityNames, db, 0);
                {
                    return SendDuplexStream<ICollection<string>>(message, OnFault);
                }


            }

            /// <summary>
            /// Get entity items report from sync cache using entityName.
            /// </summary>
            /// <param name="db"></param>
            /// <param name="tableName"></param>
            /// <returns></returns>
            public DataTable GetItemsReport(string db, string tableName)
            {
                if (string.IsNullOrWhiteSpace(db))
                {
                    throw new ArgumentNullException("db is required");
                }
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new ArgumentNullException("tableName is required");
                }
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetItemsReport, db, tableName, null, null, 0);
                {
                    return SendDuplexStream<DataTable>(message, OnFault);
                }
            }
            #endregion
        }
        #endregion
    }
}