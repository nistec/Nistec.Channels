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

        /// <summary>
        /// Get<see cref="SyncApi"/> api.
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="httpMethod"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static DataApi Data(string hostAddress, int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new DataApi() { useConfig = false, hostType = HostType.Sync, protocol = protocol, hostAddress = hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod == null ? CacheApi.DefaultHttpMethod : httpMethod };
        }
        /// <summary>
        /// Get<see cref="SyncApi"/> api.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static DataApi Data(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new DataApi() { useConfig = true, hostType = HostType.Sync, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null, protocol) };
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
            IMessageStream message = MessageStream.Create(protocol, command, key, null,value, 0);
            //message.SetBody(value);
            //message.Expiration = expiration;
            //string json = JsonSerializer.Serialize(message);
            string response = SendJsonDuplex(message.ToJson(), pretty);
            return response;
        }
        public string SendJsonDuplex(string command, string key, object value, int expiration, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, null,value, expiration);
            //message.SetBody(value);
            //message.Expiration = expiration;
            //string json = JsonSerializer.Serialize(message);
            string response = SendJsonDuplex(message.ToJson(), pretty);
            return response;
        }
        public string SendJsonDuplex(string command, string key, object value, int expiration, string sessionId, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, null, value, expiration);
            message.GroupId = sessionId;
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

        #region cache message json

        public string SendHttpJsonDuplex(MessageStream message, bool pretty = false)
        {
            string response = null;

            message.TransformType = TransformType.Json;
            message.IsDuplex = true;

            response = HttpClient.SendDuplexJson(message, hostAddress, false);
            //response = HttpClientCache.SendDuplexJson(message, RemoteHostName, false);

            if (pretty)
            {
                if (response != null)
                    response = JsonSerializer.Print(response);
            }
            return response;
        }

        public void SendHttpJsonOut(MessageStream message)
        {
            HttpClient.SendOutJson(message, hostAddress, false);
            //HttpClientCache.SendOut(message, RemoteHostName, false);
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

            #region do custom
            public object DoCustom(string command, string key, string groupId, string label = null, object value = null, int expiration = 0)
            {
                switch ("cach_" + command)
                {
                    case CacheCmd.Add:
                        return Add(key, value, expiration);
                    case CacheCmd.CopyTo:
                        CopyTo(label, key, expiration);
                        return RemoteCacheState.Ok;
                    case CacheCmd.CutTo:
                        CutTo(label, key, expiration);
                        return RemoteCacheState.Ok;
                    case CacheCmd.Fetch:
                        return Fetch(key);
                    case CacheCmd.Get:
                        return Get(key);
                    //case CacheCmd.GetEntry:
                    //    return GetEntry(key);
                    case CacheCmd.GetRecord:
                        return GetRecord(key);
                    case CacheCmd.KeepAliveItem:
                        KeepAliveItem(key);
                        return RemoteCacheState.Ok;
                    //case CacheCmd.LoadData:
                    //    return LoadData();
                    case CacheCmd.Remove:
                        Remove(key);
                        return RemoteCacheState.Ok;
                    //case CacheCmd.RemoveAsync:
                    //    RemoveAsync(key);
                    //    return MessageState.Ok;
                    case CacheCmd.RemoveItemsBySession:
                        RemoveItemsBySession(key);
                        return RemoteCacheState.Ok;
                    case CacheCmd.Reply:
                        return Reply(key);
                    case CacheCmd.Set:
                        return Set(key, value, expiration);
                    //case CacheCmd.ViewEntry:
                    //    return ViewEntry(key);
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }

            public string DoHttpJson(string command, string key, string groupId = null, string label = null, object value = null, int expiration = 0, bool pretty = false)
            {
                string cmd = "cach_" + command.ToLower();
                switch (cmd)
                {
                    case CacheCmd.Add:
                        //return Add(key, value, expiration);
                        {
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw new ArgumentNullException("key is required");
                            }
                            var msg = new GenericMessage() { Command = cmd, Id = key, GroupId = groupId, Expiration = expiration };
                            msg.SetBody(value);
                            return SendHttpJsonDuplex(msg, pretty);
                        }
                    case CacheCmd.CopyTo:
                    //return CopyTo(key, detail, expiration);
                    case CacheCmd.CutTo:
                        //return CutTo(key, detail, expiration);
                        {
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw new ArgumentNullException("key is required");
                            }
                            var msg = new GenericMessage() { Command = cmd, Id = key, GroupId = groupId, Expiration = expiration };
                            msg.SetBody(value);
                            msg.Args = MessageStream.CreateArgs(KnowsArgs.Source, label, KnowsArgs.Destination, key);
                            return SendHttpJsonDuplex(msg, pretty);
                        }
                    case CacheCmd.Fetch:
                    case CacheCmd.Get:
                    case CacheCmd.GetEntry:
                    case CacheCmd.GetRecord:
                    case CacheCmd.RemoveItemsBySession:
                    case CacheCmd.Reply:
                    case CacheCmd.Remove:
                    case CacheCmd.ViewEntry:
                        {
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw new ArgumentNullException("key is required");
                            }
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Id = key }, pretty);
                        }
                    case CacheCmd.KeepAliveItem:
                    case CacheCmd.RemoveAsync:
                        {
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw new ArgumentNullException("key is required");
                            }
                            SendHttpJsonOut(new GenericMessage() { Command = cmd, Id = key });
                            return MessageState.Ok.ToString();
                        }

                    //case CacheCmd.LoadData:
                    //    return LoadData();
                    case CacheCmd.Set:
                        //return Set(key, value, expiration);
                        {
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw new ArgumentNullException("key is required");
                            }

                            if (value == null)
                            {
                                throw new ArgumentNullException("value is required");
                            }
                            var message = new GenericMessage(cmd, key, value, expiration);
                            return SendHttpJsonDuplex(message, pretty);
                        }
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }
            #endregion

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
            public object Fetch(string cacheKey)
            {
                if (string.IsNullOrWhiteSpace(cacheKey))
                {
                    throw new ArgumentNullException("cacheKey is required");
                }
                var message = new GenericMessage() { Command = CacheCmd.Fetch, Id = cacheKey };
                return SendDuplexStreamValue(message, OnFault);
            }
            /// <summary>
            /// Set a new item to the cache, if this item is exists override it with the new one.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns>return RemoteCacheState</returns>
            public RemoteCacheState Set(string cacheKey, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Set, cacheKey, null, value, expiration);
                return SendDuplexState(message);
            }
            /// <summary>
            ///  Set a new item to the cache, if this item is exists override it with the new one.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <returns>return RemoteCacheState</returns>
            public RemoteCacheState Set(string cacheKey, object value, string sessionId, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Set, cacheKey, null, value, expiration);
                message.GroupId = sessionId;
                return SendDuplexState(message);
            }
            /// <summary>
            /// Add a new item to the cache, only if this item not exists.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns>return RemoteCacheState</returns>
            public RemoteCacheState Add(string cacheKey, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Add, cacheKey, null, value, expiration);
                return SendDuplexState(message);
            }
            /// <summary>
            /// Add a new item to the cache, only if this item not exists.
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <returns>return RemoteCacheState</returns>
            public RemoteCacheState Add(string cacheKey, object value, string sessionId, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Add, cacheKey, null, value,  expiration);
                message.GroupId = sessionId;
                return SendDuplexState(message);
            }


            /// <summary>
            /// Copy item in cache from source to another destination.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="dest"></param>
            /// <param name="expiration"></param>
            /// <returns>return RemoteCacheState</returns>
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

            #region do custom
            public object DoCustom(string command, string groupId, string id, object value = null, int expiration = 0)
            {
                switch ("sess_" + command)
                {
                    case SessionCmd.Add:
                        return Add(groupId, id, value, expiration);
                    case SessionCmd.ClearAll:
                        return ClearAll();
                    case SessionCmd.ClearItems:
                        return ClearItems(groupId);
                    case SessionCmd.CopyTo:
                        return CopyTo(groupId, id, ComplexKey.Get(groupId, id).ToString(), expiration);
                    case SessionCmd.CreateSession:
                        return CreateSession(groupId, expiration, null);
                    case SessionCmd.CutTo:
                        return CutTo(groupId, id, ComplexKey.Get(groupId, id).ToString(), expiration);
                    case SessionCmd.Exists:
                        return Exists(groupId, id);
                    //case SessionCmd.Fetch:
                    //    return Fetch(groupId, id);
                    case SessionCmd.FetchRecord:
                        return FetchRecord(groupId, id);
                    case SessionCmd.Get:
                        return Get(groupId, id);
                    //case SessionCmd.GetEntry:
                    //    return GetEntry(groupId, id);
                    //case SessionCmd.GetOrCreateSession:
                    //    return GetOrCreateSession(groupId);
                    //case SessionCmd.GetOrCreateRecord:
                    //    return GetOrCreateRecord(groupId, id, value, expiration);
                    case SessionCmd.GetRecord:
                        return GetRecord(groupId, id);
                    case SessionCmd.GetSessionItems:
                        return GetSessionItems(groupId);
                    case SessionCmd.Refresh:
                        return Refresh(groupId);
                    case SessionCmd.RefreshOrCreate:
                        return RefreshOrCreate(groupId);
                    case SessionCmd.Remove:
                        return Remove(groupId, id);
                    case SessionCmd.RemoveSession:
                        return RemoveSession(groupId);
                    case SessionCmd.Reply:
                        return Reply(groupId);
                    case SessionCmd.Set:
                        return Set(groupId, id, value, expiration);
                    //case SessionCmd.ViewAllSessionsKeys:
                    //    return ViewAllSessionsKeys();
                    //case SessionCmd.ViewAllSessionsKeysByState:
                    //    return ViewAllSessionsKeysByState(SessionState.Active);
                    //case SessionCmd.ViewEntry:
                    //    return ViewEntry(groupId, id);
                    //case SessionCmd.ViewSessionKeys:
                    //    return ViewSessionKeys(groupId);
                    //case SessionCmd.ViewSessionStream:
                    //    return ViewSessionStream(groupId);
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }

            public string DoHttpJson(string command, string groupId, string id, object value = null, int expiration = 0, bool pretty = false)
            {
                string cmd = "sync_" + command.ToLower();
                switch (cmd)
                {
                    case SessionCmd.Add:
                    case SessionCmd.GetOrCreateRecord:
                    case SessionCmd.Set:
                        {
                            if (string.IsNullOrWhiteSpace(groupId))
                            {
                                throw new ArgumentNullException("key is required");
                            }
                            var msg = new GenericMessage() { Command = cmd, GroupId = groupId, Id = id, Expiration = expiration };
                            msg.SetBody(value);
                            return SendHttpJsonDuplex(msg, pretty);
                        }
                    case SessionCmd.ViewAllSessionsKeys:
                    case SessionCmd.ClearAll:
                        return SendHttpJsonDuplex(new GenericMessage() { Command = cmd }, pretty);
                    case SessionCmd.GetOrCreateSession:
                    case SessionCmd.RefreshOrCreate:
                    case SessionCmd.GetSessionItems:
                    case SessionCmd.ViewSessionStream:
                    case SessionCmd.Reply:
                    case SessionCmd.RemoveSession:
                    case SessionCmd.Refresh:
                    case SessionCmd.ClearItems:
                        if (string.IsNullOrWhiteSpace(groupId))
                        {
                            throw new ArgumentNullException("groupId is required");
                        }
                        return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, GroupId = groupId }, pretty);
                    case SessionCmd.Exists:
                    case SessionCmd.Fetch:
                    case SessionCmd.FetchRecord:
                    case SessionCmd.Get:
                    case SessionCmd.GetEntry:
                    case SessionCmd.GetRecord:
                    case SessionCmd.Remove:
                    case SessionCmd.ViewEntry:
                        if (string.IsNullOrWhiteSpace(groupId))
                        {
                            throw new ArgumentNullException("groupId is required");
                        }
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            throw new ArgumentNullException("id is required");
                        }
                        return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Id = id, GroupId = groupId }, pretty);
                    case SessionCmd.CopyTo:
                        //return CopyTo(key, detail, ComplexKey.Get(key, detail).ToString(), expiration);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case SessionCmd.CreateSession:
                        {
                            if (string.IsNullOrWhiteSpace(groupId))
                            {
                                throw new ArgumentNullException("sessionId is required");
                            }
                            var message = new GenericMessage()
                            {
                                Command = SessionCmd.CreateSession,
                                GroupId = groupId,
                                Args = MessageStream.CreateArgs(KnowsArgs.UserId, "0", KnowsArgs.StrArgs, ""),
                                Expiration = expiration
                            };
                            return SendHttpJsonDuplex(message, pretty);
                        }
                    case SessionCmd.CutTo:
                        //return CutTo(key, detail, ComplexKey.Get(key, detail).ToString(), expiration);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case SessionCmd.ViewAllSessionsKeysByState:
                        //return ViewAllSessionsKeysByState(SessionState.Active);
                        {
                            var message = new GenericMessage()
                            {
                                Command = SessionCmd.ViewAllSessionsKeysByState,
                                Args = GenericMessage.CreateArgs("state", KnownSessionState.Active)
                            };
                            return SendHttpJsonDuplex(message, pretty);
                        }
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }
            #endregion
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Get, key, null, null, 0);
                message.GroupId = sessionId;
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetRecord, key, null, null, 0);
                message.GroupId = sessionId;
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.FetchRecord, key, null, null, 0);
                message.GroupId = sessionId;
                return SendDuplexStream<DynamicEntity>(message, OnFault);
            }
            /// <summary>
            /// Get item from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns></returns>
            public IDictionary<string,object> GetSessionItems(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetSessionItems, null, null, null, 0);
                message.GroupId = sessionId;
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CreateSession, "*", null, expiration);
                message.GroupId = sessionId;
                message.Args = MessageStream.CreateArgs(keyValueArgs);
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

                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Add, key, null, value,  expiration);
                message.GroupId = sessionId;
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

                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Set, key, null, value, expiration);
                message.GroupId = sessionId;
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CopyTo, key, null, null, expiration);
                message.GroupId = sessionId;
                message.Args = MessageStream.CreateArgs(new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() });
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CutTo, key, null, null,  expiration);
                message.GroupId = sessionId;
                message.Args = MessageStream.CreateArgs(new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() });
                return SendDuplexState(message);
            }

            /// <summary>
            /// Remove session from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState RemoveSession(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RemoveSession, "*", null, null, 0);
                message.GroupId = sessionId;
                return SendDuplexState(message);
            }
            /// <summary>
            /// Remove all items from specified session.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState ClearItems(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ClearItems, "*", null, null, 0);
                message.GroupId = sessionId;
                return SendDuplexState(message);
            }
            /// <summary>
            /// Remove all sessions from session cache.
            /// </summary>
            public RemoteCacheState ClearAll()
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Refresh, "*", null, null, 0);
                message.GroupId = sessionId;
                return SendDuplexState(message);
            }
            /// <summary>
            /// Refresh sfcific session in session cache or create a new session bag if not exists.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState RefreshOrCreate(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RefreshOrCreate, "*",  null, null, 0);
                message.GroupId = sessionId;
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Remove, key, null, null, 0);
                message.GroupId = sessionId;
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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Exists, key, null, null, 0);
                message.GroupId = sessionId;
                return SendDuplexState(message)== RemoteCacheState.Ok;
            }

            /// <summary>
            /// Get all sessions keys in session cache.
            /// </summary>
            /// <returns></returns>
            public string[] GetAllSessionsKeys()
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ViewAllSessionsKeys, "*", "*", null, 0);
                return SendDuplexStream<string[]> (message, OnFault);
            }
            /// <summary>
            /// Get all items keys in specified session.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns></returns>
            public string[] GetSessionsItemsKeys(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ViewSessionKeys, "*", null, null, 0);
                message.GroupId = sessionId;
                return SendDuplexStream<string[]>(message, OnFault);
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Reply, text, text, null, 0);
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

            #region do custom
            public object DoCustom(string command, string entityName, string primaryKey, string field = null, object value = null, string args = null)
            {
                NameValueArgs valArgs = null;
                if (!string.IsNullOrEmpty(args))
                {
                    var argsArray = KeySet.SplitTrim(args);
                    valArgs = NameValueArgs.Get(argsArray);
                }

                if (primaryKey != null)
                {
                    primaryKey = primaryKey.Replace(",", KeySet.Separator);
                }

                switch ("sync_" + command)
                {
                    case SyncCacheCmd.AddEntity:
                        //return AddEntity(itemName, tableName);
                        return RemoteCacheState.CommandNotSupported;
                    case SyncCacheCmd.AddSyncItem:
                        //return AddSyncItem(itemName, tableName);
                        return RemoteCacheState.CommandNotSupported;
                    case SyncCacheCmd.Contains:
                        return Contains(ComplexArgs.Parse(entityName));
                    case SyncCacheCmd.Get:
                        return Get(ComplexKey.Get(entityName, primaryKey), field);
                    case SyncCacheCmd.GetAllEntityNames:
                        return GetAllEntityNames();
                    case SyncCacheCmd.GetAs:
                        return GetAs(ComplexKey.Get(entityName, primaryKey));
                    case SyncCacheCmd.GetEntity:
                        return GetEntity<object>(ComplexKey.Get(entityName, primaryKey));
                    case SyncCacheCmd.GetEntityItems:
                        return GetEntityItems(entityName);
                    case SyncCacheCmd.GetEntityItemsCount:
                        return GetEntityItemsCount(entityName);
                    case SyncCacheCmd.GetEntityKeys:
                        return GetEntityKeys(entityName);
                    //case SyncCacheCmd.GetItemProperties:
                    //    return GetItemProperties(entityName);
                    case SyncCacheCmd.GetItemsReport:
                        return GetItemsReport(entityName);
                    case SyncCacheCmd.GetRecord:
                        return GetRecord(ComplexKey.Get(entityName, primaryKey));
                    case SyncCacheCmd.Refresh:
                        Refresh(entityName);
                        return RemoteCacheState.Ok;
                    case SyncCacheCmd.RefreshAll:
                        RefreshAll();
                        return RemoteCacheState.Ok;
                    case SyncCacheCmd.Remove:
                        Remove(entityName);
                        return RemoteCacheState.Ok;
                    case SyncCacheCmd.Reply:
                        return Reply(entityName);
                    case SyncCacheCmd.Reset:
                        Reset();
                        return RemoteCacheState.Ok;
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }


            public string DoHttpJson(string command, string entityName, string primaryKey, string field = null, object value = null, string keyValueArgs = null, bool pretty = false)
            {
                NameValueArgs valArgs = null;
                if (!string.IsNullOrEmpty(keyValueArgs))
                {
                    var argsArray = KeySet.SplitTrim(keyValueArgs);
                    valArgs = NameValueArgs.Get(argsArray);
                }

                if (primaryKey != null)
                {
                    primaryKey = primaryKey.Replace(",", KeySet.Separator);
                }

                string cmd = "sync_" + command.ToLower();
                switch (cmd)
                {
                    case SyncCacheCmd.AddEntity:
                        //return AddEntity(itemName, tableName);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case SyncCacheCmd.AddSyncItem:
                        //return AddSyncItem(itemName, tableName);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case SyncCacheCmd.Get:
                        {
                            if (string.IsNullOrWhiteSpace(entityName))
                            {
                                throw new ArgumentNullException("entityName is required");
                            }
                            //var ck = ComplexArgs.Parse(itemName);
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Label = entityName, Id = primaryKey, Args = MessageStream.CreateArgs(KnowsArgs.Column, field) }, pretty);
                        }
                    case SyncCacheCmd.GetAllEntityNames:
                        {
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd }, pretty);
                        }
                    case SyncCacheCmd.Contains:
                    case SyncCacheCmd.GetRecord:
                    case SyncCacheCmd.GetAs:
                    case SyncCacheCmd.GetEntity:
                        {
                            if (string.IsNullOrWhiteSpace(entityName))
                            {
                                throw new ArgumentNullException("entityName is required");
                            }
                            //var ck = ComplexArgs.Parse(itemName);
                            //return SendJsonDuplex(cmd, ComplexKey.Get(entityName, primeryKey), pretty);
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Label = entityName, Id = primaryKey }, pretty);
                        }
                    case SyncCacheCmd.FindEntity:
                        {
                            if (string.IsNullOrWhiteSpace(entityName))
                            {
                                throw new ArgumentNullException("entityName is required");
                            }
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Label = entityName, Args = valArgs });
                        }
                    case SyncCacheCmd.GetEntityPrimaryKey:
                    case SyncCacheCmd.GetItemsReport:
                    case SyncCacheCmd.GetItemProperties:
                    case SyncCacheCmd.GetEntityKeys:
                    case SyncCacheCmd.GetEntityItemsCount:
                    case SyncCacheCmd.GetEntityItems:
                        {
                            if (string.IsNullOrWhiteSpace(entityName))
                            {
                                throw new ArgumentNullException("entityName is required");
                            }
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Label = entityName }, pretty);
                        }
                    case SyncCacheCmd.Reply:
                    case SyncCacheCmd.Remove:
                    case SyncCacheCmd.Refresh:
                        SendHttpJsonDuplex(new GenericMessage() { Command = cmd, Label = entityName });
                        return RemoteCacheState.Ok.ToString();
                    case SyncCacheCmd.Reset:
                    case SyncCacheCmd.RefreshAll:
                        SendHttpJsonDuplex(new GenericMessage() { Command = cmd });
                        return RemoteCacheState.Ok.ToString();
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }

            #endregion
            protected void OnFault(string message)
            {
                Console.WriteLine("SyncApi Fault: " + message);
            }

            #region  public SyncCacheApi commands

            /// <summary>
            /// Get item value from sync cache using <see cref="ComplexKey"/> an <see cref="Type"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="field"></param>
            /// <returns></returns>
            public object Get(ComplexKey info, string field)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("ComplexKey is required");
                }
                using (var message = new GenericMessage()
                {
                    Command = SyncCacheCmd.Get,
                    Label = info.Prefix,// info.ToString(),
                    Id = info.Suffix,
                    Args = MessageStream.CreateArgs(KnowsArgs.Column, field)
                })
                {
                    return SendDuplexStreamValue(message, OnFault);
                }
            }
            /// <summary>
            /// Get item value from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <param name="field"></param>
            /// <returns></returns>
            public object Get(string entityName, string[] keys, string field)//, Type type)
            {
                if (string.IsNullOrWhiteSpace(entityName))
                {
                    throw new ArgumentNullException("entityName is required");
                }
                if (keys == null)
                {
                    throw new ArgumentNullException("keys is required");
                }

                using (var message = new GenericMessage()
                {
                    Command = SyncCacheCmd.Get,
                    Label = entityName,// ComplexArgs.GetInfo(entityName, keys),
                    Id = KeySet.Join(keys),
                    Args = MessageStream.CreateArgs(KnowsArgs.Column, field)
                })
                {
                    return SendDuplexStreamValue(message, OnFault);
                }
            }

            /// <summary>
            /// Get item value from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="info"></param>
            /// <returns></returns>
            public T Get<T>(ComplexKey info, string field)
            {
                if (info == null)
                {
                    throw new ArgumentNullException("ComplexKey is required");
                }
                using (var message = new GenericMessage()
                {
                    Command = SyncCacheCmd.Get,
                    Label = info.Prefix,
                    Id = info.Suffix,
                    Args = MessageStream.CreateArgs(KnowsArgs.Column, field)
                })
                {
                    return SendDuplexStream<T>(message, OnFault);
                }
            }
            /// <summary>
            /// Get item value from sync cache using arguments.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public T Get<T>(string entityName, string[] keys, string field)
            {
                if (string.IsNullOrWhiteSpace(entityName))
                {
                    throw new ArgumentNullException("entityName is required");
                }
                if (keys == null)
                {
                    throw new ArgumentNullException("keys is required");
                }
                using (var message = new GenericMessage()
                {
                    Command = SyncCacheCmd.Get,
                    Label = entityName,//ComplexArgs.GetInfo(entityName, keys)
                    Id = KeySet.Join(keys),
                    Args = MessageStream.CreateArgs(KnowsArgs.Column, field)
                })
                {
                    return SendDuplexStream<T>(message, OnFault);
                }
            }

            ///// <summary>
            ///// Get item from sync cache using <see cref="ComplexKey"/> an <see cref="Type"/>.
            ///// </summary>
            ///// <param name="info"></param>
            ///// <returns></returns>
            //public object Get(ComplexKey info)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, info.Prefix,info.Suffix, null, 0);
            //    return SendDuplexStreamValue(message, OnFault);
            //}

            ///// <summary>
            ///// Get item from sync cache using arguments.
            ///// </summary>
            ///// <param name="entityName"></param>
            ///// <param name="keys"></param>
            ///// <returns></returns>
            //public object Get(string entityName, string[] keys)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, entityName, KeySet.Join(keys), null, 0);
            //    return SendDuplexStreamValue(message,OnFault);
            //}

            ///// <summary>
            ///// Get item from sync cache using <see cref="ComplexKey"/>.
            ///// </summary>
            ///// <typeparam name="T"></typeparam>
            ///// <param name="info"></param>
            ///// <returns></returns>
            //public T Get<T>(ComplexKey info)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, info.Prefix, info.Suffix, null, 0);
            //    message.TransformType = MessageStream.GetTransformType(typeof(T));
            //    return SendDuplexStream<T>(message, OnFault);
            //}

            ///// <summary>
            ///// Get item from sync cache using arguments.
            ///// </summary>
            ///// <typeparam name="T"></typeparam>
            ///// <param name="entityName"></param>
            ///// <param name="keys"></param>
            ///// <returns></returns>
            //public T Get<T>(string entityName, string[] keys)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Get, entityName, KeySet.Join(keys), null, 0);
            //    message.TransformType = MessageStream.GetTransformType(typeof(T));
            //    return SendDuplexStream<T>(message, OnFault);
            //}

            /// <summary>
            ///  Get item as <see cref="IDictionary"/> from sync cache using pipe name and item key.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public IDictionary GetRecord(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, entityName, KeySet.Join(keys), null, 0);
                return SendDuplexStream<IDictionary>(message, OnFault); 
            }


            /// <summary>
            /// Get item as <see cref="IDictionary"/> from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public IDictionary GetRecord(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, info.Prefix, info.Suffix, null, 0);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAs, info.Prefix, info.Suffix, null, 0);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAs, entityName, KeySet.Join(keys), null, 0);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, info.Prefix, info.Suffix, null, 0);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, entityName, KeySet.Join(keys), null, 0);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Reset, "*", null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Refresh all items in sync cache
            /// </summary>
            public void RefreshAll()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.RefreshAll, "*", null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Refresh specified item in sync cache.
            /// </summary>
            /// <param name="syncName"></param>
            public void Refresh(string syncName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Refresh, syncName, null, null, 0);
                SendOutAsync(message);
            }
            /// <summary>
            /// Refresh specified item in sync cache.
            /// </summary>
            /// <param name="syncName"></param>
            public void Remove(string syncName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Remove, syncName, null, null, 0);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, entityName, KeySet.Join(keys), null, 0);
                {
                    return SendDuplexState(message) == RemoteCacheState.Ok;
                }
            }
            public bool Contains(ComplexKey keyInfo)
            {
                if (keyInfo == null)
                {
                    throw new ArgumentNullException("keyInfo is required");
                }
                using (var message = new GenericMessage()
                {
                    Command = SyncCacheCmd.Contains,
                    Label = keyInfo.Prefix,
                    Id = keyInfo.Suffix
                })
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

            #region do custom
            public object DoCustom(string command, string db, string tableName, string primaryKey, string field = null, object value = null, string args = null)
            {
                NameValueArgs valArgs = null;
                if (!string.IsNullOrEmpty(args))
                {
                    var argsArray = KeySet.SplitTrim(args);
                    valArgs = NameValueArgs.Get(argsArray);
                }
                if (primaryKey != null)
                {
                    primaryKey = primaryKey.Replace(",", KeySet.Separator);
                }
                switch ("data_" + command)
                {
                    case DataCacheCmd.Add:
                        AddValue(db, tableName, primaryKey, field, value);
                        return RemoteCacheState.Ok;
                    case DataCacheCmd.AddSyncItem:
                        //return AddSyncItem(db, tableName);
                        return RemoteCacheState.CommandNotSupported;
                    case DataCacheCmd.AddTable:
                        //return AddTable(db, tableName);
                        return RemoteCacheState.CommandNotSupported;
                    case DataCacheCmd.AddTableWithSync:
                        //return AddTableWithSync(db, tableName,valArgs[KnowsArgs.MappingName],);
                        return RemoteCacheState.CommandNotSupported;
                    //case DataCacheCmd.Contains:
                    //    return Contains(db, tableName);
                    case DataCacheCmd.Get:
                        return GetValue(db, tableName, primaryKey, field);
                    case DataCacheCmd.GetAllEntityNames:
                        return GetAllEntityNames(db);
                    //case DataCacheCmd.GetEntityItems:
                    //    return GetEntityItems(db, tableName);
                    case DataCacheCmd.GetEntityItemsCount:
                        return GetEntityItemsCount(db, tableName);
                    case DataCacheCmd.GetEntityKeys:
                        return GetEntityKeys(db, tableName);
                    //case DataCacheCmd.GetItemProperties:
                    //    return GetItemProperties(db, tableName);
                    case DataCacheCmd.GetItemsReport:
                        return GetItemsReport(db, tableName);
                    case DataCacheCmd.GetRecord:
                        return GetRecord(db, tableName, primaryKey);
                    //case DataCacheCmd.GetStream:
                    //    return GetStream(db, tableName, primaryKey);
                    //case DataCacheCmd.GetTable:
                    //    return GetTable(db, tableName);
                    case DataCacheCmd.Refresh:
                        Refresh(db, tableName);
                        return RemoteCacheState.Ok;
                    case DataCacheCmd.RemoveTable:
                        RemoveTable(db, tableName);
                        return RemoteCacheState.Ok;
                    case DataCacheCmd.Reply:
                        return Reply(db);
                    case DataCacheCmd.Reset:
                        Reset(db);
                        return RemoteCacheState.Ok;
                    case DataCacheCmd.Set:
                        SetValue(db, tableName, primaryKey, field, value);
                        return RemoteCacheState.Ok;
                    case DataCacheCmd.SetTable:
                        //return SetTable(db, tableName);
                        return RemoteCacheState.CommandNotSupported;
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }

            public string DoHttpJson(string command, string db, string tableName, string primaryKey, string field = null, object value = null, string keyValueArgs = null, bool pretty = false)
            {
                NameValueArgs valArgs = null;
                if (!string.IsNullOrEmpty(keyValueArgs))
                {
                    var argsArray = KeySet.SplitTrim(keyValueArgs);
                    valArgs = NameValueArgs.Get(argsArray);
                }

                string cmd = "data_" + command.ToLower();
                switch (cmd)
                {
                    case DataCacheCmd.Add:
                        //AddValue(db, tableName, field, primaryKey, value);
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }
                            if (string.IsNullOrWhiteSpace(tableName))
                            {
                                throw new ArgumentNullException("tableName is required");
                            }
                            if (string.IsNullOrWhiteSpace(field))
                            {
                                throw new ArgumentNullException("field is required");
                            }
                            if (string.IsNullOrWhiteSpace(primaryKey))
                            {
                                throw new ArgumentNullException("primaryKey is required");
                            }

                            if (value == null)
                            {
                                throw new ArgumentNullException("value is required");
                            }
                            var message = new GenericMessage()
                            {
                                Command = DataCacheCmd.Add,
                                GroupId = db,
                                Label = tableName,
                                Id = primaryKey,
                                Args = MessageStream.CreateArgs(KnowsArgs.Column, field),
                                BodyStream = BinarySerializer.ConvertToStream(value)
                            };
                            SendHttpJsonOut(message);
                            return RemoteCacheState.Ok.ToString();
                        }
                    case DataCacheCmd.AddSyncItem:
                        //return AddSyncItem(db, tableName);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case DataCacheCmd.AddTable:
                        //return AddTable(db, tableName);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case DataCacheCmd.AddTableWithSync:
                        //return AddTableWithSync(db, tableName,valArgs[KnowsArgs.MappingName],);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    case DataCacheCmd.Get:
                        //return GetValue(db, tableName, field, primaryKey);
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }
                            if (string.IsNullOrWhiteSpace(tableName))
                            {
                                throw new ArgumentNullException("tableName is required");
                            }
                            if (string.IsNullOrWhiteSpace(field))
                            {
                                throw new ArgumentNullException("field is required");
                            }
                            if (string.IsNullOrWhiteSpace(primaryKey))
                            {
                                throw new ArgumentNullException("primaryKey is required");
                            }

                            var message = new GenericMessage()
                            {
                                Command = DataCacheCmd.Get,
                                GroupId = db,
                                Label = tableName,
                                Id = primaryKey,
                                Args = MessageStream.CreateArgs(KnowsArgs.Column, field)
                            };
                            return SendHttpJsonDuplex(message, pretty);
                        }
                    case DataCacheCmd.GetAllEntityNames:
                    case DataCacheCmd.Reply:
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, GroupId = db }, pretty);
                        }
                    case DataCacheCmd.GetEntityItems:
                    case DataCacheCmd.GetEntityItemsCount:
                    case DataCacheCmd.GetEntityKeys:
                    case DataCacheCmd.GetItemProperties:
                    case DataCacheCmd.GetItemsReport:
                    case DataCacheCmd.GetTable:
                    case DataCacheCmd.Contains:
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }

                            if (tableName == null)
                            {
                                throw new ArgumentNullException("tableName is required");
                            }
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, GroupId = db, Label = tableName }, pretty);
                        }

                    case DataCacheCmd.GetRecord:
                    //return GetRecord(db, tableName, primaryKey);
                    case DataCacheCmd.GetStream:
                        //return GetStream(db, tableName, primaryKey);
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }
                            if (tableName == null)
                            {
                                throw new ArgumentNullException("tableName is required");
                            }
                            if (primaryKey == null)
                            {
                                throw new ArgumentNullException("primaryKey is required");
                            }
                            return SendHttpJsonDuplex(new GenericMessage() { Command = cmd, GroupId = db, Label = tableName, Args = valArgs }, pretty);
                        }
                    case DataCacheCmd.Refresh:
                    case DataCacheCmd.RemoveTable:
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }

                            if (tableName == null)
                            {
                                throw new ArgumentNullException("tableName is required");
                            }
                            SendHttpJsonOut(new GenericMessage() { Command = cmd, GroupId = db, Label = tableName });
                            return RemoteCacheState.Ok.ToString();
                        }
                    case DataCacheCmd.Reset:
                        //Reset(db);
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }
                            SendHttpJsonOut(new GenericMessage() { Command = cmd, GroupId = db });
                            return RemoteCacheState.Ok.ToString();
                        }
                    case DataCacheCmd.Set:
                        //SetValue(db, tableName, field, primaryKey, value);
                        {
                            if (string.IsNullOrWhiteSpace(db))
                            {
                                throw new ArgumentNullException("db is required");
                            }
                            if (string.IsNullOrWhiteSpace(tableName))
                            {
                                throw new ArgumentNullException("tableName is required");
                            }
                            if (string.IsNullOrWhiteSpace(field))
                            {
                                throw new ArgumentNullException("field is required");
                            }
                            if (string.IsNullOrWhiteSpace(primaryKey))
                            {
                                throw new ArgumentNullException("primaryKey is required");
                            }
                            if (value == null)
                            {
                                throw new ArgumentNullException("value is required");
                            }
                            var message = new GenericMessage()
                            {
                                Command = DataCacheCmd.Set,
                                GroupId = db,
                                Label = tableName,
                                Id = primaryKey,
                                Args = MessageStream.CreateArgs(KnowsArgs.Column, field),
                                BodyStream = BinarySerializer.ConvertToStream(value)// CacheMessageStream.EncodeBody(value)
                            };
                            SendHttpJsonOut(message);
                            return RemoteCacheState.Ok.ToString();
                        }
                    case DataCacheCmd.SetTable:
                        //return SetTable(db, tableName);
                        return RemoteCacheState.CommandNotSupported.ToString();
                    default:
                        throw new ArgumentException("Unknown command " + command);
                }
            }
            #endregion

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

                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Add, primaryKey,tableName, BinarySerializer.ConvertToStream(value), 0);
                {
                    message.GroupId = db;
                    message.Args = MessageStream.CreateArgs(KnowsArgs.Column, column);
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Set, primaryKey, tableName, BinarySerializer.ConvertToStream(value), 0);
                {
                    message.GroupId = db;
                    message.Args = MessageStream.CreateArgs(KnowsArgs.Column, column);
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Get, primaryKey, tableName,null, 0 );
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.Column, column);
                }
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Get, primaryKey, tableName, null, 0);
                {
                    message.Args = MessageStream.CreateArgs(KnowsArgs.Column, column);
                }
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetRecord, primaryKey, tableName, null, 0);
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.RemoveTable, null, tableName,null, 0);
                {
                    message.GroupId = db;
                    //message.IsDuplex = false;
                    message.DuplexType =  DuplexTypes.None;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.AddTable, primaryKey.JoinTrim(), tableName, BinarySerializer.ConvertToStream(dt), 0);
                {
                    message.GroupId = db;
                    //message.TypeName = typeof(DataTable).FullName;
                    //message.Detail = primaryKey.JoinTrim();
                    message.Args = NameValueArgs.Get(KnowsArgs.MappingName, mappingName);
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.SetTable, primaryKey.JoinTrim(), tableName,BinarySerializer.ConvertToStream(dt), 0);
                {
                    message.GroupId = db;
                    //message.TypeName = typeof(DbTable).FullName;
                    //message.Detail = primaryKey.JoinTrim();
                    message.Args = NameValueArgs.Get(KnowsArgs.MappingName, mappingName);
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Reset, null, 0);
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.Refresh, null, tableName, null, 0);
                {
                    message.GroupId = db;
                    //message.IsDuplex = false;
                    message.DuplexType =  DuplexTypes.None;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetEntityItemsCount, null, tableName,null, 0);
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetEntityKeys, null, tableName, null, 0);
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetAllEntityNames, null, 0);
                {
                    message.GroupId = db;
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
                IMessageStream message = MessageStream.Create(protocol, DataCacheCmd.GetItemsReport, null, tableName, null, 0);
                {
                    message.GroupId = db;
                    return SendDuplexStream<DataTable>(message, OnFault);
                }
            }
            #endregion
        }
        #endregion
    }
}