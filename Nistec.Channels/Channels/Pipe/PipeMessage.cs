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
    public class PipeMessage : MessageStream, ITransformMessage//, IDisposable
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
            Id = key;
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
            Id = key;
            Expiration = expiration;
            Label = sessionId;
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

        public static PipeOptions GetPipeOptions(bool isAsync, PipeTransmissionMode transformMode= PipeTransmissionMode.Message)
        {
            if (isAsync)
                return transformMode== PipeTransmissionMode.Byte? PipeOptions.Asynchronous| PipeOptions.WriteThrough: PipeOptions.Asynchronous;
            return transformMode == PipeTransmissionMode.Byte ? PipeOptions.WriteThrough : PipeOptions.None;
        }

        #endregion

        #region send methods
        /// <summary>
        /// Send duplex message to named pipe server using the pipe name argument.
        /// </summary>
        /// <param name="PipeName"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public object SendDuplex(string PipeName, PipeOptions option= PipeOptions.None)
        {
            return PipeClient.SendDuplex(this, PipeName, false, option);
        }
        /// <summary>
        /// Send duplex message to named pipe server using the pipe name argument.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PipeName"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public T SendDuplex<T>(string PipeName, PipeOptions option = PipeOptions.None)
        {
            return PipeClient.SendDuplex<T>(this, PipeName, false, option);
        }
        /// <summary>
        /// Send one way message to named pipe server using the pipe name argument.
        /// </summary>
        /// <param name="PipeName"></param>
        /// <param name="option"></param>
        public void SendOut(string PipeName, PipeOptions option = PipeOptions.None)
        {
            PipeClient.SendOut(this, PipeName, false, option);
        }

        #endregion

        #region Read/Write

        internal static PipeMessage ReadRequest(NamedPipeServerStream pipeServer, int ReceiveBufferSize = 8192)
        {
            var message = new PipeMessage();
            message.EntityRead(pipeServer, null);

            return message;
        }

        #endregion

        #region ReadResponse pipe

        //public object ReadResponse(NamedPipeClientStream stream, int ReceiveBufferSize = 8192)
        //{
        //    return new TransStream(stream, ReceiveBufferSize);
        //}
        //public object ReadResponse(NamedPipeClientStream stream, TransformType transformType, int ReceiveBufferSize = 8192)
        //{
        //    if (transformType == TransformType.TransStream)
        //    {
        //        return new TransStream(stream, ReceiveBufferSize);
        //    }

        //    using (TransStream ack = new TransStream(stream, ReceiveBufferSize, TransformType.Message))
        //    {
        //        return ack.ReadValue();
        //    }
        //}
        //public TResponse ReadResponse<TResponse>(NamedPipeClientStream stream, int ReceiveBufferSize = 8192)
        //{
        //    if (TransReader.IsTransStream(typeof(TResponse)))
        //    {
        //        TransStream ts = new TransStream(stream, ReceiveBufferSize);
        //        return GenericTypes.Cast<TResponse>(ts, true);
        //    }
        //    using (TransStream ack = new TransStream(stream, ReceiveBufferSize, TransformType.Message))
        //    {
        //        return ack.ReadValue<TResponse>();
        //    }
        //}

        #endregion

    }
}
