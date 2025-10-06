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
#pragma warning disable CS1591
namespace Nistec.Channels
{
    [Serializable]
    public class TransBinary : ISerialEntity, IDisposable, IDataStream
    {
        #region static encode/decode

        public static object TypeofValue(IDataStream ds, Type type)
        {
            if (ds == null || ds.IsEmpty)
                return null;
            if (ds.TypeName == type.FullName)
                return ds.ReadBody();
            return null;
        }
        public static bool TryGetValue<T>(IDataStream ds, out T value)
        {
            if (ds == null || ds.IsEmpty)
            {
                value = default(T);
                return false;
            }
            if (ds.TypeName == typeof(T).FullName)
            {
                value = (T)ds.ReadBody();
                return true;
            }
            value = default(T);
            return false;
        }

        public static string BytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
        public static byte[] StringToBytes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            return Encoding.UTF8.GetBytes(text);
        }

        public static SerialType ReadSerialType(NetStream stream)
        {
            if (stream == null)
                return SerialType.nullType;
            return (SerialType)stream.PeekByte(0);
        }

        //SerialType ReadSerialType(byte t)

        public static object Decode(NetStream stream)
        {
            if (stream == null)
                return null;
            return BinarySerializer.DeserializeFromStream(stream);
        }

        public static NetStream Encode(object value)
        {
            if (value == null)
                return null;
            return BinarySerializer.SerializeToStream(value);
        }

        /// <summary>
        /// Get content converted to object.
        /// </summary>
        /// <returns></returns>
        public static object Decode(NetStream stream, TransType transType)// string TypeName)
        {
            if (stream == null)
                return null;

            byte[] BodyStream = stream.ToArray();
            //var type = SerializeTools.GetType(TypeName);
            //return ReadBody();

            switch (transType)
            {
                case TransType.Stream:
                    //if (BodyStream == null || BodyStream.Length == 0)
                    //    return BodyStream;
                    //if (TypeName == typeof(byte[]).FullName)
                    //    return BodyStream;
                    //if (TypeName == typeof(NetStream).FullName)
                    //    return new NetStream(BodyStream);
                    //if (SerializeTools.IsEntityClassOrStructSerialize(SerializeTools.GetType(TypeName)))
                    //    return BinarySerializer.Deserialize(BodyStream);
                    //else
                    return BodyStream;
                case TransType.Csv:
                case TransType.Text:
                    return BytesToString(BodyStream);
                case TransType.Json:
                    {
                        return BytesToString(BodyStream);
                    }
                case TransType.Xml:
                    {
                        //return Xml.XSerializer.Deserialize(BytesToString(BodyStream),type);
                        return BytesToString(BodyStream);
                    }
                case TransType.Base64:
                    {
                        return BinarySerializer.DeserializeFromBase64(BytesToString(BodyStream));
                    }
                case TransType.Object:
                    {
                        return BinarySerializer.Deserialize(BodyStream);
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Set the given value to message string, for base64 set FlexType to FlexType.Stream.
        /// </summary>
        /// <param name="transType"></param>
        /// <param name="value"></param>
        public static NetStream Encode(object value, TransType transType)
        {
            //TransType = transType;
            byte[] BodyStream = null;
            string TypeName;

            if (value == null)
            {
                //TypeName = typeof(object).FullName;
                //BodyStream = null;
                ////Message = null;
                return null;
            }
            else
            {
                switch (transType)
                {
                    case TransType.Stream:
                        if (value.GetType() == typeof(byte[]))
                            BodyStream = (byte[])value;
                        else if (value.GetType() == typeof(NetStream))
                            BodyStream = ((NetStream)value).ToArray();
                        else if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        //Message = null;
                        break;
                    case TransType.Csv:
                    case TransType.Text:
                        TypeName = typeof(string).FullName;
                        BodyStream = StringToBytes(value.ToString());
                        //Message = value.ToString();
                        //BodyStream = null;
                        break;
                    case TransType.Json:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(JsonSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Xml:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(Xml.XSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Base64:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(BinarySerializer.SerializeToBase64(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Object:
                        BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        break;
                }

                if (BodyStream != null && BodyStream.Length > 0)
                {
                    return new NetStream(BodyStream);
                }
                return null;
            }
        }

        public static TransBinary FromBytes(byte[] bytes)
        {
            TransBinary tb = new TransBinary();
            using (var stream = new NetStream(bytes))
            {
                tb.EntityRead(stream, null);
                return tb;
            }
        }

        public static TransBinary FromStream(NetStream stream)
        {
            TransBinary tb = new TransBinary();
            tb.EntityRead(stream, null);
            return tb;
        }
        #endregion

        #region ctor
        public TransBinary()
        {

        }

        public TransBinary(Stream stream, IBinaryStreamer streamer = null)
        {
            EntityRead(stream, streamer);
        }
        public TransBinary(byte[] value, TransType type = TransType.Stream)
        {
            TransType = type;
            State = 0;
            if (value != null)
            {
                TypeName = value.GetType().FullName;
                SetContent(type, value);
            }
            //else
            //{
            //    TypeName = typeof(object).FullName;
            //    BodyStream = null;
            //}
        }

        public TransBinary(byte[] data, int offset, int count, TransType type = TransType.Stream)
        {
            using (var stream = new NetStream(data, offset, count))
            {
                SetContent(type, stream.ToArray());
            }
        }

        public TransBinary(int state, string message)
        {
            TransType = TransType.State;
            Message = message;
            State = state;
            TypeName = state.GetType().FullName;
            //SetContent(TransType.State, state);
        }

        public string GetBodyString()
        {
            if (BodyStream == null || BodyStream.Length == 0)
                return null;
            return Encoding.UTF8.GetString(BodyStream, 0, BodyStream.Length);
        }

        public TransStream ToTransStream()
        {
            return new TransStream() { BodyStream = this.BodyStream, Message = this.Message, State = this.State, TransType = this.TransType, TypeName = this.TypeName };
        }

        /// <summary>
        /// Get content converted to object.
        /// </summary>
        /// <returns></returns>
        public object GetContent()
        {

            switch (TransType)
            {
                case TransType.Stream:
                    if (BodyStream == null || BodyStream.Length == 0)
                        return BodyStream;
                    if (TypeName == typeof(byte[]).FullName)
                        return BodyStream;
                    if (TypeName == typeof(NetStream).FullName)
                        return new NetStream(BodyStream);
                    if (SerializeTools.IsEntityClassOrStructSerialize(SerializeTools.GetType(TypeName)))
                        return BinarySerializer.Deserialize(BodyStream);
                    else
                        return BodyStream;
                case TransType.Csv:
                case TransType.Text:
                    return GetBodyString();
                case TransType.Json:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return JsonSerializer.Deserialize(GetBodyString(), type);
                        else
                            return GetBodyString();
                    }
                case TransType.Xml:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return Xml.XSerializer.Deserialize(GetBodyString(), type);
                        else
                            return GetBodyString();
                    }
                case TransType.Base64:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return BinarySerializer.DeserializeFromBase64(GetBodyString());
                        else
                            return GetBodyString();
                    }
                case TransType.Object:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (BodyStream == null)
                            return null;
                        //BodyStream.Position = 0;
                        return BinarySerializer.Deserialize(BodyStream);
                        //return ReadBody();
                    }
                case TransType.State:
                case TransType.Ack:
                    if (!IsEmpty)
                        return BinarySerializer.Deserialize(BodyStream);
                    return this;// MessageAck.DoAck((ChannelState)State, Message);
                default:
                    return this;
            }
        }
        /// <summary>
        /// Set the given value to message string, for base64 set FlexType to FlexType.Stream.
        /// </summary>
        /// <param name="transType"></param>
        /// <param name="value"></param>
        public void SetContent(TransType transType, object value)
        {
            TransType = transType;

            if (value == null)
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
                //Message = null;
            }
            else
            {
                switch (transType)
                {
                    case TransType.Stream:
                        if (value.GetType() == typeof(byte[]))
                            BodyStream = (byte[])value;
                        else if (value.GetType() == typeof(NetStream))
                            BodyStream = ((NetStream)value).ToArray();
                        else if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        //Message = null;
                        break;
                    case TransType.Csv:
                    case TransType.Text:
                        TypeName = typeof(string).FullName;
                        BodyStream = StringToBytes(value.ToString());
                        //Message = value.ToString();
                        //BodyStream = null;
                        break;
                    case TransType.Json:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(JsonSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Xml:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(Xml.XSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Base64:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(BinarySerializer.SerializeToBase64(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Object:
                        BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        break;
                    case TransType.Ack:
                    case TransType.State:
                        TypeName = value.GetType().FullName;
                        if (Types.IsNumeric(value))
                            this.State = Types.ToInt(value);
                        if (TypeName == typeof(string).FullName)
                            this.Message = value.ToString();
                        BodyStream = BinarySerializer.SerializeToBytes(value);
                        break;
                }
            }
        }
        
        #endregion

        #region Stream / properties
        public string Message { get; set; }
        //public GenericNameValue Header { get; set; }
        public string TypeName { get; set; }
        public TransType TransType { get; set; }
        public int State { get; set; }
        public byte[] BodyStream { get; set; }

        public bool IsEmpty
        {
            get { return BodyStream == null || BodyStream.Length == 0; }
        }

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
            streamer.WriteString(Message);
            //streamer.WriteValue(Header);
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
            Message = streamer.ReadString();
            //Header = streamer.ReadValue<GenericNameValue>();
            BodyStream = (byte[])streamer.ReadValue();

        }

        #endregion

        #region Encode/Decode Body

        public override string ToString()
        {
            return string.Format("TypeName: {0}, TransType: {1}, Size: {2}", TypeName, TransType.ToString(), BodyStream == null ? 0 : BodyStream.Length);
        }

        public byte[] DataStream()
        {
            return BodyStream;
        }
        public byte[] GetBytes()
        {
            return ToStream().ToArray();
        }
        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }


        public static TransBinary Deserialize(byte[] bytes)
        {
            return new TransBinary(bytes);
        }
        public byte[] Serialize()
        {
            return GetStream().ToArray();
        }



        public NetStream GetStream()
        {
            NetStream stream = new NetStream();
            EntityRead(stream, null);
            return stream;
        }



        public string ToJson(bool pretty = false)
        {
            return GenericKeyValue.Create("TransType", TransType, "State", State, "TypeName", TypeName, "Message", Message, "Body", ReadBody()).ToJson(pretty);
        }

        public virtual object ReadBody()
        {
            if (BodyStream == null)
                return null;
            //BodyStream.Position = 0;
            return BinarySerializer.Deserialize(BodyStream);
        }

        public T ReadBody<T>()
        {
            if (BodyStream == null)
                return default(T);
            //BodyStream.Position = 0;
            return BinarySerializer.Deserialize<T>(BodyStream);
        }

        public string BodyToJson(bool pretty = false)
        {
            return JsonSerializer.Serialize(ReadBody(), pretty);
        }

        public IAck ToAck()
        {
            if (State > 0 && State <= 299)
                return MessageAck.DoOk();
            else
                return MessageAck.DoAck((ChannelState)State, Message);
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

        #region static

        public static bool IsEmptyStream(TransBinary ts)
        {
            return (ts == null || ts.IsEmpty);
        }

        #endregion

    }

#if (false)
    [Serializable]
    public class TransBinary : ISerialEntity,IDisposable, IDataStream
    {
    #region static encode/decode

        public static object TypeofValue(IDataStream ds, Type type)
        {
            if (ds == null || ds.IsEmpty)
                return null;
            if (ds.TypeName == type.FullName)
                return ds.ReadBody();
            return null;
        }
        public static bool TryGetValue<T>(IDataStream ds,out T value )
        {
            if (ds == null || ds.IsEmpty)
            {
                value = default(T);
                return false;
            }
            if (ds.TypeName == typeof(T).FullName)
            {
                value=(T) ds.ReadBody();
                return true;
            }
            value = default(T);
            return false;
        }

        public static string BytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
        public static byte[] StringToBytes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            return Encoding.UTF8.GetBytes(text);
        }

        public static SerialType ReadSerialType(NetStream stream)
        {
            if (stream == null)
                return SerialType.nullType;
            return (SerialType) stream.PeekByte(0);
        }

        //SerialType ReadSerialType(byte t)

        public static object Decode(NetStream stream)
        {
            if (stream == null)
                return null;
            return BinarySerializer.DeserializeFromStream(stream);
        }

        public static NetStream Encode(object value)
        {
            if (value == null)
                return null;
            return BinarySerializer.SerializeToStream(value);
        }

        /// <summary>
        /// Get content converted to object.
        /// </summary>
        /// <returns></returns>
        public static object Decode(NetStream stream, TransType transType)// string TypeName)
        {
            if (stream == null)
                return null;

            byte[] BodyStream = stream.ToArray();
            //var type = SerializeTools.GetType(TypeName);
            //return ReadBody();

            switch (transType)
            {
                case TransType.Stream:
                    //if (BodyStream == null || BodyStream.Length == 0)
                    //    return BodyStream;
                    //if (TypeName == typeof(byte[]).FullName)
                    //    return BodyStream;
                    //if (TypeName == typeof(NetStream).FullName)
                    //    return new NetStream(BodyStream);
                    //if (SerializeTools.IsEntityClassOrStructSerialize(SerializeTools.GetType(TypeName)))
                    //    return BinarySerializer.Deserialize(BodyStream);
                    //else
                    return BodyStream;
                case TransType.Csv:
                case TransType.Text:
                    return BytesToString(BodyStream);
                case TransType.Json:
                    {
                        return BytesToString(BodyStream);
                    }
                case TransType.Xml:
                    {
                        //return Xml.XSerializer.Deserialize(BytesToString(BodyStream),type);
                        return BytesToString(BodyStream);
                    }
                case TransType.Base64:
                    {
                        return BinarySerializer.DeserializeFromBase64(BytesToString(BodyStream));
                    }
                case TransType.Object:
                    {
                        return BinarySerializer.Deserialize(BodyStream);
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Set the given value to message string, for base64 set FlexType to FlexType.Stream.
        /// </summary>
        /// <param name="transType"></param>
        /// <param name="value"></param>
        public static NetStream Encode(object value, TransType transType)
        {
            //TransType = transType;
            byte[] BodyStream = null;
            string TypeName;

            if (value == null)
            {
                //TypeName = typeof(object).FullName;
                //BodyStream = null;
                ////Message = null;
                return null;
            }
            else
            {
                switch (transType)
                {
                    case TransType.Stream:
                        if (value.GetType() == typeof(byte[]))
                            BodyStream = (byte[])value;
                        else if (value.GetType() == typeof(NetStream))
                            BodyStream = ((NetStream)value).ToArray();
                        else if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        //Message = null;
                        break;
                    case TransType.Csv:
                    case TransType.Text:
                        TypeName = typeof(string).FullName;
                        BodyStream = StringToBytes(value.ToString());
                        //Message = value.ToString();
                        //BodyStream = null;
                        break;
                    case TransType.Json:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(JsonSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Xml:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(Xml.XSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Base64:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(BinarySerializer.SerializeToBase64(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Object:
                        BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        break;
                }

                if (BodyStream != null && BodyStream.Length > 0)
                {
                    return new NetStream(BodyStream);
                }
                return null;
            }
        }
        
        public static TransBinary FromBytes(byte[] bytes)
        {
            TransBinary tb = new TransBinary();
            using (var stream = new NetStream(bytes))
            {
                tb.EntityRead(stream, null);
                return tb;
            }
        }

        public static TransBinary FromStream(NetStream stream)
        {
            TransBinary tb = new TransBinary();
            tb.EntityRead(stream, null);
            return tb;
        }
    #endregion

    #region ctor
        public TransBinary()
        {

        }

        public TransBinary(Stream stream, IBinaryStreamer streamer= null)
        {
            EntityRead(stream, streamer);
        }
        public TransBinary(object value, TransType type = TransType.Object)//, string command=null)
        {
            TransType = type;
            State = 0;
            if (value != null)
            {
                TypeName = value.GetType().FullName;
                SetContent(type, value);
                //using (NetStream ns = new NetStream())
                //{
                //    var ser = new BinarySerializer();
                //    ser.Serialize(ns, value);
                //    ns.Position = 0;
                //    BodyStream = ns.ToArray();
                //}
            }
            else
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
            }
        }
        public TransBinary(object value, string command, TransType type = TransType.Object)
        {
            TransType = type;
            Message = command;
            State = 0;
            if (value != null)
            {
                TypeName = value.GetType().FullName;
                SetContent(type, value);
                //using (NetStream ns = new NetStream())
                //{
                //    var ser = new BinarySerializer();
                //    ser.Serialize(ns, value);
                //    ns.Position = 0;
                //    BodyStream = ns.ToArray();
                //}
            }
            else
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
            }
        }

        public TransBinary(byte[] value, TransType type = TransType.Stream)
        {
            TransType = type;
            //Command = command;
            State = 0;
            if (value != null)
            {
                TypeName = value.GetType().FullName;
                SetContent(type, value);
            }
            else
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
            }
        }

        public TransBinary(byte[] data, int offset, int count, TransType type=TransType.Stream)
        {
            using (var stream = new NetStream(data, offset, count))
            {
                SetContent(type, stream.ToArray());
            }
        }

        public TransBinary(int state, string message)
        {
            TransType = TransType.State;
            Message = message;
            State = state;
            TypeName = state.GetType().FullName;
            //SetContent(TransType.State, state);
        }

        public string GetBodyString()
        {
            if (BodyStream == null || BodyStream.Length == 0)
                return null;
            return Encoding.UTF8.GetString(BodyStream, 0, BodyStream.Length);
        }

        public TransStream ToTransStream()
        {
            return new TransStream() { BodyStream = this.BodyStream, Message = this.Message, State = this.State, TransType = this.TransType, TypeName = this.TypeName };
            //if (BodyStream != null && BodyStream.Length > 0)
            //    return new TransStream(BodyStream, TransType);// (BodyStream, 0, BodyStream.Length, TransType);
            //return new TransStream("NA", TransType);//, State);
        }

        //public byte[] StringToBytes(string text)
        //{
        //    if (string.IsNullOrEmpty(text))
        //        return null;
        //    return Encoding.UTF8.GetBytes(text);
        //}

        /// <summary>
        /// Get content converted to object.
        /// </summary>
        /// <returns></returns>
        public object GetContent()
        {

            switch (TransType)
            {
                case TransType.Stream:
                    if (BodyStream == null || BodyStream.Length == 0)
                        return BodyStream;
                    if (TypeName == typeof(byte[]).FullName)
                        return BodyStream;
                    if (TypeName == typeof(NetStream).FullName)
                        return new NetStream(BodyStream);
                    if (SerializeTools.IsEntityClassOrStructSerialize(SerializeTools.GetType(TypeName)))
                        return BinarySerializer.Deserialize(BodyStream);
                    else
                        return BodyStream;
                case TransType.Csv:
                case TransType.Text:
                    return GetBodyString();
                case TransType.Json:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return JsonSerializer.Deserialize(GetBodyString(), type);
                        else
                            return GetBodyString();
                    }
                case TransType.Xml:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return Xml.XSerializer.Deserialize(GetBodyString(), type);
                        else
                            return GetBodyString();
                    }
                case TransType.Base64:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return BinarySerializer.DeserializeFromBase64(GetBodyString());
                        else
                            return GetBodyString();
                    }
                case TransType.Object:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (BodyStream == null)
                            return null;
                        //BodyStream.Position = 0;
                        return BinarySerializer.Deserialize(BodyStream);
                        //return ReadBody();
                    }
                case TransType.State:
                case TransType.Ack:
                    if (!IsEmpty)
                        return BinarySerializer.Deserialize(BodyStream);
                    return this;// MessageAck.DoAck((ChannelState)State, Message);
                default:
                    return this;
            }
        }
        /// <summary>
        /// Set the given value to message string, for base64 set FlexType to FlexType.Stream.
        /// </summary>
        /// <param name="transType"></param>
        /// <param name="value"></param>
        public void SetContent(TransType transType, object value)
        {
            TransType = transType;

            if (value == null)
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
                //Message = null;
            }
            else
            {
                switch (transType)
                {
                    case TransType.Stream:
                        if (value.GetType() == typeof(byte[]))
                            BodyStream = (byte[])value;
                        else if (value.GetType() == typeof(NetStream))
                            BodyStream = ((NetStream)value).ToArray();
                        else if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        //Message = null;
                        break;
                    case TransType.Csv:
                    case TransType.Text:
                        TypeName = typeof(string).FullName;
                        BodyStream = StringToBytes(value.ToString());
                        //Message = value.ToString();
                        //BodyStream = null;
                        break;
                    case TransType.Json:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(JsonSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Xml:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(Xml.XSerializer.Serialize(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Base64:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            BodyStream = StringToBytes(BinarySerializer.SerializeToBase64(value));
                        else
                            BodyStream = StringToBytes(value.ToString());
                        TypeName = value.GetType().FullName;
                        //BodyStream = null;
                        break;
                    case TransType.Object:
                        BodyStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        break;
                    case TransType.Ack:
                    case TransType.State:
                        TypeName = value.GetType().FullName;
                        if (Types.IsNumeric(value))
                            this.State = Types.ToInt(value);
                        if (TypeName == typeof(string).FullName)
                            this.Message = value.ToString();
                        BodyStream = BinarySerializer.SerializeToBytes(value);
                        break;
                }
            }
        }
        //public TransBinary(object value, string command, string[] keyValueMessage, TransType type = TransType.Object)
        //{
        //    TransType = type;
        //    Command = command;
        //    State = 0;
        //    Header = new GenericNameValue(keyValueMessage);

        //    if (value != null)
        //    {
        //        TypeName = value.GetType().FullName;

        //        using (NetStream ns = new NetStream())
        //        {
        //            var ser = new BinarySerializer();
        //            ser.Serialize(ns, value);
        //            ns.Position = 0;
        //            BodyStream = ns.ToArray();
        //        }
        //    }
        //    else
        //    {
        //        TypeName = typeof(object).FullName;
        //        BodyStream = null;
        //    }
        //}
    #endregion

    #region Stream / properties
        public string Message { get; set; }
        //public GenericNameValue Header { get; set; }
        public string TypeName { get; set; }
        public TransType TransType { get; set; }
        public int State { get; set; }
        public byte[] BodyStream { get; set; }

        public bool IsEmpty
        {
            get { return BodyStream == null || BodyStream.Length == 0; }
        }

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
            streamer.WriteString(Message);
            //streamer.WriteValue(Header);
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
            Message = streamer.ReadString();
            //Header = streamer.ReadValue<GenericNameValue>();
            BodyStream = (byte[])streamer.ReadValue();

        }

    #endregion

    #region Encode/Decode Body

        public override string ToString()
        {
            return string.Format("TypeName: {0}, TransType: {1}, Size: {2}", TypeName, TransType.ToString(), BodyStream == null ? 0 : BodyStream.Length);
        }

        public byte[] DataStream()
        {
            return BodyStream;
        }
        public byte[] GetBytes()
        {
            return ToStream().ToArray();
        }
        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }

      
        public static TransBinary Deserialize(byte[] bytes)
        {
            return new TransBinary(bytes);
        }
        public byte[] Serialize()
        {
            return GetStream().ToArray();
        }

 

        public NetStream GetStream()
        {
            NetStream stream = new NetStream();
            EntityRead(stream, null);
            return stream;
        }

        

        public string ToJson(bool pretty = false)
        {
            return GenericKeyValue.Create("TransType", TransType, "State", State, "TypeName", TypeName, "Message", Message, "Body", ReadBody()).ToJson(pretty);
        }

        public virtual object ReadBody()
        {
            if (BodyStream == null)
                return null;
            //BodyStream.Position = 0;
            return BinarySerializer.Deserialize(BodyStream);
        }

        public T ReadBody<T>()
        {
            if (BodyStream == null)
                return default(T);
            //BodyStream.Position = 0;
            return BinarySerializer.Deserialize<T>(BodyStream);
        }

        //public T ReadBody<T>()
        //{
        //    return GenericTypes.Cast<T>(ReadBody(), true);
        //}

        public string BodyToJson(bool pretty = false)
        {
            return JsonSerializer.Serialize(ReadBody(), pretty);
        }

        public IAck ToAck()
        {
            if (State > 0 && State <= 299)
                return MessageAck.DoOk();
            else
                return MessageAck.DoAck((ChannelState)State, Message);
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

    #region static

        public static bool IsEmptyStream(TransBinary ts)
        {
            return (ts == null || ts.IsEmpty);
        }

    #endregion

    }

#endif

}
