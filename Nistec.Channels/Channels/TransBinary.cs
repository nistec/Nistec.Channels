//licHeader
//===============================================================================================================
// System  : Nistec.Lib - Nistec.Lib Class Library
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
//using Nistec.Channels;
using Nistec.Generic;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Nistec.Channels
{
 
    [Serializable]
    public class TransBinary : ISerialEntity,IDisposable
    {
        #region ctor
        public TransBinary()
        {

        }
         public TransBinary(object value, TransType type = TransType.Object)
        {
            TransType = type;
            State = 0;
            if (value != null)
            {
                TypeName = value.GetType().FullName;

                using (NetStream ns = new NetStream())
                {
                    var ser = new BinarySerializer();
                    ser.Serialize(ns, value);
                    ns.Position = 0;
                    BodyStream = ns.ToArray();
                }
            }
            else
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
            }
        }

        #endregion

        #region Stream / properties

        public string TypeName { get; set; }
        public TransType TransType { get; set; }
        public int State { get; set; }
        public byte[] BodyStream { get; set; }

        #endregion

        #region  Write/Read Trans

        
        public virtual void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            //stream.Clear();
            streamer.WriteValue((byte)TransType);
            streamer.WriteValue((int)State);
            streamer.WriteString(TypeName);
            streamer.WriteValue(BodyStream);
            streamer.Flush();
        }

        public virtual void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            TransType = (TransType)streamer.ReadValue<byte>();
            State = streamer.ReadValue<int>();
            TypeName = streamer.ReadString();
            BodyStream = (byte[])streamer.ReadValue();

        }

        #endregion

        #region Encode/Decode Body

        public override string ToString()
        {
            return string.Format("TypeName: {0}, TransType: {1}, Size: {2}", TypeName, TransType.ToString(), BodyStream.Length);
        }


        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }

        public string ToJson(bool pretty = false)
        {
            return GenericKeyValue.Create("TransType", TransType, "State", State, "TypeName", TypeName,"Body", ReadBody()).ToJson(pretty);
        }

        public virtual object ReadBody()
        {
            if (BodyStream == null)
                return null;
            //BodyStream.Position = 0;
            var ser = new BinarySerializer();
            return ser.Deserialize(new NetStream(BodyStream));
        }
 
        public T ReadBody<T>()
        {
            return GenericTypes.Cast<T>(ReadBody(), true);
        }

        public string BodyToJson(bool pretty = false)
        {
            return JsonSerializer.Serialize(ReadBody(), pretty);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    BodyStream = null;
                    //if (BodyStream != null)
                    //{
                    //    BodyStream.Dispose();
                    //}
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion

    }

}
