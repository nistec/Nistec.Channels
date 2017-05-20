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


namespace Nistec.Channels
{

    /// <summary>
    /// Represent a response message for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class MessageAck : ISerialEntity, IDisposable
    {
        #region properties
        public MessageState State { get; set; }
        public Formatters Formatter { get { return Formatters.BinarySerializer; } }
        public string Message { get; set; }
        public DateTime Modified { get; protected set; }
        #endregion

        #region ctor

        public MessageAck()
        { 
            Modified = DateTime.Now;
        }

        public MessageAck(MessageState state, string message)
            : this()
        {
            State = state;
            Message = message;
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
            streamer.WriteValue(Modified);
            streamer.Flush();
        }

        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            State = (MessageState)streamer.ReadValue();
            Message = streamer.ReadString();
            Modified = streamer.ReadValue<DateTime>();
        }

        #endregion

        #region static

        public static NetStream DoAck<T>(T value)
        {
            NetStream ns = new NetStream();
            BinaryStreamer streamer = new BinaryStreamer(ns);
            streamer.WritePrimitive<T>(value);
            ns.Position = 0;
            return ns;
        }

        public static NetStream DoResponse(MessageState state, string message)
        {
            MessageAck pm = new MessageAck(state,message);
            return pm.Serialize();
        }
        #endregion

    }
}
