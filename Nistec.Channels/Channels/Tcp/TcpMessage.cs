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
using System.IO;
using Nistec.Generic;
using System.Collections;
using Nistec.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Net.Sockets;
using Nistec.Serialization;
using Nistec.Runtime;

namespace Nistec.Channels.Tcp
{
    /// <summary>
    /// Represent a message for named tcp communication.
    /// </summary>
    [Serializable]
    public class TcpMessage : MessageStream//, ITransformMessage, IDisposable
    {
        #region ctor

        /// <summary>
        /// Initialize a new instance of tcp message.
        /// </summary>
        public TcpMessage() : base() 
        { 
            Formatter = MessageStream.DefaultFormatter;
            Creation = DateTime.Now;
        }
        /// <summary>
        /// Initialize a new instance of tcp message.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        public TcpMessage(string command, string key, object value, int expiration)
            : this()
        {
            Command = command;
            //Identifier = key;
            //mqh-CustomId = key;
            //mqh-Expiration = expiration;
            SetBody(value);
        }
        /// <summary>
        /// Initialize a new instance of tcp message.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        public TcpMessage(string command, string key, object value, int expiration, string sessionId)
            : this()
        {
            Command = command;
            //Identifier = key;
            //mqh-CustomId = key;
            //mqh-Expiration = expiration;
            //mqh-SessionId = sessionId;
            SetBody(value);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Destructor.
        /// </summary>
        ~TcpMessage()
        {
            Dispose(false);
        }
        #endregion

        #region send methods
        /// <summary>
        /// Send duplex message to tcp server using the host name and port arguments.
        /// </summary>
        /// <param name="HostAddress"></param>
        /// <param name="Port"></param>
        /// <param name="ConnectTimeOut"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public object SendDuplex(string HostAddress,int Port, int ConnectTimeOut, bool IsAsync)
        {
            return TcpStreamClient.SendDuplex(this, HostAddress, Port,ConnectTimeOut, IsAsync);
        }
        /// <summary>
        /// Send duplex message to tcp server using the host name and port arguments.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HostAddress"></param>
        /// <param name="Port"></param>
        /// <param name="ConnectTimeOut"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public T SendDuplex<T>(string HostAddress, int Port, int ConnectTimeOut, bool IsAsync)
        {
            return TcpStreamClient.SendDuplex<T>(this, HostAddress, Port, ConnectTimeOut, IsAsync);
        }
        /// <summary>
        /// Send one way message to tcp server using the host name and port arguments.
        /// </summary>
        /// <param name="HostAddress"></param>
        /// <param name="Port"></param>
        /// <param name="ConnectTimeOut"></param>
        /// <param name="IsAsync"></param>
        public void SendOut(string HostAddress, int Port, int ConnectTimeOut, bool IsAsync)
        {
            TcpStreamClient.SendOut(this, HostAddress, Port, ConnectTimeOut, IsAsync);
        }

        #endregion

        #region Read/Write

        internal static TcpMessage ServerReadRequest(NetworkStream streamServer)
        {

            var message = new TcpMessage();
            message.EntityRead(streamServer, null);
            return message;
        }

        /*
        /// <summary>
        /// Get <see cref="TcpMessage"/> as <see cref="NetStream"/> Stream.
        /// </summary>
        /// <returns></returns>
        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }
        /// <summary>
        /// Convert stream to <see cref="TcpMessage"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static TcpMessage ParseStream(Stream stream)
        {
            var message = new TcpMessage();
            message.EntityRead(stream, null);
            return message;
        }

        internal static void ServerWriteResponse(NetworkStream streamServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            int cbResponse = bResponse.iLength;

            streamServer.Write(bResponse.ToArray(), 0, cbResponse);

            streamServer.Flush();

        }

        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary dict)
        {
            TcpMessage message = new TcpMessage()
            {
                Command = dict.Get<string>("Command"),
                Key = dict.Get<string>("Key"),
                Args = dict.Get<GenericNameValue>("Args"),
                BodyStream = dict.Get<NetStream>("Body", null),//, ConvertDescriptor.Implicit),
                Expiration = dict.Get<int>("Expiration", 0),
                IsDuplex = dict.Get<bool>("IsDuplex", true),
                Modified = dict.Get<DateTime>("Modified", DateTime.Now),
                TypeName = dict.Get<string>("TypeName"),
                Id = dict.Get<string>("Id"),
                ReturnTypeName = dict.Get<string>("ReturnTypeName")
            };

            return message;
        }
        */
        #endregion

        #region ReadAck tcp
       /*
        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        public object ReadAck(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        {
            using (TransStream ack = new TransStream(stream, readTimeout, ReceiveBufferSize))
            {
                return ack.ReadValue();
            }
        }

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="type"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <returns></returns>
        public object ReadAck(NetworkStream stream, TransformType type, int readTimeout, int ReceiveBufferSize)
        {

            using (TransStream ack = new TransStream(stream, readTimeout, ReceiveBufferSize))
            {
                return ack.ReadValue();
            }
        }

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <returns></returns>
        public TResponse ReadAck<TResponse>(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        {

            using (TransStream ack = new TransStream(stream, readTimeout, ReceiveBufferSize))
            {
                return ack.ReadValue<TResponse>();
            }
        }
        */
        ///// <summary>
        ///// Read response from server.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="readTimeout"></param>
        ///// <param name="ReceiveBufferSize"></param>
        //public object ReadAck(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        //{
        //    using (AckStream ack = new AckStream(stream, TransformType.Message, readTimeout, ReceiveBufferSize))
        //    {
        //        if (ack.State > MessageState.Ok)
        //        {
        //            throw new MessageException(ack);
        //        }
        //        return ack.Value;
        //    }
        //}

        ///// <summary>
        ///// Read response from server.
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="type"></param>
        ///// <param name="readTimeout"></param>
        ///// <param name="ReceiveBufferSize"></param>
        ///// <returns></returns>
        //public object ReadAck(NetworkStream stream, TransformType type, int readTimeout, int ReceiveBufferSize)
        //{

        //    using (AckStream ack = new AckStream(stream, type, readTimeout, ReceiveBufferSize))
        //    {
        //        if (ack.State > MessageState.Ok)
        //        {
        //            throw new MessageException(ack);
        //        }
        //        return ack.Value;
        //    }
        //}

        ///// <summary>
        ///// Read response from server.
        ///// </summary>
        ///// <typeparam name="TResponse"></typeparam>
        ///// <param name="stream"></param>
        ///// <param name="readTimeout"></param>
        ///// <param name="ReceiveBufferSize"></param>
        ///// <returns></returns>
        //public TResponse ReadAck<TResponse>(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        //{

        //    using (AckStream ack = new AckStream(stream, MessageStream.GetTransformType(typeof(TResponse)), readTimeout, ReceiveBufferSize))
        //    {
        //        if (ack.State > MessageState.Ok)
        //        {
        //            throw new MessageException(ack);
        //        }
        //        return ack.GetValue<TResponse>();
        //    }
        //}

        #endregion

        #region static
        /*
        /// <summary>
        /// Create a new message stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public static MessageStream Create(Stream stream, IBinaryStreamer streamer)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(stream, streamer);
            return message;
        }
        */
        #endregion      
    }
}
