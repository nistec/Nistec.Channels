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
using Nistec.IO;
using Nistec.Runtime;
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
    public class StringMessage : ITransformMessage
    {
        public StringMessage()
        {
            IsDuplex = true;
        }

        public StringMessage(string message, bool isDuplex, int expiration)
        {
            Message = message;
            IsDuplex = isDuplex;
            Expiration = expiration;
        }
        public StringMessage(string message, bool isDuplex=true)
        {
            Message = message;
            IsDuplex = isDuplex;
        }
        public StringMessage(Stream stream, bool isDuplex = true)
        {
            Message = ReadString(stream);
            IsDuplex = isDuplex;
        }
        public StringMessage(HttpRequestInfo request, bool isDuplex = true)
        {
            Message= request.Body;
            IsDuplex = isDuplex;
        }

        public string Message { get; internal set; }

        #region ITransformMessage

        /// <summary>
        /// Get indicate wether the message is a duplex type.
        /// </summary>
        bool _IsDuplex;
        public bool IsDuplex
        {
            get { return _IsDuplex; }
            set
            {
                _IsDuplex = value;
                if (!value)
                    _DuplexType = DuplexTypes.None;
                else if (_DuplexType == DuplexTypes.None)
                    _DuplexType = DuplexTypes.WaitOne;
            }
        }
        //public bool IsDuplex { get { return !(DuplexType == DuplexTypes.None); } }// set { DuplexType = (value) ? DuplexTypes.NoWaite : DuplexTypes.None; } }// { get; set; }

        /// <summary>
        /// Get or Set DuplexType.
        /// </summary>
        DuplexTypes _DuplexType;
        public DuplexTypes DuplexType
        {
            get { return _DuplexType; }
            set
            {
                _DuplexType = value;
                _IsDuplex = (_DuplexType != DuplexTypes.None);
            }
        }
        /// <summary>
        ///  Get or Set The message expiration.
        /// </summary>
        public int Expiration { get; set; }
        /// <summary>
        /// Get or Set The result type name.
        /// </summary>
        public TransformType TransformType { get; set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Message = null;
        }
        #endregion

        #region Stream Read\Write
                
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
            byte[] buffer = null;
            if (stream is NetworkStream)
            {
                buffer=((NetworkStream)stream).ReadStream();
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

            var response= encoding.GetString(buffer);

            return response;
        }

        public static int WriteStringWithCount(string outString, Stream stream, Encoding encoding)
        {
            byte[] outBuffer = encoding.GetBytes(outString);
            int len = outBuffer.Length;
            WriteValue(stream,len);
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


        //[SecuritySafeCritical]
        //public unsafe static byte[] GetBytes(int value)
        //{
        //    byte[] buffer = new byte[4];
        //    fixed (byte* numRef = buffer)
        //    {
        //        *((int*)numRef) = value;
        //    }
        //    return buffer;

        //}

        static int ReadInt32(byte[] buffer, int offset)
        {
            return (((buffer[offset + 0] | (buffer[offset + 1] << 8)) | (buffer[offset + 2] << 0x10)) | (buffer[offset + 3] << 0x18));
        }
        #endregion
    }
}
