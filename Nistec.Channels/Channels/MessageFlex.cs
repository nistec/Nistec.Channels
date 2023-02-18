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
    public class MessageFlex : ITransformMessage, ITransformResponse, ISerialEntity
    {

        #region ctor

        public MessageFlex()
        {
            IsDuplex = true;
            TransformType = TransformType.Json;
        }

        //public MessageFlex(string message, bool isDuplex, int expiration, StringFormatType formatType = StringFormatType.Json)
        //{
        //    Message = message;
        //    IsDuplex = isDuplex;
        //    Expiration = expiration;
        //    TransformType =(TransformType)(int) formatType;
        //}
        public MessageFlex(string message, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        {
            Message = message;
            IsDuplex = isDuplex;
            TransformType = (TransformType)(int)formatType;
        }
        //public MessageFlex(Stream stream, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        //{
        //    Message = ReadString(stream);
        //    IsDuplex = isDuplex;
        //    TransformType = (TransformType)(int)formatType;
        //}
        public MessageFlex(HttpRequestInfo request, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        {
            Message= request.Body;
            IsDuplex = isDuplex;
            TransformType = (TransformType)(int)formatType;
        }

        /// <summary>
        /// Initialize a new instance of MessageFlex from stream using for <see cref="ISerialEntity"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        protected MessageFlex(Stream stream, IBinaryStreamer streamer)
        {
            EntityRead(stream, streamer);
        }

        protected MessageFlex(string json, IJsonSerializer streamer)
        {
            EntityRead(json, streamer);
        }

        #endregion

        #region properties

        public string Message { get; set; }
        public string Command { get; set; }
        public string Sender { get; set; }
        public string Label { get; set; }
        public string EncodingName { get; set; }
        public int State { get; internal set; }
        #endregion

        #region  ISerialEntity

        /// <summary>
        /// Write the current object include the body and properties to stream using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public virtual void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            //NetStream nsetstream = ReadToNetStream(stream);

            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteValue((byte)TransformType);
            //streamer.WriteValue((byte)StringFormatType);
            streamer.WriteString(Message);
            streamer.WriteString(Label);
            streamer.WriteString(Command);
            streamer.WriteString(Sender);
            streamer.WriteValue((int)DuplexType);
            streamer.WriteValue(Expiration);
            streamer.WriteString(EncodingName);
            streamer.WriteValue((int)State);
            streamer.Flush();
        }


        /// <summary>
        /// Read stream to the current object include the body and properties using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public virtual void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            //NetStream nsetstream = ReadToNetStream(stream);

            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            TransformType = (TransformType)streamer.ReadValue<byte>();
            Message = streamer.ReadString();
            Label = streamer.ReadString();
            Command = streamer.ReadString();
            Sender = streamer.ReadString();
            DuplexType = (DuplexTypes)streamer.ReadValue<int>();
            Expiration = streamer.ReadValue<int>();
            EncodingName = Types.NZorEmpty(streamer.ReadString(), "utf-8");
            State = streamer.ReadValue<int>();
        }

        #endregion

        #region ISerialJson

        public virtual string EntityWrite(IJsonSerializer serializer, bool pretty = false)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Write, null);

            //object body = null;
            //if (BodyStream != null)
            //{
            //    body = BinarySerializer.ConvertFromStream(BodyStream);
            //}
            serializer.WriteToken("TransformType", TransformType);
            serializer.WriteToken("Message", Message);
            serializer.WriteToken("Label", Label, null);
            serializer.WriteToken("Command", Command);
            serializer.WriteToken("Sender", Sender);
            serializer.WriteToken("DuplexType", (int)DuplexType);
            serializer.WriteToken("Expiration", Expiration);
            serializer.WriteToken("EncodingName", EncodingName);
            serializer.WriteToken("State", State);
            return serializer.WriteOutput(pretty);
        }

        public virtual object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            var dic = serializer.Read<Dictionary<string, object>>(json);

            if (dic != null)
            {
                TransformType = (TransformType)dic.GetEnum<TransformType>("TransformType", TransformType.Json);
                Message = dic.Get<string>("Message");
                Label = dic.Get<string>("Label");
                Command = dic.Get<string>("Command");
                Sender = dic.Get<string>("Sender");
                DuplexType = (DuplexTypes)dic.Get<int>("DuplexType");
                Expiration = dic.Get<int>("Expiration");
                EncodingName = Types.NZorEmpty(dic.Get<string>("EncodingName"), "utf-8");
                State = dic.Get<int>("State");
            }

            return this;
        }

        #endregion

        #region Read/Write
        /// <summary>
        /// Get message as Stream.
        /// </summary>
        /// <returns></returns>
        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }

        /// <summary>
        /// Convert stream to <see cref="MessageFlex"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MessageFlex Parse(Stream stream)
        {
            return new MessageFlex(stream, null);
        }

        public static MessageFlex Parse(string text, StringFormatType format = StringFormatType.Json)
        {
            return new MessageFlex(text, new JsonSerializer(JsonSerializerMode.Read, null)); 
        }

        /// <summary>
        /// Get message as Stream.
        /// </summary>
        /// <returns></returns>
        public string ToJson(bool pretty = false)
        {
            return EntityWrite(new JsonSerializer(JsonSerializerMode.Write, null), pretty);
        }
        
        #endregion

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

        TransformType _TransformType;
        /// <summary>
        /// Get or Set The result type name.
        /// </summary>
        public TransformType TransformType
        {
            get { return _TransformType; }
            set
            {
                if (!(value == TransformType.Json || value == TransformType.Base64 || value == TransformType.Csv || value == TransformType.Text || value == TransformType.None))
                    _TransformType = TransformType.None;
                else
                    _TransformType = value;
            }
        }
        //public TransformType TransformType { get; set; }

        public StringFormatType FormatType { get { return (StringFormatType)(int)TransformType; } }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            Message = null;
        }
        #endregion

        #region ITransformResponse

        public void SetState(int state, string message)
        {
            State = state;
            Message = message;
            TransformType = TransformType.State;
        }

        public byte[] GetBytes()
        {
            return ToStream().ToArray();
        }
        public byte[] GetMessageBytes()
        {
            return Encoding.UTF8.GetBytes(Message);
        }
        public byte[] GetMessageBytes(Encoding encoding)
        {
            return encoding.GetBytes(Message);
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

        #endregion

        #region Static Stream Read\Write

        public static MessageFlex WriteState(int state,string message)
        {
            return new MessageFlex() { Message = message, TransformType = TransformType.State, DuplexType = DuplexTypes.None, State=state };
        }

        //public static int WriteString(string outString, Stream stream)
        //{
        //    return WriteString(outString, stream, Encoding.UTF8);
        //}

        //public static int WriteString(string outString, Stream stream, Encoding encoding)
        //{
        //    byte[] outBuffer = encoding.GetBytes(outString);
        //    int len = outBuffer.Length;
        //    stream.Write(outBuffer, 0, len);
        //    stream.Flush();
        //    return outBuffer.Length + 4;
        //}

        public static string ReadString(Stream stream)
        {
            return ReadString(stream, Encoding.UTF8);
        }

        public static string ReadString(Stream stream, Encoding encoding)
        {
            byte[] buffer = ReadToBytes(stream);

            if (buffer == null)
                return null;
            var response= encoding.GetString(buffer);

            return response;
        }

        public static NetStream ReadToNetStream(Stream stream)
        {
            if (stream is NetStream)
            {
                return (NetStream)stream;
            }
            
            byte[] buffer = ReadToBytes(stream);

            if (buffer == null)
                return null;
            return new NetStream(buffer);
        }

        public static byte[] ReadToBytes(Stream stream)
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

        static int ReadInt32(byte[] buffer, int offset)
        {
            return (((buffer[offset + 0] | (buffer[offset + 1] << 8)) | (buffer[offset + 2] << 0x10)) | (buffer[offset + 3] << 0x18));
        }
        #endregion

    }
}
