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
using Nistec.Runtime;
using Nistec.Serialization;
using System.Collections;
using Nistec.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Nistec.Channels
{
    /// <summary>
    /// Represent a message for named pipe communication.
    /// </summary>
    [Serializable]
    public class PipeMessage : MessageStream,IMessage, IDisposable
    {
 
        #region ctor

        /// <summary>
        /// Initialize a new instance of pipe message.
        /// </summary>
        public PipeMessage() : base() 
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
        public PipeMessage(string command, string key, object value, int expiration)
            : this()
        {
            Command = command;
            Key = key;
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
        public PipeMessage(string command, string key, object value, int expiration, string sessionId)
            : this()
        {
            Command = command;
            Key = key;
            Expiration = expiration;
            Id = sessionId;
            SetBody(value);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Destructor.
        /// </summary>
        ~PipeMessage()
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
            PipeMessage message = new PipeMessage();
            message.EntityRead(stream, streamer);
            return message;
        }

        #endregion

        #region send methods
        /// <summary>
        /// Send duplex message to named pipe server using the pipe name argument.
        /// </summary>
        /// <param name="PipeName"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public object SendDuplex(string PipeName, bool IsAsync)
        {
            return PipeClient.SendDuplex(this, PipeName, IsAsync);
        }
        /// <summary>
        /// Send duplex message to named pipe server using the pipe name argument.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PipeName"></param>
        /// <param name="IsAsync"></param>
        /// <returns></returns>
        public T SendDuplex<T>(string PipeName, bool IsAsync)
        {
            return PipeClient.SendDuplex<T>(this, PipeName, IsAsync);
        }
        /// <summary>
        /// Send one way message to named pipe server using the pipe name argument.
        /// </summary>
        /// <param name="PipeName"></param>
        /// <param name="IsAsync"></param>
        public void SendOut(string PipeName, bool IsAsync)
        {
            PipeClient.SendOut(this, PipeName, IsAsync);
        }

        #endregion

        #region Read/Write

        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }

        public static PipeMessage ParseStream(Stream stream)
        {
            var message = new PipeMessage();
            message.EntityRead(stream, null);
            return message;
        }

        internal static PipeMessage ReadRequest(NamedPipeServerStream pipeServer, int InBufferSize = 8192)
        {
            var message = new PipeMessage();
            message.EntityRead(pipeServer, null);

            return message;
        }

        internal static void WriteResponse(NamedPipeServerStream pipeServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            int cbResponse = bResponse.iLength;

            pipeServer.Write(bResponse.ToArray(), 0, cbResponse);

            pipeServer.Flush();

        }

        #endregion

        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary dict)
        {
            PipeMessage message = new PipeMessage()
            {
                Command = dict.Get<string>("Command"),
                Key = dict.Get<string>("Key"),
                Args = dict.Get<GenericNameValue>("Args"),
                BodyStream = dict.Get<NetStream>("Body", null),
                Expiration = dict.Get<int>("Expiration", 0),
                IsDuplex = dict.Get<bool>("IsDuplex", true),
                Modified = dict.Get<DateTime>("Modified", DateTime.Now),
                TypeName = dict.Get<string>("TypeName"),
                Id = dict.Get<string>("Id")
            };

            return message;
        }

        #region ReadAck pipe

        public object ReadAck(NamedPipeClientStream stream, Type type, int InBufferSize = 8192)
        {

            using (AckStream ack = AckStream.Read(stream, type, InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new Exception(ack.Message);
                }
                return ack.Value;
            }
        }

        public TResponse ReadAck<TResponse>(NamedPipeClientStream stream, int InBufferSize = 8192)
        {

            using (AckStream ack = AckStream.Read(stream, typeof(TResponse), InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new Exception(ack.Message);
                }
                return ack.GetValue<TResponse>();
            }
        }


        #endregion

    }
}
