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
using System.Threading.Tasks;

namespace Nistec.Channels
{
    /// <summary>
    /// Represent a message for named pipe communication.
    /// </summary>
    [Serializable]
    public class GenericMessage : MessageStream, ITransformMessage, IDisposable
    {
        #region ctor

        /// <summary>
        /// Initialize a new instance of message stream.
        /// </summary>
        public GenericMessage() : base() 
        { 
            Formatter = MessageStream.DefaultFormatter;
            //mqh-Modified = DateTime.Now;
        }
        /// <summary>
        /// Initialize a new instance of message stream.
        /// </summary>
        /// <param name="body"></param>
        public GenericMessage(object body)
            : this()
        {
            SetBody(body);
        }

        /// <summary>
        /// Initialize a new instance of message stream.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        public GenericMessage(string command, string key, object value, int expiration)
            : this()
        {
            Command = command;
            //Identifier = id;
            CustomId = key;
            Expiration = expiration;
            SetBody(value);
        }

        /// <summary>
        /// Initialize a new instance of message stream.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="label"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        public GenericMessage(string command, string key, string label, object value, int expiration, string sessionId)
            : this()
        {
            Command = command;
            CustomId = key;
            SessionId = sessionId;
            Expiration = expiration;
            Label = label;
            SetBody(value);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Destructor.
        /// </summary>
        ~GenericMessage()
        {
            Dispose(false);
        }
        #endregion

        #region Convert
        /*
        public StreamBinary ConvertToStreamBinary()
        {
            return new StreamBinary(this);
        }
        public static GenericMessage ConvertFrom(StreamBinary sb)
        {
            NetStream ns = new NetStream(sb.BodyStream);
            var ser = new BinarySerializer();
            return ser.Deserialize<GenericMessage>(ns);
        }
        */
        #endregion

        public byte[] Body { get => base._Body; set => base._Body = value; }

    }
}
