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
using System.Collections;
using Nistec.Runtime;
using Nistec.Channels;
using Nistec.Generic;
using System.Data;
using Nistec.IO;
using Nistec.Channels.Tcp;

namespace Nistec.Channels.RemoteTrace
{
    /// <summary>
    /// Represent sync trace api for client.
    /// </summary>
    public static class TraceApi
    {
        enum EnumEmpty { NA };
        /// <summary>
        /// Default Protocol
        /// </summary>
        public const NetProtocol DefaultProtocol = NetProtocol.Pipe;
        internal static Type TypeEmpty = typeof(EnumEmpty);
        internal enum HostType { Trace };
        
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

        #region Send methods internal

        internal static string GetHost(HostType hostType)
        {
            string hostName = TraceSettings.RemoteTraceHostName;
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
                            TraceSettings.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex(message as PipeMessage,
                            GetHost(hostType),
                            TraceSettings.IsRemoteAsync,
                            TraceSettings.EnableRemoteException);
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
                            TraceSettings.EnableRemoteException);
                    }
                case NetProtocol.Pipe:
                    {
                        return PipeClient.SendDuplex<T>(message as PipeMessage,
                            GetHost(hostType),
                            TraceSettings.IsRemoteAsync,
                            TraceSettings.EnableRemoteException);
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
                            TraceSettings.EnableRemoteException);
                        break;
                    }
                case NetProtocol.Pipe:
                    {
                        PipeClient.SendOut(message as PipeMessage,
                            GetHost(hostType),
                            TraceSettings.IsRemoteAsync,
                            TraceSettings.EnableRemoteException);
                        break;
                    }
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }

        }
        #endregion

        #region trace cmd

        /// <summary>
        /// Get item from sync trace using <see cref="MessageKey"/> an <see cref="Type"/>.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        internal static object Get(string command, string key, Type type, NetProtocol protocol)
        {
            IMessage message = CreateMessage(command, key, null, type, null, protocol);
            return SendDuplex(message, HostType.Trace, protocol);
        }

        /// <summary>
        /// Get item from sync trace using <see cref="MessageKey"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        internal static T Get<T>(string command, string key, NetProtocol protocol)
        {
            IMessage message = CreateMessage(command, key, null, typeof(T), null, protocol);
            return SendDuplex<T>(message, HostType.Trace, protocol);
        }



        /// <summary>
        /// Do trace command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="keyValue"></param>
        /// <param name="protocol"></param>
        internal static void Do(string command, string key, string[] keyValue, NetProtocol protocol)
        {
            IMessage message = CreateMessage(command, key, null, TypeEmpty, keyValue, protocol);
            SendOut(message,HostType.Trace,  protocol);
        }

        /// <summary>
        /// Set command to trace.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        internal static object Set(string command, string key, string id, object value, NetProtocol protocol)
        {
            if (value == null)
                return KnownTraceState.ArgumentsError;
            IMessage message = CreateMessage(command, key, id, value.GetType(), null, protocol);
            message.SetBody(value);
            return SendDuplex(message, HostType.Trace, protocol);
        }

        /// <summary>
        /// SiteCounter
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="state"></param>
        /// <param name="device"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static void DoSiteCounter(int siteId, int state, int device, NetProtocol protocol = DefaultProtocol)
        {

            string[] keyValues = new string[] { "SiteId", siteId.ToString(), "State", state.ToString(), "Device", device.ToString() };
            Do(TraceCmd.SiteCounter, siteId.ToString(), keyValues, protocol);

        }
        /// <summary>
        /// ServiceCounter
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="state"></param>
        /// <param name="device"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static void DoServiceCounter(int serviceId, int state, int device, NetProtocol protocol = DefaultProtocol)
        {
            string[] keyValues = new string[] { "ServiceId", serviceId.ToString(), "State", state.ToString(), "Device", device.ToString() };
            Do(TraceCmd.ServiceCounter, serviceId.ToString(), keyValues, protocol);
        }


        #endregion

 
    }
}
