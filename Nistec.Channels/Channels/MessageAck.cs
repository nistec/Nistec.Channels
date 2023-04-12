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
using Nistec.Serialization;
using Nistec.IO;
using System.IO;
using Nistec.Generic;

namespace Nistec.Channels
{

    /// <summary>
    /// Represent a response message for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class MessageAck : ISerialEntity, ITransformResponse , IAck//IDisposable
    {
        #region properties
        public ChannelState State { get; set; }
        //public Formatters Formatter { get { return Formatters.BinarySerializer; } }
        public string Message { get; set; }
        //public DateTime Modified { get; protected set; }
        public object Response { get; set; }

        #endregion

        #region ITransformResponse

        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(Message);
        }

        public void SetState(int state, string message)
        {
            State =(ChannelState)state;
            Message = message;
        }

        #endregion

        #region ctor

        public MessageAck()
        { 
            //Modified = DateTime.Now;
        }

        public MessageAck(ChannelState state, string message)
            : this()
        {
            State = state;
            Message = message;
        }
        public MessageAck(ChannelState state, string message, object response)
            : this()
        {
            State = state;
            Message = message;
            Response = response;
        }
        public MessageAck(NetStream stream)
            : this()
        {
            EntityRead(stream, null);
        }

        protected MessageAck(Stream stream, IBinaryStreamer streamer)
            : this()
        {
            EntityRead(stream, streamer);
        }
        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        bool disposed = false;
        protected bool IsDisposed
        {
            get { return disposed; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Message = null;
            }
            disposed = true;
        }
        #endregion

        #region  IEntityFormatter

        public void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteValue((int)State);
            streamer.WriteString(Message);
            //streamer.WriteValue(Modified);
            streamer.WriteValue(Response);
            streamer.Flush();
        }

        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            State = (ChannelState)streamer.ReadValue();
            Message = streamer.ReadString();
            //Modified = streamer.ReadValue<DateTime>();
            Response = streamer.ReadValue();
        }

        #endregion

        #region static
        public static MessageAck DoOk()
        {
            return new MessageAck(ChannelState.Ok, "ok");
        }
        public static MessageAck DoAck(ChannelState state, string message)
        {
            return new MessageAck(state, message);
        }

        //public static NetStream DoAck<T>(T value)
        //{
        //    NetStream ns = new NetStream();
        //    BinaryStreamer streamer = new BinaryStreamer(ns);
        //    streamer.WritePrimitive<T>(value);
        //    ns.Position = 0;
        //    return ns;
        //}
        public static MessageAck DoResponse(ChannelState state, string message, object response = null)
        {
            return new MessageAck(state, message, response);
        }
        public static NetStream DoStream(ChannelState state, string message, object response=null)
        {
            MessageAck pm = new MessageAck(state, message, response);
            return pm.Serialize();
        }
        //public static TransStream ToTransStream(ChannelState state, string message, object response = null)
        //{
        //    MessageAck pm = new MessageAck(state, message, response);
        //    return pm.ToTransStream();
        //}
        #endregion

        public TransStream ToTransStream()
        {
            return new TransStream(this.Serialize(), TransType.Ack);
        }

        public override string ToString()
        {
            return string.Format("State: {0}, Message: {1}", State.ToString(),Message);
        }
        public string Display()
        {
            return Strings.ReflatJson(GenericKeyValue.Create("Message", Message, "Response", Response, "State", State.ToString()).ToJson());
        }
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
