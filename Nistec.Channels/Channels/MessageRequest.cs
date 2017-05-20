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
    /// Represent a request message for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class MessageRequest : ISerialEntity, IDisposable
    {
        #region properties

        public Formatters Formatter { get { return Formatters.BinarySerializer; } }
        public string Sender { get; set; }
        public string Destination { get; set; }
        public GenericNameValue Request { get; set; }
        public string Command { get; set; }
        public DateTime Modified { get; set; }
        #endregion

        #region ctor

        public MessageRequest() 
        { 
            Modified = DateTime.Now;
        }

        public MessageRequest(string sender, string dest,string command,string[] keyValueMessage)
            : this()
        {
            Sender = sender;
            Destination = dest;
            Command = command;
            Request = new GenericNameValue(keyValueMessage);
        }

        public MessageRequest(NetStream stream)
            : this()
        {
            EntityRead(stream, null);
        }

        public MessageRequest(string sender, string dest, string command, string queryString)
            : this()
        {

            Request = GenericNameValue.ParseQueryString(queryString);


        }
        protected MessageRequest(Stream stream, IBinaryStreamer streamer)
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
                Command=null;
                Destination=null;
                Sender = null;
                Request = null;
            }
            disposed = true;
        }
        #endregion

        #region  IEntityFormatter

        public void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteString(Command);
            streamer.WriteString(Sender);
            streamer.WriteString(Destination);
            streamer.WriteValue(Modified);
            streamer.WriteValue(Request);
            streamer.Flush();
        }

        

        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            Command = streamer.ReadString();
            Sender = streamer.ReadString();
            Destination = streamer.ReadString();
            Modified = streamer.ReadValue<DateTime>();
            Request = streamer.ReadValue<GenericNameValue>();
        }
 
        #endregion

    }
}
