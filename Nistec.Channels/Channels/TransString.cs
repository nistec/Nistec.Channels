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
using Nistec.Channels.Http;
using Nistec.Generic;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Security;
using System.Text;

namespace Nistec.Channels
{

    /// <summary>
    /// String message stream
    /// </summary>
    [Serializable]
    public class TransString : ITransformMessage, ITransformResponse, IDisposable
    {

        #region ctor

        public TransString()
        {
            DuplexType = DuplexTypes.Respond;
            TransformType = TransformType.Json;
        }

        //public TransString(string message, bool isDuplex, int expiration, StringFormatType formatType = StringFormatType.Json)
        //{
        //    Body = message;
        //    DuplexType = isDuplex? DuplexTypes.Respond: DuplexTypes.None;
        //    Expiration = expiration;
        //    TransformType = (TransformType)(int)formatType;
        //}
        public TransString(string message, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        {
            Body = message;
            DuplexType = isDuplex ? DuplexTypes.Respond : DuplexTypes.None;
            TransformType = (TransformType)(int)formatType;
        }
        public TransString(Stream stream, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        {
            Body = ReadString(stream);
            DuplexType = isDuplex ? DuplexTypes.Respond : DuplexTypes.None;
            TransformType = (TransformType)(int)formatType;
        }
        public TransString(HttpRequestInfo request, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        {
            Body = request.Body;
            DuplexType = isDuplex ? DuplexTypes.Respond : DuplexTypes.None;
            TransformType = (TransformType)(int)formatType;
        }
        #endregion

        public string Body { get; internal set; }

        #region ITransformMessage
        /// <summary>
        /// Get or Set DuplexTypes
        /// </summary>
        public DuplexTypes DuplexType { get; set; }
        /// <summary>
        /// Get or Set TransformType
        /// </summary>
        public TransformType TransformType { get; set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Body = null;
        }
        #endregion

        #region ITransformResponse

        public void SetState(int state, string message)
        {
            //State = (MessageState)state;
            Body = message;
            TransformType = TransformType.State;
        }

        public byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(Body);
        }
        public byte[] GetBytes(Encoding encoding)
        {
            return encoding.GetBytes(Body);
        }

        #endregion

        #region Static Stream Read\Write

        public static TransString WriteState(int state, string message)
        {
            return new TransString() { Body = message, TransformType = TransformType.State, DuplexType = DuplexTypes.None };
        }
        //return new TransStream(message, TransType.State, state);

        public static int WriteString(string outString, Stream stream)
        {
            return WriteString(outString, stream, Encoding.UTF8);
        }

        public static int WriteString(string outString, Stream stream, Encoding encoding)
        {
            byte[] outBuffer = encoding.GetBytes(outString);
            int len = outBuffer.Length;
            stream.Write(outBuffer, 0, len);
            stream.Flush();
            return outBuffer.Length + 4;
        }

        public static string ReadString(Stream stream)
        {
            return ReadString(stream, Encoding.UTF8);
        }

        public static string ReadString(Stream stream, Encoding encoding)
        {
            byte[] buffer = ReadToBytes(stream, encoding);

            if (buffer == null)
                return null;
            var response = encoding.GetString(buffer);

            return response;
        }

        public static byte[] ReadToBytes(Stream stream, Encoding encoding)
        {
            byte[] buffer = null;
            if (stream is NetworkStream)
            {
                buffer = ((NetworkStream)stream).ReadStream();
            }
            else if (stream is PipeStream)
            {
                buffer = ((PipeStream)stream).ReadStream();
            }
            else if (stream is MemoryStream)
            {
                buffer = ((MemoryStream)stream).ToArray();
            }
            else if (stream is NetStream)
            {
                buffer = ((NetStream)stream).ToArray();
            }
            else
            {
                buffer = stream.ReadStream();
            }

            if (buffer == null)
                return null;

            return buffer;
        }

        public static int WriteStringWithCount(string outString, Stream stream, Encoding encoding)
        {
            byte[] outBuffer = encoding.GetBytes(outString);
            int len = outBuffer.Length;
            WriteValue(stream, len);
            stream.Write(outBuffer, 0, len);
            stream.Flush();

            return outBuffer.Length + 4;
        }
        public static string ReadStringWithCount(Stream stream, Encoding encoding)
        {
            byte[] buffer = null;
            int byteRead = 0;
            if (stream is NetworkStream)
            {
                byteRead = ((NetworkStream)stream).ReadStreamWithCount(out buffer);
            }
            else if (stream is PipeStream)
            {
                byteRead = ((PipeStream)stream).ReadStreamWithCount(out buffer);
            }
            else
            {
                byteRead = stream.ReadStreamWithCount(out buffer);
            }

            if (buffer == null)
                return null;

            var response = encoding.GetString(buffer);

            return response;
        }
        static void WriteValue(Stream stream, int value)
        {
            byte[] buffer = new byte[4];

            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 0x10);
            buffer[3] = (byte)(value >> 0x18);
            stream.Write(buffer, 0, 4);
        }

        static int ReadInt32(byte[] buffer, int offset)
        {
            return (((buffer[offset + 0] | (buffer[offset + 1] << 8)) | (buffer[offset + 2] << 0x10)) | (buffer[offset + 3] << 0x18));
        }
        #endregion

    }

}
