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
    /// Represent cache api for client.
    /// </summary>
    public class CacheApi 
    {


        NetProtocol protocol = CacheApi.DefaultProtocol;
        string hostAddress;
        int port;
        int readTimeout;
        bool useConfig;

        static CacheApi()
        {
            IsRemoteAsync = CacheSettings.IsRemoteAsync;
            EnableRemoteException = CacheSettings.EnableRemoteException;
            SessionTimeout = CacheSettings.SessionTimeout;
        }
        public static CacheApi Get(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = CacheSettings.Protocol;
            }
            return new CacheApi() {useConfig=true, protocol = protocol };
        }
        public static CacheApi GetTcp(string hostAddress, int port, int readTimeout)
        {
            return new CacheApi() { useConfig = false, hostAddress = hostAddress, port = port, readTimeout = readTimeout, protocol = NetProtocol.Tcp };
        }
        public static CacheApi GetHttp(string hostAddress, string method, int readTimeout)
        {
            return new CacheApi() { useConfig = false, hostAddress = hostAddress, port = CacheApi.HttpMethodToPort(method), readTimeout = readTimeout, protocol = NetProtocol.Http };
        }
        public static CacheApi GetPipe(string hostAddress, int readTimeout)
        {
            return new CacheApi() { useConfig = false, hostAddress = hostAddress, port = 0, readTimeout = readTimeout, protocol = NetProtocol.Pipe };
        }
                
        /// <summary>
        /// Get or Set if the client use async method.
        /// </summary>
        public static bool IsRemoteAsync = false;
        /// <summary>
        /// Get or Set if the client will throw exeption on error.
        /// </summary>
        public static bool EnableRemoteException = false;
        /// <summary>
        /// Get or Set The session timeout.
        /// </summary>
        public static int SessionTimeout = CacheDefaults.DefaultSessionTimeout;

        internal enum EnumEmpty { NA };
        /// <summary>
        /// Default Protocol
        /// </summary>
        public const NetProtocol DefaultProtocol = NetProtocol.Tcp;
                
        #region internal methods
        internal static Type TypeEmpty = typeof(EnumEmpty);
        internal enum HostType { Cache, Sync, Session, Data };
        
        internal static string PortToHttpMethod(int port)
        {
            return (port > 0) ? "get" : "post";
        }
        internal static int HttpMethodToPort(string method)
        {
            return (method.ToLower() == "post") ? 0 : 1;
        }
        #endregion
        public static string ToJson(NetStream stream, JsonFormat format)
        {
            using (BinaryStreamer streamer = new BinaryStreamer(stream))
            {
                var obj = streamer.Decode();
                if (obj == null)
                    return null;
                else
                    return JsonSerializer.Serialize(obj, null, format);
            }
        }


        #region create message

        internal static IMessage CreateMessage(string command, string key, NetProtocol protocol)
        {
            return CreateMessage(command, key, null, TypeEmpty, null, protocol);
        }

        internal static IMessage CreateMessage(string command, string key, Type type, NetProtocol protocol)
        {
            return CreateMessage(command, key, null, type, null, protocol);
        }

        internal static IMessage CreateMessage(string command, string key, string id, Type type, string[] args, NetProtocol protocol)
        {
            if (type == null)
                throw new ArgumentNullException("CreateMessage.type");

            string typeName = type == TypeEmpty ? "*" : type.FullName;

            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("CreateMessage.key");

            IMessage message = null;
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    message = new TcpMessage()
                    {
                        Command = command,
                        Key = key,
                        TypeName = typeName
                    };
                    break;
                case NetProtocol.Pipe:
                    message = new PipeMessage()
                    {
                        Command = command,
                        Key = key,
                        TypeName = typeName
                    };
                    break;
                case NetProtocol.Http:
                    message = new HttpMessage()
                    {
                        Command = command,
                        Key = key,
                        TypeName = typeName
                    };
                    break;
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
            if (id != null)
                message.Id = id;
            if (args != null)
                message.Args = MessageStream.CreateArgs(args);

            return message;
        }
        #endregion

        #region static internal

        internal static object SendDuplex(IMessage message, string hostAddress, int port, int readTimeout, NetProtocol protocol)
        {

            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        return TcpClient.SendDuplex(message as TcpMessage,
                            hostAddress, port, readTimeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Http:
                    {
                        return HttpClient.SendDuplex(message as HttpMessage,
                            hostAddress, PortToHttpMethod(port), readTimeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex(message as PipeMessage,
                            hostAddress,
                            CacheApi.IsRemoteAsync,
                            CacheApi.EnableRemoteException);
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
        }

        internal static T SendDuplex<T>(IMessage message, string hostAddress, int port, int readTimeout, NetProtocol protocol)
        {
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        return TcpClient.SendDuplex<T>(message as TcpMessage,
                            hostAddress, port, readTimeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Http:
                    {
                        return HttpClient.SendDuplex<T>(message as HttpMessage,
                            hostAddress, PortToHttpMethod(port), readTimeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex<T>(message as PipeMessage,
                            hostAddress,
                            CacheApi.IsRemoteAsync,
                            CacheApi.EnableRemoteException);
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
        }

        internal static void SendOut(IMessage message, string hostAddress, int port, int readTimeout, NetProtocol protocol)
        {
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        TcpClient.SendOut(message as TcpMessage, hostAddress, port, readTimeout, false, CacheApi.EnableRemoteException);
                        break;
                    }
                case NetProtocol.Http:
                    {
                        HttpClient.SendOut(message as HttpMessage, hostAddress, PortToHttpMethod(port), readTimeout, CacheApi.EnableRemoteException);
                        break;
                    }
                case NetProtocol.Pipe:
                    {
                        PipeClient.SendOut(message as PipeMessage,
                            hostAddress,
                            CacheApi.IsRemoteAsync,
                            CacheApi.EnableRemoteException);
                        break;
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }

        }
        #endregion

        #region Send config internal

        internal static string GetHost(HostType hostType)
        {
            string hostName = CacheSettings.RemoteCacheHostName;
            switch (hostType)
            {
                case HostType.Sync:
                    hostName = CacheSettings.RemoteSyncCacheHostName; break;
                case HostType.Session:
                    hostName = CacheSettings.RemoteSessionHostName; break;
                case HostType.Data:
                    hostName = CacheSettings.RemoteDataCacheHostName; break;
            }
            return hostName;
        }
        
       internal static object SendDuplex(IMessage message,HostType hostType, NetProtocol protocol)
       {
 
           switch (protocol)
           {
               case NetProtocol.Tcp:
                   {
                       return TcpClient.SendDuplex(message as TcpMessage,
                           GetHost(hostType),
                           CacheApi.EnableRemoteException);
                   }
               case NetProtocol.Http:
                   {
                       return HttpClient.SendDuplex(message as HttpMessage,
                           GetHost(hostType),
                           CacheApi.EnableRemoteException);
                   }
               case NetProtocol.Pipe:
                   {
                       return PipeClient.SendDuplex(message as PipeMessage,
                           GetHost(hostType),
                           CacheApi.IsRemoteAsync,
                           CacheApi.EnableRemoteException);
                   }
               default:
                   throw new ArgumentException("Protocol is not supported " + protocol.ToString());
           }
       }

       internal static T SendDuplex<T>(IMessage message, HostType hostType, NetProtocol protocol)
       {
           switch (protocol)
           {
               case NetProtocol.Tcp:
                   {
                       return TcpClient.SendDuplex<T>(message as TcpMessage,
                           GetHost(hostType),
                           CacheApi.EnableRemoteException);
                   }
               case NetProtocol.Http:
                   {
                       return HttpClient.SendDuplex<T>(message as HttpMessage,
                           GetHost(hostType),
                           CacheApi.EnableRemoteException);
                   }
               case NetProtocol.Pipe:
                   {
                       return PipeClient.SendDuplex<T>(message as PipeMessage,
                           GetHost(hostType),
                           CacheApi.IsRemoteAsync,
                           CacheApi.EnableRemoteException);
                   }
               default:
                   throw new ArgumentException("Protocol is not supported " + protocol.ToString());
           }
       }

       internal static void SendOut(IMessage message, HostType hostType, NetProtocol protocol)
       {
           switch (protocol)
           {
               case NetProtocol.Tcp:
                   {
                       TcpClient.SendOut(message as TcpMessage,
                           GetHost(hostType),
                           CacheApi.EnableRemoteException);
                       break;
                   }
               case NetProtocol.Http:
                   {
                       HttpClient.SendOut(message as HttpMessage,
                           GetHost(hostType),
                           CacheApi.EnableRemoteException);
                       break;
                   }
               case NetProtocol.Pipe:
                   {
                       PipeClient.SendOut(message as PipeMessage,
                           GetHost(hostType),
                           CacheApi.IsRemoteAsync,
                           CacheApi.EnableRemoteException);
                       break;
                   }
               default:
                   throw new ArgumentException("Protocol is not supported " + protocol.ToString());
           }

       }
               internal static object Get(string command, string key, Type type, NetProtocol protocol)
       {
           IMessage message = CreateMessage(command, key, null, type, null, protocol);
           return SendDuplex(message, HostType.Cache, protocol);
       }

       internal static T Get<T>(string command, string key, NetProtocol protocol)
       {
           IMessage message = CreateMessage(command, key, null, typeof(T), null, protocol);
           return SendDuplex<T>(message, HostType.Cache, protocol);
       }

       internal static void Do(string command, string key, string[] keyValue, NetProtocol protocol)
       {
           IMessage message = CreateMessage(command, key, null, TypeEmpty, keyValue, protocol);
           SendOut(message,HostType.Cache,  protocol);
       }

       internal static object Set(string command, string key, string id, object value, int expiration, NetProtocol protocol)
       {
           if (value == null)
               return KnownCacheState.ArgumentsError;
           IMessage message = CreateMessage(command, key, id, value.GetType(), null, protocol);
           message.SetBody(value);
           message.Expiration = expiration;
           return SendDuplex(message, HostType.Cache, protocol);
       }
       
        #endregion


        #region internal method
        internal object Get(string command, string key, Type type)
        {
            IMessage message = CreateMessage(command, key, null, type, null, protocol);
            if (useConfig)
                return SendDuplex(message, HostType.Cache, protocol);
            return SendDuplex(message, hostAddress, port, readTimeout, protocol);
        }

        internal T Get<T>(string command, string key)
        {
            IMessage message = CreateMessage(command, key, null, typeof(T), null, protocol);
            if (useConfig)
                return SendDuplex<T>(message, HostType.Cache, protocol);
            return SendDuplex<T>(message, hostAddress, port, readTimeout, protocol);
        }

        internal void Do(string command, string key, string[] keyValue)
        {
            IMessage message = CreateMessage(command, key, null, TypeEmpty, keyValue, protocol);
            if (useConfig)
                SendOut(message, HostType.Cache, protocol);
            else
                SendOut(message, hostAddress, port, readTimeout, protocol);
        }

        internal object Set(string command, string key, string id, object value, int expiration)
        {
            if (value == null)
                return KnownCacheState.ArgumentsError;
            IMessage message = CreateMessage(command, key, id, value.GetType(), null, protocol);
            message.SetBody(value);
            message.Expiration = expiration;
            if (useConfig)
                return SendDuplex(message, HostType.Cache, protocol);
            return SendDuplex(message, hostAddress, port, readTimeout, protocol);
        }
        #endregion

        #region cache cmd host

        /// <summary>
        /// Remove item from cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns>return <see cref="CacheState"/></returns>
        /// <example>
        /// <code>
        /// //Remove item from cache.
        ///public void RemoveItem()
        ///{
        ///    var state = CacheApi.RemoveItem("item key 3");
        ///    Console.WriteLine(state);
        ///}
        /// </code>
        /// </example>
        public void RemoveItem(string cacheKey)
        {
            Do(CacheCmd.RemoveItem, cacheKey, null);
        }

        /// <summary>
        /// Get value from cache as <see cref="NetStream"/>
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// //Get item value from cache.
        ///public void GetStream()
        ///{
        ///    string key = "item key 1";
        ///    <![CDATA[var item = CacheApi.GetStream(key);]]>
        ///    Print(item, key);
        ///}
        /// </code>
        /// </example>
        public NetStream GetStream(string cacheKey)
        {
            return Get<NetStream>(CacheCmd.GetValue, cacheKey);
        }

        /// <summary>
        /// Get item value from cache as json.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="format"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public string GetJson(string cacheKey, JsonFormat format)
        {
            var obj = GetValue(cacheKey);
            if (obj == null)
                return null;
            return JsonSerializer.Serialize(obj, null, format);

            //var stream = GetStream(cacheKey, protocol);
            //return CacheApi.ToJson(stream, format);
        }

        /// <summary>
        /// Get value from cache.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public object GetValue(string cacheKey)
        {
            return Get(CacheCmd.GetValue, cacheKey, typeof(object));
        }


        /// <summary>
        /// Get value from cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// //Get item value from cache.
        ///public void GetValue()
        ///{
        ///    string key = "item key 1";
        ///    <![CDATA[var item = CacheApi.GetValue<EntitySample>(key);]]>
        ///    Print(item, key);
        ///}
        /// </code>
        /// </example>
        public T GetValue<T>(string cacheKey)
        {
            return Get<T>(CacheCmd.GetValue, cacheKey);
        }
        /// <summary>
        /// Fetch Value from cache (Cut item from cache)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// //Fetch item value from cache.
        ///public void FetchValue()
        ///{
        ///    string key = "item key 2";
        ///    <![CDATA[var item = CacheApi.FetchValue<EntitySample>(key);]]>
        ///    Print(item, key);
        ///}
        /// </code>
        /// </example>
        public T FetchValue<T>(string cacheKey)
        {
            return Get<T>(CacheCmd.FetchValue, cacheKey);
        }
        /// <summary>
        /// Add new item to cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <returns>return <see cref="CacheState"/></returns>
        /// <example>
        /// <code>
        /// //Add items to remote cache.
        ///public void AddItems()
        ///{
        ///    int timeout = 30;
        ///    CacheApi.AddItem("item key 1", new EntitySample() { Id = 123, Name = "entity sample 1", Creation = DateTime.Now, Value = "entity item one" }, timeout);
        ///    CacheApi.AddItem("item key 2", new EntitySample() { Id = 124, Name = "entity sample 2", Creation = DateTime.Now, Value = "entity item second" }, timeout);
        ///    CacheApi.AddItem("item key 3", new EntitySample() { Id = 125, Name = "entity sample 3", Creation = DateTime.Now, Value = "entity item minute" }, timeout);
        ///}
        /// </code>
        /// </example>
        public int AddItem(string cacheKey, object value, int expiration)
        {
            object o = Set(CacheCmd.AddItem, cacheKey, null, value, expiration);
            return Types.ToInt(o);
        }
        /// <summary>
        /// Add new item to cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="sessionId"></param>
        /// <param name="expiration"></param>
        /// <returns>return <see cref="CacheState"/></returns>
        public int AddItem(string cacheKey, object value, string sessionId, int expiration)
        {
            object o = Set(CacheCmd.AddItem, cacheKey, sessionId, value, expiration);
            return Types.ToInt(o);
        }


        /// <summary>
        /// Copy item in cache from source to another destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="expiration"></param>
        /// <returns>return <see cref="CacheState"/></returns>
        /// <example>
        /// <code>
        /// //Duplicate existing item from cache to a new destination.
        ///public void CopyItem()
        ///{
        ///    string source = "item key 1";
        ///    string dest = "item key 2";
        ///    var state = CacheApi.CopyItem(source, dest, timeout);
        ///    Console.WriteLine(state);
        ///}
        /// </code>
        /// </example>
        public void CopyItem(string source, string dest, int expiration)
        {
            IMessage message = CreateMessage(CacheCmd.CopyItem, dest, protocol);
            message.Args = MessageStream.CreateArgs(KnowsArgs.Source, source, KnowsArgs.Destination, dest);
            SendOut(message, hostAddress, port, readTimeout, protocol);
        }
        /// <summary>
        /// Cut item in cache from source to another destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="expiration"></param>
        /// <returns>return <see cref="CacheState"/></returns>
        /// <example>
        /// <code>
        /// //Duplicate existing item from cache to a new destination and remove the old one.
        ///public void CutItem()
        ///{
        ///    string source = "item key 2";
        ///    string dest = "item key 3";
        ///    var state = CacheApi.CutItem(source, dest, timeout);
        ///    Console.WriteLine(state);
        ///}
        /// </code>
        /// </example>
        public void CutItem(string source, string dest, int expiration)
        {
            IMessage message = CreateMessage(CacheCmd.CutItem, dest, protocol);
            message.Args = MessageStream.CreateArgs(KnowsArgs.Source, source, KnowsArgs.Destination, dest);
            SendOut(message, hostAddress, port, readTimeout, protocol);
        }

        /// <summary>
        /// Remove all session items from cache.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns>return number of items removed from cache.</returns>
        public void RemoveCacheSessionItems(string sessionId)
        {
            if (sessionId == null)
                return;
            IMessage message = CreateMessage(CacheCmd.RemoveCacheSessionItems, sessionId, protocol);
            SendOut(message, hostAddress, port, readTimeout, protocol);
        }
        /// <summary>
        /// Keep Alive Cache Item.
        /// </summary>
        /// <param name="cacheKey"></param>
        public void KeepAliveItem(string cacheKey)
        {
            if (cacheKey == null)
                return;
            IMessage message = CreateMessage(CacheCmd.KeepAliveItem, cacheKey, protocol);
            SendOut(message, hostAddress, port, readTimeout, protocol);
        }

        /// <summary>
        /// Reply for test.
        /// </summary>
        /// <returns></returns>
        public string Reply(string text)
        {
            return Get<string>(CacheCmd.Reply, text);
        }
        #endregion
    }
}