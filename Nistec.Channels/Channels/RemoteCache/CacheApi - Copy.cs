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
        internal const int DefaultExpiration = 0;

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
        /// <summary>
        /// Get or Set The session timeout.
        /// </summary>
        public static int SessionTimeout = RemoteCacheSettings.DefaultSessionTimeout;

        internal enum EnumEmpty { NA };
        /// <summary>
        /// Default Protocol
        /// </summary>
        public const NetProtocol DefaultProtocol = NetProtocol.Tcp;


        //internal static Type TypeEmpty = typeof(EnumEmpty);
        //internal enum HostType { Cache, Sync, Session, Data };

        //internal static string PortToHttpMethod(int port)
        //{
        //    return (port > 0) ? "get" : "post";
        //}
        //internal static int HttpMethodToPort(string method)
        //{
        //    return (method.ToLower() == "post") ? 0 : 1;
        //}
        #endregion

        #region ctor
        static RemoteCacheApi()
        {
            IsRemoteAsync = RemoteCacheSettings.IsRemoteAsync;
            EnableRemoteException = RemoteCacheSettings.EnableRemoteException;
            SessionTimeout = RemoteCacheSettings.SessionTimeout;
        }
        
        //public static CacheApi Get(NetProtocol protocol = CacheApi.DefaultProtocol)
        //{
        //    if (protocol == NetProtocol.NA)
        //    {
        //        protocol = CacheSettings.Protocol;
        //    }
        //    return new CacheApi() { useConfig = true, protocol = protocol, port = (protocol == NetProtocol.Http) ? DefaultHttpPort : DefaultTcpPort, httpMethod = CacheApi.DefaultHttpMethod };
        //}
        //public static CacheApi Get(HostType hostType, NetProtocol protocol = CacheApi.DefaultProtocol)
        //{
        //    if (protocol == NetProtocol.NA)
        //    {
        //        protocol = CacheSettings.Protocol;
        //    }
        //    return new CacheApi() { useConfig = true, hostType = hostType, protocol = protocol, port = (protocol == NetProtocol.Http) ? DefaultHttpPort : DefaultTcpPort, httpMethod = CacheApi.DefaultHttpMethod };
        //}

        public static CacheApi Cache(string hostAddress,int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new CacheApi() { useConfig = false, hostType = HostType.Cache, protocol = protocol, hostAddress=hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod==null? CacheApi.DefaultHttpMethod: httpMethod };
        }
        public static CacheApi Cache(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new CacheApi() { useConfig = true, hostType = HostType.Cache, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null,protocol) };
        }
        public static SessionApi Session(string hostAddress, int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SessionApi() { useConfig = false, hostType = HostType.Session, protocol = protocol, hostAddress = hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod == null ? CacheApi.DefaultHttpMethod : httpMethod };
        }
        public static SessionApi Session(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SessionApi() { useConfig = true, hostType = HostType.Session, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null, protocol) };
        }

        public static SyncApi Sync(string hostAddress, int port, string httpMethod, NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SyncApi() { useConfig = false, hostType = HostType.Sync, protocol = protocol, hostAddress = hostAddress, port = RemoteCacheSettings.GetPort(port, protocol), httpMethod = httpMethod == null ? CacheApi.DefaultHttpMethod : httpMethod };
        }
        public static SyncApi Sync(NetProtocol protocol = CacheApi.DefaultProtocol)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = RemoteCacheSettings.Protocol;
            }
            return new SyncApi() { useConfig = true, hostType = HostType.Sync, protocol = protocol, hostAddress = RemoteCacheSettings.GetHostAddress(null, protocol), port = RemoteCacheSettings.GetPort(0, protocol), httpMethod = RemoteCacheSettings.GetHttpMethod(null, protocol) };
        }


        //public static CacheApi GetTcp(string hostAddress, int port, int timeout)
        //{
        //    return new CacheApi() { useConfig = false, hostAddress = hostAddress, port = port, timeout = timeout, protocol = NetProtocol.Tcp };
        //}
        //public static CacheApi GetHttp(string hostAddress, int port, string method, int timeout)
        //{
        //    return new CacheApi() { useConfig = false, hostAddress = hostAddress, port = port, httpMethod = method, timeout = timeout, protocol = NetProtocol.Http };
        //}
        //public static CacheApi GetPipe(string hostAddress, int timeout)
        //{
        //    return new CacheApi() { useConfig = false, hostAddress = hostAddress, port = 0, timeout = timeout, protocol = NetProtocol.Pipe };
        //}


        #endregion


        #region Send internal



        //internal object SendDuplexAsync(IMessageStream message)//, string hostAddress, int port, string method, int timeout, NetProtocol protocol)
        //{
        //    var task = Task.Factory.StartNew(() => SendDuplex(message));//, hostAddress, port, method, timeout, protocol));
        //    task.Wait();
        //    return task.Result;
        //}
        public T SendDuplexStream<T>(IMessageStream message, Action<string> onFault)
        {
            TransStream ts = SendDuplexStream(message);
            return TransReader.ReadValue<T>(ts, onFault);
        }
        public object SendDuplexStreamValue(IMessageStream message, Action<string> onFault)
        {
            TransStream ts = SendDuplexStream(message);
            return TransReader.ReadValue(ts, onFault);
        }
        public RemoteCacheState SendDuplexState(IMessageStream message)
        {
            TransStream ts = SendDuplexStream(message);
            return (RemoteCacheState)TransReader.ReadState(ts);
        }
        public TransStream SendDuplexStream(IMessageStream message)
        {
            switch (protocol)
            {
                case NetProtocol.Http:
                    return HttpClient.SendDuplexStream(message as HttpMessage, hostAddress, port, httpMethod, timeout,
                            CacheApi.EnableRemoteException);

                case NetProtocol.Pipe:
                    return PipeClient.SendDuplexStream(message as PipeMessage, hostAddress,
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);

                case NetProtocol.Tcp:
                    break;
            }
            return TcpClient.SendDuplexStream(message as TcpMessage, hostAddress, port, timeout,
                            CacheApi.EnableRemoteException);
        }
        /*

        internal object SendDuplex(IMessageStream message)//, string hostAddress, int port, string method, int timeout, NetProtocol protocol)
        {

            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        return TcpClient.SendDuplex(message as TcpMessage,
                            hostAddress, port, timeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Http:
                    {
                        return HttpClient.SendDuplex(message as HttpMessage,
                            hostAddress, port, httpMethod, timeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex(message as PipeMessage,
                            hostAddress,
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
        }

        internal T SendDuplexAsync<T>(IMessageStream message)//, string hostAddress, int port, string method, int timeout, NetProtocol protocol)
        {
            var task = Task.Factory.StartNew<T>(() => SendDuplex<T>(message));//, hostAddress, port, method, timeout, protocol));
            task.Wait();
            return task.Result;
        }

        internal T SendDuplex<T>(IMessageStream message)//, string hostAddress, int port, string method, int timeout, NetProtocol protocol)
        {
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        return TcpClient.SendDuplex<T>(message as TcpMessage,
                            hostAddress, port, timeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Http:
                    {
                        return HttpClient.SendDuplex<T>(message as HttpMessage,
                            hostAddress, port, httpMethod, timeout,
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex<T>(message as PipeMessage,
                            hostAddress,
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
        }
        */
        internal void SendOutAsync(IMessageStream message)//, string hostAddress, int port, string method, int timeout, NetProtocol protocol)
        {
            Task.Factory.StartNew(() => SendOut(message));//, hostAddress, port, method, timeout, protocol));
        }

        internal void SendOut(IMessageStream message)//, string hostAddress, int port, string method, int timeout, NetProtocol protocol)
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
                           PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);
                        break;
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }

        }

        //internal static string GetHost(HostType hostType)
        //{
        //    string hostName = CacheSettings.RemoteCacheHostName;
        //    switch (hostType)
        //    {
        //        case HostType.Sync:
        //            hostName = CacheSettings.RemoteSyncCacheHostName; break;
        //        case HostType.Session:
        //            hostName = CacheSettings.RemoteSessionHostName; break;
        //        case HostType.Data:
        //            hostName = CacheSettings.RemoteDataCacheHostName; break;
        //    }
        //    return hostName;
        //}
        /*
        internal static object SendDuplex(IMessageStream message, HostType hostType, NetProtocol protocol)
        {

            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        return TcpClient.SendDuplex(message as TcpMessage,
                            GetHostAddress(null,protocol),
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Http:
                    {
                        return HttpClient.SendDuplex(message as HttpMessage,
                            GetHostAddress(hostType),
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex(message as PipeMessage,
                            GetHostAddress(hostType),
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
        }

        internal static T SendDuplex<T>(IMessageStream message, HostType hostType, NetProtocol protocol)
        {
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    {
                        return TcpClient.SendDuplex<T>(message as TcpMessage,
                            GetHostAddress(null, protocol),
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Http:
                    {
                        return HttpClient.SendDuplex<T>(message as HttpMessage,
                            GetHostAddress(null, protocol),
                            CacheApi.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex<T>(message as PipeMessage,
                            GetHostAddress(null, protocol),
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
        }

        internal static void SendOut(IMessageStream message, HostType hostType, NetProtocol protocol)
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
                            PipeMessage.GetPipeOptions(CacheApi.IsRemoteAsync),
                            CacheApi.EnableRemoteException);
                        break;
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }

        }

        */

        /*

        internal static object Get(string command, string key, Type type, NetProtocol protocol)
        {
            IMessageStream message = CreateMessage(command, key, null, type, null, protocol);
            return SendDuplex(message, HostType.Cache, protocol);
        }

        internal static T Get<T>(string command, string key, NetProtocol protocol)
        {
            IMessageStream message = CreateMessage(command, key, null, typeof(T), null, protocol);
            return SendDuplex<T>(message, HostType.Cache, protocol);
        }

        internal static void Do(string command, string key, string[] keyValue, NetProtocol protocol)
        {
            IMessageStream message = CreateMessage(command, key, null, TypeEmpty, keyValue, protocol);
            SendOut(message, HostType.Cache, protocol);
        }

        internal static object Set(string command, string key, string id, object value, int expiration, NetProtocol protocol)
        {
            if (value == null)
                return KnownCacheState.ArgumentsError;
            IMessageStream message = CreateMessage(command, key, id, value.GetType(), null, protocol);
            message.SetBody(value);
            message.Expiration = expiration;
            return SendDuplex(message, HostType.Cache, protocol);
        }
        */

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


        /*
        internal static IMessageStream CreateMessage(string command, string key, NetProtocol protocol)
        {
            return CreateMessage(command, key, null, TypeEmpty, null, protocol);
        }

        internal static IMessageStream CreateMessage(string command, string key, Type type, NetProtocol protocol)
        {
            return CreateMessage(command, key, null, type, null, protocol);
        }

        internal static IMessageStream CreateMessage(string command, string key, string id, Type type, string[] args, NetProtocol protocol)
        {
            if (type == null)
                throw new ArgumentNullException("CreateMessage.type");

            string typeName = type == TypeEmpty ? "*" : type.FullName;

            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("CreateMessage.key");

            IMessageStream message = null;
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
        */
        /*
        internal static IMessageStream CreateMessage(string command, string key, int expiration, NetProtocol protocol)
        {
            return CreateMessage(command, key, null, null, null, expiration,protocol);
        }
        internal static IMessageStream CreateMessage(string command, string key, object value, int expiration, NetProtocol protocol)
        {
            return CreateMessage(command, key, null, value, null, expiration, protocol);
        }
        internal static IMessageStream CreateMessage(string command, string key, string id, object value, string[] args, int expiration, NetProtocol protocol)
        {
            //if (type == null)
            //    throw new ArgumentNullException("CreateMessage.type");

            //string typeName = type == TypeEmpty ? "*" : type.FullName;

            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("CreateMessage.key");

            IMessageStream message = null;
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    message = new TcpMessage(command, key, value, expiration);
                    break;
                case NetProtocol.Pipe:
                    message = new PipeMessage(command, key, value, expiration);
                    break;
                case NetProtocol.Http:
                    message = new HttpMessage(command, key, value, expiration);
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
        */
        #endregion

        #region Common SendJson

        public string SendJsonDuplex(string command, string key, bool pretty = false)
        {
            IMessageStream message = MessageStream.Create(protocol, command, key, null, 0);
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
        public class CacheApi: RemoteCacheApi
        {
            #region json query
            /*
            public string SendJsonDuplex(string command, string key, object value, int expiration, string hostName, bool pretty = false)
            {
                IMessageStream message = CacheApi.CreateMessage(command, key, value, expiration,protocol);
                //message.SetBody(value);
                message.Expiration = expiration;
                string json = JsonSerializer.Serialize(message);
                string response = SendJsonDuplex(json, hostName, pretty);
                return response;
            }
            //public string SendJsonDuplex(string command, string key, object value, int expiration, string hostName, bool pretty = false)
            //{
            //    IMessageStream message = CacheApi.CreateMessage(command, key, value, expiration,protocol);
            //    //message.SetBody(value);
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, hostName, pretty);
            //    return response;
            //} 

            public string SendJsonDuplex(string command, string key, int expiration, string hostName, bool pretty = false)
            {
                IMessageStream message = CacheApi.CreateMessage(command, key, null, expiration, protocol);
                string json = JsonSerializer.Serialize(message);
                string response = SendJsonDuplex(json, hostName, pretty);
                return response;
            }
            public string SendJsonDuplex(string json, string hostName, bool pretty = false)
            {
                string response = null;

                switch (protocol)
                {
                    case NetProtocol.Pipe:
                        response = PipeJsonClient.SendDuplex(json, hostName, false);
                        break;
                    case NetProtocol.Tcp:
                        response = TcpJsonClient.SendDuplex(json, hostName, false);
                        break;
                    case NetProtocol.Http:
                        response = HttpJsonClient.SendDuplex(json, hostName, false);
                        break;
                }

                if (pretty)
                {
                    if (response != null)
                        response = JsonSerializer.Print(response);
                }
                return response;

            }
            */

            //public string SendJsonDuplex(string command, string key, int expiration,  bool pretty = false)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null, expiration);
            //    //string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(message.ToJson(), pretty);
            //    return response;
            //}

            #endregion

            #region internal method
            //internal object Get(string command, string key)//, Type returnType)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null, null, null, DefaultExpiration);
            //    //message.ReturnTypeName = returnType.FullName;
            //    return SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //internal T Get<T>(string command, string key)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null, null, null, DefaultExpiration);
            //    //message.ReturnTypeName = typeof(T).FullName;
            //    return SendDuplex<T>(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //internal void Do(string command, string key, string[] keyValue)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null, null, keyValue, DefaultExpiration);
            //    SendOut(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //internal object Do(string command, string key, string[] keyValue)
            //{
            //    IMessageStream message = CreateMessage(command, key, null, null, keyValue,, protocol);
            //    return SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);
            //}
            //internal MessageAck Set(string command, string key, string id, object value, int expiration)
            //{
            //    if (value == null)
            //        return new MessageAck( MessageState.ArgumentsError, "Invalid value");
            //    IMessageStream message = MessageStream.Create(protocol, command, key, id, value, null,expiration, TransformType.Ack);
            //    //IMessageStream message = CreateMessage(command, key, id, value.GetType(), null, protocol);
            //    //message.SetBody(value);
            //    //message.Expiration = expiration;
            //    return SendDuplex<MessageAck>(message, hostAddress, port, httpMethod, timeout, protocol);
            //}
            #endregion

            protected void OnFault(string message)
            {
                Console.WriteLine("CacheApi Fault: " + message);
            }

            #region public cache commands

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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.GetValue, key, null, null, null, 0);
                message.TransformType = TransformType.Stream;
                return SendDuplexStreamValue(message, OnFault);
                //return stream.ReadAckValue();
                //return BinarySerializer.ConvertFromStream(stream.Value);
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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.RemoveItem, cacheKey, null, null, null, DefaultExpiration);
                SendOut(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(CacheCmd.RemoveItem, cacheKey, null);
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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.GetValue, cacheKey, null, null, null, DefaultExpiration);
                return SendDuplexStream<NetStream>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<NetStream>(CacheCmd.GetValue, cacheKey);
            }

 

            ///// <summary>
            ///// Get value from cache.
            ///// </summary>
            ///// <param name="cacheKey"></param>
            ///// <returns></returns>
            //public object GetValue(string cacheKey)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, CacheCmd.GetValue, cacheKey, null, null, null, 0);
            //    //message.ReturnTypeName = returnType.FullName;
            //    return SendDuplexStreamValue(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

            //    //return Get(CacheCmd.GetValue, cacheKey);//, typeof(object));
            //}


            ///// <summary>
            ///// Get value from cache
            ///// </summary>
            ///// <typeparam name="T"></typeparam>
            ///// <param name="cacheKey"></param>
            ///// <returns></returns>
            ///// <example>
            ///// <code>
            ///// //Get item value from cache.
            /////public void GetValue()
            /////{
            /////    string key = "item key 1";
            /////    <![CDATA[var item = CacheApi.GetValue<EntitySample>(key);]]>
            /////    Print(item, key);
            /////}
            ///// </code>
            ///// </example>
            //public T GetValue<T>(string cacheKey)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, CacheCmd.GetValue, cacheKey, null, null, null, DefaultExpiration);
            //    return SendDuplex<T>(message);//, hostAddress, port, httpMethod, timeout, protocol);

            //    //return Get<T>(CacheCmd.GetValue, cacheKey);
            //}

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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.FetchValue, cacheKey, null, null, null, DefaultExpiration);
                return SendDuplexStream<T>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);
                //return Get<T>(CacheCmd.FetchValue, cacheKey);
            }
            /// <summary>
            /// Add new item to cache
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
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
            public RemoteCacheState AddItem(string cacheKey, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;// new MessageAck(MessageState.ArgumentsError, "Invalid value");
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.AddItem, cacheKey, null, value, null, expiration);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);


                //MessageAck ack = Set(CacheCmd.AddItem, cacheKey, null, value, expiration);
                //return ack;// Types.ToInt(o);
            }
            /// <summary>
            /// Add new item to cache
            /// </summary>
            /// <param name="cacheKey"></param>
            /// <param name="value"></param>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
            public RemoteCacheState AddItem(string cacheKey, object value, string sessionId, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;//new MessageAck(MessageState.ArgumentsError, "Invalid value");
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.AddItem, cacheKey, sessionId, value, null, expiration);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);


                //MessageAck ack = Set(CacheCmd.AddItem, cacheKey, sessionId, value, expiration);
                //return ack;// Types.ToInt(o);
            }


            /// <summary>
            /// Copy item in cache from source to another destination.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="dest"></param>
            /// <param name="expiration"></param>
            /// <returns>return CacheState</returns>
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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.CopyItem, dest, expiration);
                message.Args = MessageStream.CreateArgs(KnowsArgs.Source, source, KnowsArgs.Destination, dest);
                SendOut(message);//, hostAddress, port, httpMethod, timeout, protocol);
            }
            /// <summary>
            /// Cut item in cache from source to another destination.
            /// </summary>
            /// <param name="source"></param>
            /// <param name="dest"></param>
            /// <param name="expiration"></param>
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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.CutItem, dest, expiration);
                message.Args = MessageStream.CreateArgs(KnowsArgs.Source, source, KnowsArgs.Destination, dest);
                SendOut(message);//, hostAddress, port, httpMethod, timeout, protocol);
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
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.RemoveCacheSessionItems, sessionId, DefaultExpiration);
                SendOut(message);//, hostAddress, port, httpMethod, timeout, protocol);
            }
            /// <summary>
            /// Keep Alive Cache Item.
            /// </summary>
            /// <param name="cacheKey"></param>
            public void KeepAliveItem(string cacheKey)
            {
                if (cacheKey == null)
                    return;
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.KeepAliveItem, cacheKey, DefaultExpiration);
                SendOut(message);//, hostAddress, port, httpMethod, timeout, protocol);
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, CacheCmd.Reply, text, null, null, null, DefaultExpiration);
                return SendDuplexStream<string>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<string>(CacheCmd.Reply, text);
            }
            #endregion
        }
        #endregion

        #region SessionApi
        public class SessionApi: RemoteCacheApi
        {
            #region json query

            //public string SendJsonDuplex(string command, string key, object value, int expiration, string sessionId , bool pretty = false)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, sessionId, value, null, expiration);
            //    //message.SetBody(value);
            //    //message.Expiration = expiration;
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, pretty);
            //    return response;
            //}
            //public string SendJsonDuplex(string command, string key, object value, int expiration, bool pretty = false)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, value, expiration);
            //    //message.SetBody(value);
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, pretty);
            //    return response;
            //}

            //public string SendJsonDuplex(string command, string key, bool pretty = false)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null,DefaultExpiration);
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, pretty);
            //    return response;
            //}
            //public string SendJsonDuplex(string json, bool pretty = false)
            //{
            //    string response = null;

            //    switch (protocol)
            //    {
            //        case NetProtocol.Pipe:
            //            response = PipeJsonClient.SendDuplex(json, hostAddress);
            //            break;
            //        case NetProtocol.Tcp:
            //            response = TcpJsonClient.SendDuplex(json, hostAddress,port,timeout);
            //            break;
            //        case NetProtocol.Http:
            //            response = HttpJsonClient.SendDuplex(json, hostAddress,port,httpMethod,timeout);
            //            break;
            //    }

            //    if (pretty)
            //    {
            //        if (response != null)
            //            response = JsonSerializer.Print(response);
            //    }
            //    return response;

            //}


            #endregion

            #region internal session comand

            //internal object Get(string command, string sessionId, string key, Type returnType)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, sessionId, null, null, DefaultExpiration);
            //    message.TransformType = MessageStream.GetTransformType(returnType);
            //    return CacheApi.SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //internal T Get<T>(string command, string sessionId, string key)
            //{

            //    IMessageStream message = MessageStream.Create(protocol, command, key, sessionId, null, null, DefaultExpiration);
            //    message.TransformType = MessageStream.GetTransformType(typeof(T));
            //    return CacheApi.SendDuplex<T>(message, hostAddress, port, httpMethod, timeout, protocol);

            //}


            //internal void Do(string command, string sessionId, string key = "*", int expiration = 0, string[] keyValue = null)
            //{
            //    IMessageStream message = CacheApi.CreateMessage(command, key, sessionId, CacheApi.TypeEmpty, null, protocol);
            //    message.Expiration = expiration;

            //    CacheApi.SendOut(message, hostAddress, port, httpMethod, timeout, protocol);

            //}

            //internal object Do(string command, string sessionId, string key = "*", int expiration = 0, string[] keyValue = null)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, sessionId, null, null, expiration);
            //    message.Expiration = expiration;
            //    return CacheApi.SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);

            //}
            //internal object Set(string command, string sessionId, string key, object value, int expiration, string[] args)
            //{
            //    if (value == null)
            //        return KnownCacheState.ArgumentsError;

            //    IMessageStream message = MessageStream.Create(protocol, command, key, sessionId, value, args, expiration);
            //    //message.SetBody(value);
            //    //message.Expiration = expiration;
            //    //message.Id = sessionId;
            //    return CacheApi.SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);
            //}
            #endregion

            protected void OnFault(string message)
            {
                Console.WriteLine("SessionApi Fault: " + message);
            }

            #region public session commands

            ///// <summary>
            ///// Get item from session cache.
            ///// </summary>
            ///// <param name="sessionId"></param>
            ///// <param name="key"></param>
            ///// <param name="defaultValue"></param>
            ///// <returns></returns>
            //public string Get(string sessionId, string key, string defaultValue = null)
            //{
            //    return Types.NzOr(Get<string>(SessionCmd.GetSessionItemDictionary, sessionId, key), defaultValue);
            //}

            ///// <summary>
            ///// Get item value from cache as json.
            ///// </summary>
            ///// <param name="sessionId"></param>
            ///// <param name="key"></param>
            ///// <param name="format"></param>
            ///// <returns></returns>
            //public string GetJson(string sessionId, string key, JsonFormat format)
            //{
            //    var stream = Get<NetStream>(SessionCmd.GetSessionItemDictionary, sessionId, key);
            //    return CacheApi.ToJson(stream, format);
            //}

            ///// <summary>
            ///// Get item from session cache.
            ///// </summary>
            ///// <typeparam name="T"></typeparam>
            ///// <param name="sessionId"></param>
            ///// <param name="key"></param>
            ///// <returns></returns>
            //public T Get<T>(string sessionId, string key)
            //{
            //    return Get<T>(SessionCmd.GetSessionItemDictionary, sessionId, key);
            //}


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
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetSessionValue, key, sessionId, null, null, 0);
                message.TransformType = TransformType.Stream;
                return SendDuplexStreamValue(message, OnFault);
                //var stream = (NetStream)SendDuplex(message);
                //if (stream == null)
                //    return null;
                //return BinarySerializer.ConvertFromStream(stream);
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
            public DynamicEntity GetSessionItem(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetSessionItemRecord, key, sessionId, null, null, 0);
                return SendDuplexStream<DynamicEntity>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);


                // return Get<DynamicEntity>(SessionCmd.GetSessionItemRecord, sessionId, key);
            }

 
            /// <summary>
            ///  Fetch item from specified session cache using session id and item key.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public DynamicEntity FetchSessionItem(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.FetchSessionItemRecord, key, sessionId, null, null, 0);
                return SendDuplexStream<DynamicEntity>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<DynamicEntity>(SessionCmd.FetchSessionItemRecord, sessionId, key);
            }
            /// <summary>
            /// Get item from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns></returns>
            public DynamicEntity GetSession(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetExistingSessionRecord, null, sessionId, null, null, 0);
                return SendDuplexStream<DynamicEntity>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);
            }

            /// <summary>
            /// Add new session with CacheSettings.SessionTimeout configuration.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="expiration"></param>
            /// <param name="keyValueArgs"></param>
            public RemoteCacheState Create(string sessionId, int expiration, string[] keyValueArgs)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.AddSession, "*", sessionId, null, keyValueArgs, expiration);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.AddSession, sessionId, "*", expiration, keyValueArgs);
            }


            /// <summary>
            /// Add item to specified session (if session not exists create new one) with CacheSettings.SessionTimeout configuration.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <returns></returns>
            public RemoteCacheState AddSessionItem(string sessionId, string key, object value, int expiration)
            {
                if (value == null)
                    return RemoteCacheState.ArgumentsError;// new MessageAck(MessageState.ArgumentsError, "AddSessionItem.value is null");// KnownCacheState.ArgumentsError;

                IMessageStream message = MessageStream.Create(protocol, SessionCmd.AddSessionItem, key, sessionId, value, null, expiration);// CacheApi.SessionTimeout);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);


                //object o = Set(SessionCmd.AddSessionItem, sessionId, key, value, CacheApi.SessionTimeout, null);
                //return Types.ToInt(o);
            }


            /// <summary>
            /// Get all sessions keys in session cache using <see cref="KnownSessionState"/> state.
            /// </summary>
            /// <returns></returns>
            public string[] ReportAllStateKeys(string state)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetAllSessionsStateKeys, state, null, null, null, DefaultExpiration);
                return SendDuplexStream<string[]>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<string[]>(SessionCmd.GetAllSessionsStateKeys, "*", state);
            }

            ///// <summary>
            /////  Fetch item from specified session cache using session id and item key.
            ///// </summary>
            ///// <typeparam name="T"></typeparam>
            ///// <param name="sessionId"></param>
            ///// <param name="key"></param>
            ///// <returns></returns>
            //public T Fetch<T>(string sessionId, string key)
            //{
            //    return Get<T>(SessionCmd.FetchSessionItemDictionary, sessionId, key);
            //}

            /// <summary>
            /// Copy session item to a new place in MCache.
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
            public RemoteCacheState CopyTo(string sessionId, string key, string targetKey, int expiration, bool addToCache = false)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.CopyTo, key, sessionId, null, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() }, expiration);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.CopyTo, sessionId, key, expiration, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() });
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
            /// <example><code>
            /// //Fetch item from current session to cache.
            ///public void FetchTo()
            ///{
            ///    string key = "item key 2";
            ///    SessionApi.FetchTo(sessionId, key, key, timeout, true);
            ///}
            /// </code></example>
            public RemoteCacheState FetchTo(string sessionId, string key, string targetKey, int expiration, bool addToCache = false)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.FetchTo, key, sessionId, null, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() }, expiration);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.FetchTo, sessionId, key, expiration, new string[] { KnowsArgs.TargetKey, targetKey, KnowsArgs.AddToCache, addToCache.ToString() });
            }

            /// <summary>
            /// Remove session from session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <example><code>
            ///  //remove session with items.
            ///public void RemoveSession()
            ///{
            ///    SessionApi.RemoveSession(sessionId);
            ///}
            /// </code></example>
            public RemoteCacheState RemoveSession(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RemoveSession, "*", sessionId, null, null, 0);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.RemoveSession, sessionId, "*", 0, null);
            }
            /// <summary>
            /// Remove all items from specified session.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState Clear(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ClearSessionItems, "*", sessionId, null, null, 0);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.ClearSessionItems, sessionId, "*", 0, null);
            }
            /// <summary>
            /// Remove all sessions from session cache.
            /// </summary>
            public RemoteCacheState ClearAllSessions()
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.ClearAllSessions, "*", null, null, null, 0);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.ClearAllSessions, "*", "*", 0, null);
            }

            /// <summary>
            /// Refresh specified session in session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState Refresh(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.SessionRefresh, "*", sessionId, null, null, 0);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.SessionRefresh, sessionId, "*", 0, null);
            }
            /// <summary>
            /// Refresh sfcific session in session cache or create a new session bag if not exists.
            /// </summary>
            /// <param name="sessionId"></param>
            public RemoteCacheState RefreshOrCreate(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RefreshOrCreate, "*", sessionId, null, null, 0);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);


                //Do(SessionCmd.RefreshOrCreate, sessionId, "*", 0, null);
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
            public RemoteCacheState Remove(string sessionId, string key)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.RemoveSessionItem, key, sessionId, null, null, 0);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //Do(SessionCmd.RemoveSessionItem, sessionId, key, 0, null);
            }

            /// <summary>
            /// Add item to specified session in session cache.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="expiration"></param>
            /// <param name="validateExisting"></param>
            /// <returns></returns>
            /// <example><code>
            /// //Add items to current session.
            ///public void AddItems()
            ///{
            ///     string sessionId = "12345678";
            ///     string userId = "12";
            ///     int timeout = 0;
            ///    SessionApi.Set(sessionId, "item key 1", new EntitySample() { Id = 123, Name = "entity sample 1", Creation = DateTime.Now, Value = "entity item one" }, expiration);
            ///    SessionApi.Set(sessionId, "item key 2", new EntitySample() { Id = 124, Name = "entity sample 2", Creation = DateTime.Now, Value = "entity item second" }, expiration);
            ///    SessionApi.Set(sessionId, "item key 3", new EntitySample() { Id = 125, Name = "entity sample 3", Creation = DateTime.Now, Value = "entity item minute" }, expiration);
            ///}
            /// </code></example>
            public RemoteCacheState AddItemExisting(string sessionId, string key, object value, int expiration, bool validateExisting = false)
            {

                if (value == null)
                    return RemoteCacheState.ArgumentsError;// new MessageAck(MessageState.ArgumentsError, "AddItemExisting.value is null");// KnownCacheState.ArgumentsError;
                string cmd = (validateExisting) ? SessionCmd.AddItemExisting : SessionCmd.AddSessionItem;

                IMessageStream message = MessageStream.Create(protocol, cmd, key, sessionId, value, null, expiration);// CacheApi.SessionTimeout);
                return SendDuplexState(message);//, hostAddress, port, httpMethod, timeout, protocol);


                //return AddSessionItem(cmd, sessionId, key, value, expiration);
                //return o;// Types.ToInt(o);
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
                return SendDuplexState(message)== RemoteCacheState.Ok;//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<bool>(SessionCmd.Exists, sessionId, key);
            }

            /// <summary>
            /// Get all sessions keys in session cache.
            /// </summary>
            /// <returns></returns>
            public string[] GetAllSessionsKeys()
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetAllSessionsKeys, "*", "*", null, null, 0);
                return SendDuplexStream<string[]> (message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<string[]>(SessionCmd.GetAllSessionsKeys, "*", "*");
            }
            /// <summary>
            /// Get all items keys in specified session.
            /// </summary>
            /// <param name="sessionId"></param>
            /// <returns></returns>
            public string[] GetSessionsItemsKeys(string sessionId)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.GetSessionItemsKeys, "*", sessionId, null, null, 0);
                return SendDuplexStream<string[]>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return Get<string[]>(SessionCmd.GetSessionItemsKeys, "*", "*");
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, SessionCmd.Reply, text, text, null, null, 0);
                return SendDuplexStream<string>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);
                //return Get<string>(SessionCmd.Reply, text, text);
            }
            #endregion
        }
        #endregion

        #region SyncApi
        public class SyncApi:RemoteCacheApi
        {

            #region json query

            //public string SendJsonDuplex(string command, string key, object value, int expiration, string hostName, bool pretty = false)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, value, expiration);
            //    //message.SetBody(value);
            //    //message.Expiration = expiration;
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, hostName, pretty);
            //    return response;
            //}
            //public string SendJsonDuplex(string command, string key, object value, string hostName, bool pretty = false)
            //{
            //    IMessageStream message = CacheApi.CreateMessage(command, key, value, protocol);
            //    message.SetBody(value);
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, hostName, pretty);
            //    return response;
            //}

            //public string SendJsonDuplex(string command, string key, string hostName, bool pretty = false)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null,DefaultExpiration);
            //    string json = JsonSerializer.Serialize(message);
            //    string response = SendJsonDuplex(json, hostName, pretty);
            //    return response;
            //}
            //public string SendJsonDuplex(string json, string hostName, bool pretty = false)
            //{
            //    string response = null;

            //    switch (protocol)
            //    {
            //        case NetProtocol.Pipe:
            //            response = PipeJsonClient.SendDuplex(json, hostName, false);
            //            break;
            //        case NetProtocol.Tcp:
            //            response = TcpJsonClient.SendDuplex(json, hostName, false);
            //            break;
            //        case NetProtocol.Http:
            //            response = HttpJsonClient.SendDuplex(json, hostName, false);
            //            break;
            //    }

            //    if (pretty)
            //    {
            //        if (response != null)
            //            response = JsonSerializer.Print(response);
            //    }
            //    return response;

            //}


            #endregion

            #region internal sync command

            internal string GetKey(string itemName, string[] keys)
            {
                string key = keys == null ? "*" : ComplexKey.GetKey(itemName, keys);
                return key;
            }

            //internal object GetAsync(string command, string itemName, string[] keys, Type type)
            //{
            //    var result = Task.Factory.StartNew<object>(() => Get(command, itemName, keys, type));
            //    return result == null ? null : result.Result;
            //}

            //internal object Get(string command, string itemName, string[] keys, Type returnType)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, GetKey(itemName, keys), null,DefaultExpiration);
            //    message.TransformType = MessageStream.GetTransformType(returnType);
            //    return CacheApi.SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //internal T GetAsync<T>(string command, string itemName, string[] keys)
            //{
            //    var result = Task.Factory.StartNew<T>(() => Get<T>(command, itemName, keys));
            //    return result == null ? default(T) : result.Result;
            //}

            //internal T Get<T>(string command, string itemName, string[] keys)
            //{
            //    switch (command)
            //    {
            //        case SyncCacheCmd.GetEntity:
            //            var stream = (NetStream)Get(command, itemName, keys, typeof(NetStream));
            //            if (stream == null)
            //                return default(T);
            //            stream.Position = 0;
            //            return BinarySerializer.DeserializeFromStream<T>((NetStream)stream, SerialContextType.GenericEntityAsIEntityType);
            //        case SyncCacheCmd.GetEntityKeys:
            //            {
            //                IMessageStream msg = MessageStream.Create(protocol, command, itemName, null, DefaultExpiration);
            //                msg.TransformType = MessageStream.GetTransformType(typeof(T));
            //                return CacheApi.SendDuplex<T>(msg, hostAddress, port, httpMethod, timeout, protocol);
            //            }
            //        default:
            //            {
            //                IMessageStream message = MessageStream.Create(protocol, command, GetKey(itemName, keys), null,DefaultExpiration);
            //                message.TransformType = MessageStream.GetTransformType(typeof(T));
            //                return CacheApi.SendDuplex<T>(message, hostAddress, port, httpMethod, timeout, protocol);
            //            }
            //    }
            //}

            //internal void DoAsync(string command, string key, string[] args)
            //{
            //    Task.Factory.StartNew(() => Do(command, key, args));
            //}

            //internal void Do(string command, string key, string[] args)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, key, null, null, args,DefaultExpiration);
            //    CacheApi.SendDuplex(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            #endregion

            protected void OnFault(string message)
            {
                Console.WriteLine("SyncApi Fault: " + message);
            }

            //public T GetAsync<T>(string command, string itemName, string[] keys)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, GetKey(itemName, keys), null, DefaultExpiration);
            //    message.TransformType = MessageStream.GetTransformType(typeof(T));
            //    return CacheApi.SendDuplexAsync<T>(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //public T Get<T>(string command, string itemName, string[] keys)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, command, GetKey(itemName, keys), null, 0);
            //    message.TransformType = MessageStream.GetTransformType(typeof(T));
            //    return CacheApi.SendDuplex<T>(message, hostAddress, port, httpMethod, timeout, protocol);
            //}

            //public T GetEntity<T>(string itemName, string[] keys)
            //{
            //    IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, GetKey(itemName, keys), null, 0);
            //    //message.TransformType = MessageStream.GetTransformType(returnType);
            //    return CacheApi.SendDuplex<T>(message, hostAddress, port, httpMethod, timeout, protocol);
            //}
            //public T GetEntityKeys<T>(string itemName, string[] keys)
            //{
            //    IMessageStream msg = MessageStream.Create(protocol, SyncCacheCmd.GetEntityKeys, itemName, null, 0);
            //    msg.TransformType = MessageStream.GetTransformType(typeof(T));
            //    return CacheApi.SendDuplex<T>(msg, hostAddress, port, httpMethod, timeout, protocol);
            //}

            #region  public SyncCacheApi commands
            /*
            /// <summary>
            /// Get item value from cache.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key"></param>
            /// <returns></returns>
            public T Get<T>(string itemName, string[] keys)
            {
                var val = Get(itemName, keys);
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
            public T Get<T>(string itemName, string[] keys, T defaultValue)
            {
                var val = Get(itemName, keys);
                if (val == null)
                    return defaultValue;
                return GenericTypes.Convert<T>(val, defaultValue);
            }

            /// <summary>
            /// Get item value from cache.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public object Get(string itemName, string[] keys)
            {
                compl
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, key, null, null, null, 0);
                message.TransformType = TransformType.Stream;
                var stream = (NetStream)SendDuplex(message);
                if (stream == null)
                    return null;
                return BinarySerializer.ConvertFromStream(stream);
            }

            /// <summary>
            /// Get item value from cache as json.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="format"></param>
            /// <returns></returns>
            public string GetJson(string itemName, string[] keys, JsonFormat format)
            {
                var obj = Get(itemName, keys);
                if (obj == null)
                    return null;
                return JsonSerializer.Serialize(obj, null, format);
            }
            */
            /// <summary>
            /// Get item from sync cache using <see cref="ComplexKey"/> an <see cref="Type"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            public object GetItem(ComplexKey info)//, Type type)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetSyncItem, info.ToString(), null, 0);
                return SendDuplexStreamValue(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync(SyncCacheCmd.GetSyncItem, info.ItemName, info.ItemKeys, type);
            }

            /// <summary>
            /// Get item from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <param name="type"></param>
            /// <returns></returns>
            public object GetItem(string entityName, string[] keys)//, Type type)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetSyncItem, GetKey(entityName, keys), null, 0);
                return SendDuplexStreamValue(message,OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync(SyncCacheCmd.GetSyncItem, entityName, keys, type);
            }

            /// <summary>
            /// Get item from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="info"></param>
            /// <returns></returns>
            public T GetItem<T>(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetSyncItem, info.ToString(), null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);


                //return GetAsync<T>(SyncCacheCmd.GetSyncItem, info.ItemName, info.ItemKeys);
            }

            /// <summary>
            /// Get item from sync cache using arguments.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public T GetItem<T>(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetSyncItem, ComplexKey.GetKey(entityName,keys), null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<T>(SyncCacheCmd.GetSyncItem, entityName, keys);
            }

            /// <summary>
            ///  Get item as <see cref="IDictionary"/> from sync cache using pipe name and item key.
            /// </summary>
            /// <param name="itemName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public IDictionary GetRecord(string itemName, string[] keys)//, bool isAsync = false)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, ComplexKey.GetKey(itemName, keys), null, 0);
                return SendDuplexStream<IDictionary>(message, OnFault); //, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<IDictionary>(SyncCacheCmd.GetRecord, itemName, keys);
            }


            /// <summary>
            /// Get item as <see cref="IDictionary"/> from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <returns></returns>
            /// <example><code>
            /// //Get item value from sync cache as Dictionary.
            ///public void GetRecord()
            ///{
            ///    string key = "1";
            ///    var item = SyncCacheApi.GetRecord(ComplexKey.Get("contactEntity", new string[] { "1" }));
            ///    if (item == null)
            ///        Console.WriteLine("item not found " + key);
            ///    else
            ///        Console.WriteLine(item["FirstName"]);
            ///}
            /// </code></example>
            public IDictionary GetRecord(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetRecord, info.ToString(), null, 0);
                return SendDuplexStream<IDictionary>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                // return GetAsync<IDictionary>(SyncCacheCmd.GetRecord, info.ItemName, info.ItemKeys);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAs, info.ToString(), null, 0);
                message.TransformType = TransformType.Stream;
                return SendDuplexStream<NetStream>(message, OnFault); //, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<NetStream>(SyncCacheCmd.GetAs, info.ItemName, info.ItemKeys);
            }

            /// <summary>
            /// Get item as stream from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public NetStream GetAs(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAs, ComplexKey.GetKey(entityName, keys), null, 0);
                message.TransformType = TransformType.Stream;
                return SendDuplexStream<NetStream>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<NetStream>(SyncCacheCmd.GetAs, entityName, keys);
            }

            /// <summary>
            /// Get item as entity from sync cache using <see cref="ComplexKey"/>.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="info"></param>
            /// <returns></returns>
            /// <example><code>
            /// //Get item value from sync cache as Entity.
            ///public void GetEntity()
            ///{
            ///    string key = "1";
            ///    var item = <![CDATA[SyncCacheApi.GetEntity<ContactEntity>(ComplexKey.Get("contactEntity", new string[] { "1" }));]]>
            ///    if (item == null)
            ///        Console.WriteLine("item not found " + key);
            ///    else
            ///        Console.WriteLine(item.FirstName);
            ///}
            /// </code></example>
            public T GetEntity<T>(ComplexKey info)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, info.ToString(), null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<T>(SyncCacheCmd.GetEntity, info.ItemName, info.ItemKeys);
            }

            /// <summary>
            /// Get item as entity from sync cache using arguments.
            /// </summary>
            /// <param name="entityName"></param>
            /// <param name="keys"></param>
            /// <returns></returns>
            public T GetEntity<T>(string entityName, string[] keys)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntity, ComplexKey.GetKey(entityName, keys), null, 0);
                message.TransformType = MessageStream.GetTransformType(typeof(T));
                return SendDuplexStream<T>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<T>(SyncCacheCmd.GetEntity, entityName, keys);
            }
            /// <summary>
            /// Reset all items in sync cache
            /// </summary>
            public void Reset()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Reset, "*", null, null, null, 0);
                SendOutAsync(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //DoAsync(SyncCacheCmd.Reset, "*", null);
            }
            /// <summary>
            /// Refresh all items in sync cache
            /// </summary>
            public void Refresh()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Refresh, "*", null, null, null, 0);
                SendOutAsync(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //DoAsync(SyncCacheCmd.Refresh, "*", null);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.RefreshItem, syncName, null, null, null, 0);
                SendOutAsync(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //DoAsync(SyncCacheCmd.RefreshItem, syncName, null);
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
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.RemoveSyncItem, syncName, null, null, null, 0);
                SendOutAsync(message);//, hostAddress, port, httpMethod, timeout, protocol);

                //DoAsync(SyncCacheCmd.RemoveSyncItem, syncName, null);
            }
            /// <summary>
            /// Get entity count from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public int GetEntityItemsCount(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityItemsCount, entityName, null, 0);
                return SendDuplexStream<int>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<int>(SyncCacheCmd.GetEntityItemsCount, entityName, null);
            }
            /// <summary>
            /// Get entity values as <see cref="GenericKeyValue"/> array from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public GenericKeyValue GetEntityItems(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityItems, entityName, null, 0);
                return SendDuplexStream<GenericKeyValue>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<GenericKeyValue>(SyncCacheCmd.GetEntityItems, entityName, null);
            }

            /// <summary>
            /// Get entity keys from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public string[] GetEntityKeys(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetEntityKeys, entityName, null, 0);
                return SendDuplexStream<string[]>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<string[]>(SyncCacheCmd.GetEntityKeys, entityName, null);
            }

            /// <summary>
            /// Get all entity names from sync cache.
            /// </summary>
            /// <returns></returns>
            public string[] GetAllEntityNames()
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAllEntityNames, "*", null, 0);
                return SendDuplexStream<string[]>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<string[]>(SyncCacheCmd.GetAllEntityNames, "*", null);
            }

            /// <summary>
            /// Get entity items report from sync cache using entityName.
            /// </summary>
            /// <param name="entityName"></param>
            /// <returns></returns>
            public DataTable GetItemsReport(string entityName)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.GetAllEntityNames, entityName, null, 0);
                return SendDuplexStream<DataTable>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<DataTable>(SyncCacheCmd.GetItemsReport, entityName, null);
            }

            /// <summary>
            /// Reply for test.
            /// </summary>
            /// <returns></returns>
            public string Reply(string text)
            {
                IMessageStream message = MessageStream.Create(protocol, SyncCacheCmd.Reply, text, text, 0);
                return SendDuplexStream<string>(message, OnFault);//, hostAddress, port, httpMethod, timeout, protocol);

                //return GetAsync<string>(SyncCacheCmd.Reply, text, new string[] { text });
            }
            #endregion
        }
        #endregion
    }
}