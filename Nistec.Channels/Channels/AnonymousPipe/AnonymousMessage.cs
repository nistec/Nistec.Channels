﻿//licHeader
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
using Nistec.Runtime;
using Nistec.Serialization;
using System.Collections;
using Nistec.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Nistec.Channels
{
    /// <summary>
    /// Represent a message for anonymous pipe communication.
    /// </summary>
    [Serializable]
    public class AnonymousMessage : MessageStream, ITransformMessage// IDisposable
    {
 
        #region ctor

        /// <summary>
        /// Initialize a new instance of pipe message.
        /// </summary>
        public AnonymousMessage() : base() 
        { 
            Formatter = MessageStream.DefaultFormatter;
            Modified = DateTime.Now;
        }
        /// <summary>
        /// Initialize a new instance of pipe message.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        public AnonymousMessage(string command, string key, object value, int expiration)
            : this()
        {
            Command = command;
            CustomId = key;
            Expiration = expiration;
            SetBody(value);
        }
        /// <summary>
        /// Initialize a new instance of pipe message.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        public AnonymousMessage(string command, string key, object value, int expiration, string sessionId)
            : this()
        {
            Command = command;
            CustomId = key;
            Expiration = expiration;
            SessionId = sessionId;
            SetBody(value);
        }
        public AnonymousMessage(IDictionary<string, object> dict):base(dict)
        {

        }
        #endregion

        #region Dispose
        /// <summary>
        /// Destructor.
        /// </summary>
        ~AnonymousMessage()
        {
            Dispose(false);
        }
        #endregion

        #region static
        /// <summary>
        /// Create a new message stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public static MessageStream Create(Stream stream, IBinaryStreamer streamer)
        {
            AnonymousMessage message = new AnonymousMessage();
            message.EntityRead(stream, streamer);
            return message;
        }

        #endregion

        #region send methods
        /// <summary>
        /// Send duplex message to named pipe server using the pipe name argument.
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public object SendDuplex(string FileName, bool IsAsync)
        {
            return AnonymousPipeServer.SendDuplex(this, FileName, IsAsync);
        }
        /// <summary>
        /// Send duplex message to named pipe server using the pipe name argument.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="FileName"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public T SendDuplex<T>(string FileName, bool IsAsync)
        {
            return AnonymousPipeServer.SendDuplex<T>(this, FileName, IsAsync);
        }
        /// <summary>
        /// Send one way message to named pipe server using the pipe name argument.
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="IsAsync"></param>
        public void SendOut(string FileName, bool IsAsync)
        {
            AnonymousPipeServer.SendOut(this, FileName, IsAsync);
        }

        #endregion

        #region Read/Write

        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            stream.Position = 0;
            return stream;
        }

        public static AnonymousMessage ParseStream(Stream stream)
        {
            var message = new AnonymousMessage();
            message.EntityRead(stream, null);
            return message;
        }

        internal static AnonymousMessage ReadRequest(AnonymousPipeClientStream pipeClient, int ReceiveBufferSize = 8192)
        {
            var message = new AnonymousMessage();
            var stream= AnonymousMessage.CopyStream(pipeClient);
            message.EntityRead(stream, null);
            return message;
        }

        internal static void WriteResponse(AnonymousPipeClientStream pipeClient, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            int cbResponse = bResponse.iLength;

            pipeClient.Write(bResponse.ToArray(), 0, cbResponse);

            pipeClient.Flush();

        }

        public static NetStream CopyStream(PipeStream stream, int ReceiveBufferSize = 8192)
        {
            if (ReceiveBufferSize <= 0)
                ReceiveBufferSize = 8192;
            int cbRead = 0;
            NetStream ms = new NetStream();
            do
            {
                byte[] bytes = new byte[ReceiveBufferSize];
                int bytesLength = bytes.Length;
                cbRead = stream.Read(bytes, 0, bytesLength);
                if (cbRead > 0)
                    ms.Write(bytes, 0, cbRead);
            }
            while (cbRead > 0);

            if (ms.Length > 0)
            {
                ms.Position = 0;
            }
            return ms;
        }

        #endregion

        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary<string ,object> dict)
        {
            return new AnonymousMessage(dict);

            //AnonymousMessage message = new AnonymousMessage(dict)
            //{
            //    Command = dict.Get<string>("Command"),
            //    Identifier = dict.Get<string>("Identifier"),
            //    Args = dict.Get<NameValueArgs>("Args"),
            //    BodyStream = dict.Get<NetStream>("Body", null),
            //    Expiration = dict.Get<int>("Expiration", 0),
            //    //IsDuplex = dict.Get<bool>("IsDuplex", true),
            //    DuplexType = (DuplexTypes)dict.Get<int>("DuplexType", 0),
            //    Modified = dict.Get<DateTime>("Modified", DateTime.Now),
            //    TypeName = dict.Get<string>("TypeName"),
            //    Label = dict.Get<string>("Label"),
            //    TransformType=(TransformType) dict.Get<byte>("TransformType")
            //};

            //return message;
        }

        internal static NetStream CreateAnonymousAck(ChannelState state, string message)
        {
            return new AnonymousMessage("ack", state.ToString(), message, 0).ToStream();
        }

        #region ReadAck pipe

        public object ReadAck(AnonymousPipeClientStream stream, TransformType type, int ReceiveBufferSize = 8192)
        {
            //return TransReader.ReadValue(CopyStream(stream));
            return TransStream.ReadValue(CopyStream(stream));
            //var ns = CopyStream(stream);
            //using (TransStream ack = TransStream.Read(ns, type, ReceiveBufferSize))
            //{
            //    return ack.GetValue();
            //}
        }

        public TResponse ReadAck<TResponse>(AnonymousPipeClientStream stream, int ReceiveBufferSize = 8192)
        {
            //return TransReader.ReadValue<TResponse>(CopyStream(stream));
            return TransStream.ReadValue<TResponse>(CopyStream(stream));

            //var ns = CopyStream(stream);
            //using (TransStream ack = TransStream.Read(ns, MessageStream.GetTransformType(typeof(TResponse)), ReceiveBufferSize))
            //{
            //    return ack.GetValue<TResponse>();
            //}

        }
        //public object ReadAck(AnonymousPipeClientStream stream, TransformType type, int ReceiveBufferSize = 8192)
        //{
        //    var ns = CopyStream(stream);
        //    using (AckStream ack = AckStream.Read(ns, type, ReceiveBufferSize))
        //    {
        //        if (ack.State > MessageState.Ok)
        //        {
        //            throw new Exception(ack.Message);
        //        }
        //        return ack.Value;
        //    }
        //}

        //public TResponse ReadAck<TResponse>(AnonymousPipeClientStream stream, int ReceiveBufferSize = 8192)
        //{
        //    var ns= CopyStream(stream);
        //    using (AckStream ack = AckStream.Read(ns, MessageStream.GetTransformType(typeof(TResponse)), ReceiveBufferSize))
        //    {
        //        if (ack.State > MessageState.Ok)
        //        {
        //            throw new Exception(ack.Message);
        //        }
        //        return ack.GetValue<TResponse>();
        //    }
        //}


        #endregion

    }
}
