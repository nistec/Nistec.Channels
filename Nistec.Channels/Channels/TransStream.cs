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
using System.Threading.Tasks;
#pragma warning disable CS1591
namespace Nistec.Channels
{

    [Serializable]
    public class TransStream : ISerialEntity, IDisposable , ITransformResponse, IDataStream
    {

        #region static converters

        //public static object TypeofValue(IDataStream ds, Type type)
        //{
        //    if (ds == null || ds.IsEmpty)
        //        return null;
        //    if (ds.TypeName == type.FullName)
        //        return ds.ReadBody();
        //    return null;
        //}

        public static bool TryGetValue<T>(IDataStream ds, out T value)
        {
            try
            {
                if (ds == null || ds.IsEmpty)
                {
                    value = default(T);
                    return false;
                }
                var oval = ds.ReadBody();
                if (oval.GetType() == typeof(T) || oval.GetType().IsAssignableFrom(typeof(T)))
                {
                    value = (T)oval;
                    return true;
                }
                value = default(T);
                return false;
            }
            catch (Exception ex)
            {
                value = default(T);
                return false;
            }
        }
        public static string BodyString(IDataStream ds)
        {
            var BodyStream = ds.DataStream();
            if (BodyStream == null)
                return null;
            return Encoding.UTF8.GetString(BodyStream, 0, BodyStream.Length);
        }

        public static TransStream FromStream(NetStream stream)
        {
            TransStream tb = new TransStream();
            tb.EntityRead(stream, null);
            return tb;
        }

        public static TransStream FromBytes(byte[] bytes)
        {
            TransStream tb = new TransStream();
            tb.EntityRead(new NetStream(bytes), null);
            return tb;
        }
        #endregion

        #region ITransformResponse

        public void SetState(int state, string message)
        {
            TransType = TransType.State;
            Message = message;
            State = state;
            TypeName = state.GetType().FullName;
        }

        //public byte[] GetBytes()
        //{
        //    return Stream.ToArray();
        //}

        #endregion
        
        #region ctor
        public TransStream()
        {

        }

        public TransStream(Stream stream, IBinaryStreamer streamer = null)
        {
            EntityRead(stream, streamer);
        }
        public TransStream(object value, TransType type = TransType.Object)//, string command = null)
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
        public TransStream(object value, string command, TransType type = TransType.Object)
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

        public TransStream(byte[] value, TransType type = TransType.Stream)
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

        public TransStream(int state, string message)
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

        //public TransStream ToTransStream()
        //{
        //    if (BodyStream != null && BodyStream.Length > 0)
        //        return new TransStream(BodyStream, 0, BodyStream.Length, TransType);
        //    return new TransStream("NA", TransType, State);
        //}

        public byte[] StringToBytes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            return Encoding.UTF8.GetBytes(text);
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

        #region cor Legacy

        public TransStream(NetworkStream stream, int readTimeout, int bufferSize, TransformType transform)
        {
            using (var ns = new NetStream())
            {
                ns.CopyBlock(stream, readTimeout, bufferSize);
                SetContent(ToTransType(transform), ns.ToArray());
            }
        }
        public TransStream(PipeStream stream, int bufferSize, TransformType transform)
        {
            using (var ns = new NetStream())
            {
                ns.CopyBlock(stream, bufferSize);
                SetContent(ToTransType(transform), ns.ToArray());
            }
        }

        public TransStream(NetStream stream, TransformType transform)
        {
            SetContent(ToTransType(transform), stream.ToArray());
        }

        public TransStream(byte[] data, int offset, int count, TransType type)
        {
            using (var stream = new NetStream(data, offset, count))
            {
                SetContent(type, stream.ToArray());
            }
        }

        public TransStream(NetworkStream stream, int readTimeout, int bufferSize, TransformType transform, bool isTransStream)
        {
            using (var ns = new NetStream())
            {
                //stream contains transType
                if (isTransStream)//transform == TransformType.Stream)
                {
                    ns.CopyBlock(stream, readTimeout, bufferSize);
                    EntityRead(ns, null);
                }
                else
                {
                    ns.CopyBlock(stream, readTimeout, bufferSize);
                    SetContent(ToTransType(transform), ns.ToArray());
                }
            }
        }

        public TransStream(PipeStream stream, int bufferSize, TransformType transform, bool isTransStream)
        {
            using (var ns = new NetStream())
            {
                //stream contains transType
                if (isTransStream)//transform == TransformType.Stream)
                {
                    ns.CopyBlock(stream, bufferSize);
                    EntityRead(ns, null);
                }
                else
                {
                    ns.CopyBlock(stream, bufferSize);
                    //WriteTrans(ns, ToTransType(transform));
                }
                SetContent(ToTransType(transform), ns.ToArray());
            }
        }

        public TransStream(NetStream stream, TransformType transform, bool isTransStream = false)
        {
            if (isTransStream)
            {
                EntityRead(stream, null);
            }
            else
            {
                SetContent(ToTransType(transform), stream.ToArray());
            }
         }


        #endregion

        #region static TransType

        public static bool IsEmptyStream(TransStream ts)
        {
            return (ts == null || ts.IsEmpty);
        }

        public static TransType PeekTransType(NetStream stream)
        {
            if (stream == null)
                return TransType.None;
            if (stream.PeekByte(0) == (byte)2)
            {
                return (TransType)stream.PeekByte(1);
            }
            return TransType.None;
        }
        public static TransType ToTransType(TransformType type)
        {
            return (TransType)(int)type;
        }

        public static TransType ToTransType(StringFormatType format)
        {
            return (TransType)format;
        }
        public static TransformType ToTransformType(StringFormatType format)
        {
            return (TransformType)format;
        }

        //public static StringFormatType ToStringFormatType(string message)
        //{
        //    if (string.IsNullOrEmpty(message))
        //        return StringFormatType.None;
        //    if (Strings.IsJsonString(message))
        //        return StringFormatType.Json;
        //    if (Strings.IsXmlString(message))
        //        return StringFormatType.Xml;
        //    if (Strings.IsBase64String(message))
        //        return StringFormatType.Base64;
        //    else
        //        return StringFormatType.Text;
        //}

        //public static TransType ToTransType(Type type, StringFormatType format = StringFormatType.Json)
        //{
        //    if (type == typeof(TransStream))
        //        return TransType.Stream;
        //    else if (SerializeTools.IsStream(type))
        //        return TransType.Stream;
        //    else if (type == typeof(string))
        //        return ToTransType(format);// TransType.Json;
        //    else //if (type == typeof(object))
        //        return TransType.Object;

        //}
        public static TransformType ToTransformType(Type type, StringFormatType format = StringFormatType.Json)
        {
            if (type == typeof(TransStream))
                return TransformType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransformType.Stream;
            else if (type == typeof(string))
                return ToTransformType(format);// TransformType.Json;
            else //if (type == typeof(object))
                return TransformType.Object;

        }
        #endregion

        #region IDataStream

        public byte[] DataStream()
        {
            return BodyStream;
        }
        public byte[] GetBytes()
        {
            return ToStream().ToArray();
        }
        public virtual object ReadBody()
        {
            if (BodyStream == null)
                return null;
            if(IsEmptyStream(BodyStream))
                return "NA";
            //BodyStream.Position = 0;
            var ser = new BinarySerializer();
            return ser.Deserialize(new NetStream(BodyStream));
        }
        //string 'NA'
        public static bool IsEmptyStream(byte[] buf)
        {
            if (buf == null)
                return true;
            return (buf.Length == 4 && buf[0] == 11 && buf[1] == 2 && buf[2] == 78 && buf[3] == 65);
        }

        //string 'NA'
        public static byte[] EmptyStream()
        {
            return new byte[] { 11, 2, 78, 65 };
        }

        public string TypeName { get; set; }
        //string ToJson();
        public TransType TransType { get; set; }
      
        public string Message { get; set; }

        public bool IsEmpty
        {
            get { return BodyStream == null || BodyStream.Length == 0; }
        }
        //T ReadBody<T>();
        //object GetContent();

        #endregion

        #region Stream / properties

        //public GenericNameValue Header { get; set; }

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

        public static bool IsTransStream(Stream stream)
        {
            try
            {
                var streamer = new BinaryStreamer(stream);

                var TransType = (TransType)streamer.ReadValue<byte>();
                var State = streamer.ReadValue<int>();
                var TypeName = streamer.ReadString();
                var Message = streamer.ReadString();
                //Header = streamer.ReadValue<GenericNameValue>();
                var BodyStream = (byte[])streamer.ReadValue();

                return Type.GetType(TypeName) != null && Enum.IsDefined(typeof(TransType), TransType) && TransType != TransType.None;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region Encode/Decode Body

        public override string ToString()
        {
            return string.Format("TypeName: {0}, TransType: {1}, Size: {2}", TypeName, TransType.ToString(), BodyStream == null ? 0 : BodyStream.Length);
        }

        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
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

        public string ReadToJson()
        {
            try
            {
                object Value = ReadBody();
                if (Value != null)
                {
                    if (typeof(IAck).IsAssignableFrom(Value.GetType()))
                        return ((IAck)Value).ToJson();

                    return JsonSerializer.Serialize(Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                //State = -1;
                return null;
            }
            return null;
        }

        
        public T ReadBody<T>()
        {
            if (BodyStream == null)
                return default(T);
            //BodyStream.Position = 0;
            var ser = new BinarySerializer();
            return ser.Deserialize<T>(new NetStream(BodyStream));
        }
        public virtual object ReadValue()
        {
            return ReadBody();
        }
        public T ReadValue<T>()
        {
            return ReadBody<T>();
        }
        public object ReadValue(Action<string> onFault)
        {
            try
            {
                return ReadBody();
            }
            catch (Exception ex)
            {
                //State = -1;
                onFault(ex.Message);
                return null;
            }
        }

        public T ReadValue<T>(Action<string> onFault)
        {
            try
            {
                return ReadBody<T>();
            }
            catch (Exception ex)
            {
                //State = -1;
                onFault(ex.Message);
                return default(T);
            }
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

        //public static bool IsEmptyStream(TransBinary ts)
        //{
        //    return (ts == null || ts.IsEmpty);
        //}

        public static object ReadValue(NetStream stream)
        {
            try
            {
                var ts = new TransStream(stream, null);
                return ts.GetContent();
            }
            catch
            {
                return null;
            }
        }

        public static T ReadValue<T>(NetStream stream)
        {
            try
            {
                var value = ReadValue(stream);
                return GenericTypes.Cast<T>(value, true);
            }
            catch
            {
                return default(T);
            }
        }

        #endregion

        #region Static Write

        public static async Task<TransStream> WriteAsync(object value, TransType type)
        {
            return await Task.FromResult(new TransStream(value, type));
        }
        public static async Task<TransStream> WriteAsync(NetStream stream, TransType transformType)
        {
            if (stream != null)
                stream.Position = 0;
            return await Task.FromResult(new TransStream(stream, transformType));
        }
        public static async Task<TransStream> WriteStateAsync(int state, string message)
        {
            return await Task.FromResult(new TransStream(state,message, TransType.State));
        }

        public static TransStream Write(object value, TransType type)
        {
            return new TransStream(value, type);
        }
        public static TransStream Write(object value, TransformType type)
        {
            return new TransStream(value, ToTransType(type));
        }

        public static TransStream Write(NetStream stream, TransType transformType)
        {
            if (stream != null)
                stream.Position = 0;
            return new TransStream(stream, transformType);
        }

        public static TransStream WriteState(int state, string message)
        {
            return new TransStream(state, message, TransType.State);
        }

        public static TransStream WriteAck(int state, string message)
        {
            return new TransStream(new MessageAck((ChannelState)state, message), TransType.Ack);
        }

        public static TransStream WriteBody(IBodyStream bs, string action, TransType transformType)
        {
            if (bs == null)
            {
                return new TransStream(MessageAck.DoAck(ChannelState.ItemNotFound, action + ", Item Not Found"), TransType.Ack);
                //return new TransStream(action + ", Item Not Found", TransType.Error);
            }
            else
                return new TransStream(bs.GetStream(), transformType);
        }
        public static TransStream Write(object item, string action, TransType transformType)
        {
            if (item == null)
                return new TransStream(MessageAck.DoAck(ChannelState.ItemNotFound, action + ", Item Not Found"), TransType.Ack);  //return new TransStream(action + ", Item Not Found", TransType.Error);
            else
                return new TransStream(item, transformType);// ToTransType(transformType));
        }

        public static TransStream CopyFrom(NetworkStream stream, int readTimeout, int bufferSize = 8192)
        {
            using (var nstream = new NetStream())
            {
                nstream.CopyBlock(stream, readTimeout, bufferSize);
                return new TransStream(nstream.ToArray());
            }
        }

        public static TransStream CopyFrom(PipeStream stream, int bufferSize = 8192)
        {
            using (var nstream = new NetStream())
            {
                nstream.CopyBlock(stream, bufferSize);
                return new TransStream(nstream.ToArray());
            }
        }

        public static TransStream CopyFromStream(Stream stream, int bufferSize = 4096)
        {
            byte[] buffer = new byte[bufferSize];
            using (var nstream = new NetStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    nstream.Write(buffer, 0, count);

                } while (count != 0);
                return new TransStream(nstream.ToArray());
            }
        }
        public static NetStream ToStream(object value)
        {
            NetStream ns = new NetStream();
            IBinaryStreamer streamer = new BinaryStreamer(ns);
            streamer.WriteValue(value);
            streamer.Flush();
            return ns;
        }
        #endregion

        #region TransStream legacy

        public static bool IsTransStream(Type type)
        {
            return type == typeof(TransStream);
        }

        public bool IsTransStream()
        {
            return IsTransStream(new NetStream(BodyStream));
        }
        public int ReadState()
        {
            return State;
        }

        public static string ReadJson(NetStream stream)
        {
            var o = ReadValue(stream);
            return (o == null) ? null : JsonSerializer.Serialize(o);
            //return TransStream.ReadJson(stream, (message) => { throw new Exception(message); });
        }
        #endregion

        #region Readers

        public string ReadToText()
        {
            try
            {
                object Value = ReadBody();
                if (Value != null)
                {
                    if (Value is string)
                        return Value.ToString();
                    if (Value is NetStream)
                        return NetStream.ReadTo((NetStream)Value);
                    if (typeof(IAck).IsAssignableFrom(Value.GetType()))
                        return ((IAck)Value).Display();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                //State = -1;
                return null;
            }
            return null;
        }
        
        #endregion
    }


#if (false)
    /// <summary>
    /// Represent a ack stream for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class TransStream : ITransformResponse, IDisposable
    {

    #region Stream / properties

        private TransType _TransType;
        //public TransType TransType { get { return _TransType; } }
        private int _State;
        //public int State { get { return _State; } }
        //object _Value;


        NetStream _Stream;

        NetStream Stream
        {
            get
            {
                if (_Stream == null)
                {
                    _Stream = new NetStream();
                }
                return _Stream;
            }
        }

        public NetStream GetStream()
        {
            return _Stream;
        }

        public int GetLength()
        {
            return _Stream == null ? 0 : _Stream.iLength;
        }

        public TransType PeekTransType()
        {
            var stream = GetStream();
            if (stream == null || stream.Length < HeaderBytes)
                return TransType.None;

            if (stream.PeekInt32(1) == Signature)
                return (TransType)stream.PeekByte(6);

            //if (stream.PeekByte(0) == (byte)2)
            //{
            //    return (TransType)stream.PeekByte(1);
            //}
            return TransType.None;
        }
        public int PeekState()
        {
            var stream = GetStream();
            if (stream == null || stream.Length < HeaderBytes)
                return -1;
            if (stream.PeekInt32(1) == Signature)
                return (int)stream.PeekInt32(8);
            else
                return -1;
            //if (stream.PeekInt32(0) == (byte)2)
            //{
            //    return (int)stream.PeekInt32(2);
            //}
            //return 0;
        }

        public static bool IsTransStream(Type type)
        {
            return type == typeof(TransStream);
        }

        public bool IsTransStream()
        {
            return IsTransStream(_Stream);
        }

        public static bool IsTransStream(NetStream stream)
        {
            if (stream == null || stream.Length < HeaderBytes)
                return false;
            return (int)stream.PeekInt32(1) == Signature;
        }

        public bool IsEmpty
        {
            get { return _Stream == null || _Stream.Length == 0; }
        }

        //public NetStream GetJsonStream()
        //{
        //    var stream = GetStream();
        //    if (stream == null)
        //        return null;
        //    if(PeekTransType(stream)== TransType.Json)
        //    {

        //        ReadJson();
        //    }
        //}

    #endregion

    #region ITransformResponse

        public void SetState(int state, string message)
        {
            WriteTrans(message, TransType.State, state);
        }

        public byte[] GetBytes()
        {
            return Stream.ToArray();
        }

    #endregion

    #region static read 
        public static object ReadValue(NetStream stream)
        {
            using (IBinaryStreamer streamer = new BinaryStreamer(stream))
            {
                if (IsTransStream(stream))
                {
                    streamer.ReadValue<int>();//signature
                    streamer.ReadValue<byte>();//TransType
                    streamer.ReadValue<int>();//State
                }
                var value = streamer.ReadValue();
                return value;
            }
        }
        public static T ReadValue<T>(NetStream stream)
        {
            object val = ReadValue(stream);
            return GenericTypes.Cast<T>(val, true);
        }
        public static string ReadJson(NetStream stream)
        {
            var o = ReadValue(stream);
            return (o == null) ? null : JsonSerializer.Serialize(o);
            //return TransStream.ReadJson(stream, (message) => { throw new Exception(message); });
        }
    #endregion

    #region static TransType

        public static bool IsEmptyStream(TransStream ts)
        {
            return (ts == null || ts.IsEmpty);
        }

        public static TransType PeekTransType(NetStream stream)
        {
            if (stream == null)
                return TransType.None;
            if (stream.PeekByte(0) == (byte)2)
            {
                return (TransType)stream.PeekByte(1);
            }
            return TransType.None;
        }
        public static TransType ToTransType(TransformType type)
        {
            return (TransType)(int)type;
            //switch (type)
            //{
            //    case TransformType.Stream:
            //        return TransType.Stream;
            //    case TransformType.Json:
            //        return TransType.Json;
            //    default:
            //        return TransType.Object;
            //}
        }

        //public static TransType ToTransType(MessageState state)
        //{
        //    switch (state)
        //    {
        //        case MessageState.None:
        //        case MessageState.Ok:
        //            return TransType.Info;
        //        default:
        //            return TransType.Error;
        //    }
        //}

        public static TransType ToTransType(StringFormatType format)
        {
            return (TransType)format;
        }
        public static TransformType ToTransformType(StringFormatType format)
        {
            return (TransformType)format;
        }

        public static StringFormatType ToStringFormatType(string message)
        {
            if (string.IsNullOrEmpty(message))
                return StringFormatType.None;
            if (Strings.IsJsonString(message))
                return StringFormatType.Json;
            if (Strings.IsXmlString(message))
                return StringFormatType.Xml;
            if (Strings.IsBase64String(message))
                return StringFormatType.Base64;
            else
                return StringFormatType.Text;
        }

        public static TransType ToTransType(Type type, StringFormatType format = StringFormatType.Json)
        {
            if (type == typeof(TransStream))
                return TransType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransType.Stream;
            else if (type == typeof(string))
                return ToTransType(format);// TransType.Json;
            else //if (type == typeof(object))
                return TransType.Object;

        }
        public static TransformType ToTransformType(Type type, StringFormatType format = StringFormatType.Json)
        {
            if (type == typeof(TransStream))
                return TransformType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransformType.Stream;
            else if (type == typeof(string))
                return ToTransformType(format);// TransformType.Json;
            else //if (type == typeof(object))
                return TransformType.Object;

        }
    #endregion

    #region Static Write

        public static Task<TransStream> WriteAsync(object value, TransType type)
        {
            return Task.FromResult(new TransStream(value, type));
        }
        public static Task<TransStream> WriteAsync(NetStream stream, TransformType transformType)
        {
            if (stream != null)
                stream.Position = 0;
            return Task.FromResult(new TransStream(stream, transformType));
        }
        public static Task<TransStream> WriteStateAsync(int state, string message)
        {
            return Task.FromResult(new TransStream(message, TransType.State, state));
        }

        public static TransStream Write(object value, TransType type)
        {
            return new TransStream(value, type);
        }

        public static TransStream Write(NetStream stream, TransformType transformType)
        {
            if (stream != null)
                stream.Position = 0;
            return new TransStream(stream, transformType);
        }

        public static TransStream WriteState(int state, string message)
        {
            return new TransStream(message, TransType.State, state);
        }

        public static TransStream WriteAck(int state, string message)
        {
            return new TransStream(new MessageAck((ChannelState)state, message), TransType.Ack);
        }

        public static TransStream WriteBody(IBodyStream bs, string action, TransformType transformType)
        {
            if (bs == null)
            {
                return new TransStream(MessageAck.DoAck(ChannelState.ItemNotFound, action + ", Item Not Found"), TransType.Ack);
                //return new TransStream(action + ", Item Not Found", TransType.Error);
            }
            else
                return new TransStream(bs.GetStream(), transformType);
        }
        public static TransStream Write(object item, string action, TransformType transformType)
        {
            if (item == null)
                return new TransStream(MessageAck.DoAck(ChannelState.ItemNotFound, action + ", Item Not Found"), TransType.Ack);  //return new TransStream(action + ", Item Not Found", TransType.Error);
            else
                return new TransStream(item, ToTransType(transformType));
        }

        public static TransStream CopyFrom(NetworkStream stream, int readTimeout, int bufferSize = 8192)
        {
            TransStream ts = new TransStream();
            ts.Stream.CopyBlock(stream, readTimeout, bufferSize);
            return ts;
        }

        public static TransStream CopyFrom(PipeStream stream, int bufferSize = 8192)
        {
            TransStream ts = new TransStream();
            ts.Stream.CopyBlock(stream, bufferSize);
            return ts;
        }

        public static TransStream CopyFromStream(Stream stream, int bufferSize = 4096)
        {
            byte[] buffer = new byte[bufferSize];

            TransStream ts = new TransStream();
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, buffer.Length);
                    ts.Stream.Write(buffer, 0, count);

                } while (count != 0);
            }
            return ts;
        }
        public static NetStream ToStream(object value)
        {
            NetStream ns = new NetStream();
            IBinaryStreamer streamer = new BinaryStreamer(ns);
            streamer.WriteValue(value);
            streamer.Flush();
            return ns;
        }
    #endregion

    #region ctor
        private TransStream()
        {

        }
        public TransStream(object value, TransType type = TransType.Object)
        {
            WriteTrans(value, type);
        }

        public TransStream(string message, TransType type, int state)
        {
            WriteTrans(message, type, state);
        }

        public TransStream(NetworkStream stream, int readTimeout, int bufferSize, TransformType transform)
        {
            var ns = new NetStream();
            ns.CopyBlock(stream, readTimeout, bufferSize);
            if (IsTransStream(ns))
                _Stream = ns;//  Stream.CopyBlock(stream, readTimeout, bufferSize);
            else
                WriteTrans(ns, ToTransType(transform));
        }
        public TransStream(PipeStream stream, int bufferSize, TransformType transform)
        {
            var ns = new NetStream();
            ns.CopyBlock(stream, bufferSize);
            if (IsTransStream(ns))
                _Stream = ns;// Stream.CopyBlock(stream, bufferSize);
            else
                WriteTrans(ns, ToTransType(transform));
        }

        public TransStream(NetStream stream, TransformType type)
        {
            if (IsTransStream(stream))
                _Stream = stream.Copy();
            else
                WriteTrans(stream, ToTransType(type));
        }

        public TransStream(byte[] data, int offset, int count, TransType type)
        {
            var stream = new NetStream(data, offset, count);
            if (IsTransStream(stream))
                _Stream = stream.Copy();
            else
                WriteTrans(stream, type);
        }

        public TransStream(NetworkStream stream, int readTimeout, int bufferSize, TransformType transform, bool isTransStream)
        {

            //stream contains transType
            if (isTransStream)//transform == TransformType.Stream)
                Stream.CopyBlock(stream, readTimeout, bufferSize);
            else
            {
                var ns = new NetStream();
                ns.CopyBlock(stream, readTimeout, bufferSize);
                WriteTrans(ns, ToTransType(transform));
            }
        }

        public TransStream(PipeStream stream, int bufferSize, TransformType transform, bool isTransStream)
        {
            //stream contains transType
            if (isTransStream)//transform == TransformType.Stream)
                Stream.CopyBlock(stream, bufferSize);
            else
            {
                var ns = new NetStream();
                ns.CopyBlock(stream, bufferSize);
                WriteTrans(ns, ToTransType(transform));
            }
        }

        public TransStream(NetStream stream, TransformType type, bool isTransStream = false)
        {
            if (isTransStream)
                _Stream = stream;
            else
                WriteTrans(stream, ToTransType(type));
        }

        //public TransStream(byte[] data, int offset, int count, TransType type = TransType.Object)
        //{
        //    WriteTrans(new NetStream(data, offset, count), type);
        //}

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
                    if (_Stream != null)
                    {
                        _Stream.Dispose();
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    #endregion

    #region  Write/Read Trans

        const int HeaderBytes = 11;
        const int Signature = -2147483647;// Int32.MaxValue-1 ;
        //const string Sig = "TransStream";
        void WriteTrans(object value, TransType type, int state = 0)
        {
            IBinaryStreamer streamer = new BinaryStreamer(Stream);

            Stream.Clear();
            streamer.WriteValue((int)Signature);
            streamer.WriteValue((byte)type);
            streamer.WriteValue((int)state);
            streamer.WriteValue(value);
            streamer.Flush();
        }

        public async Task<object>  ReadTransAsync()
        {
            await Task.CompletedTask;

            if (IsEmpty)
            {
                _TransType = TransType.None;
                _State = -1;
                Console.WriteLine("ReadTrans IsEmpty.");
                return null;
            }
            using (IBinaryStreamer streamer = new BinaryStreamer(Stream))
            {
                if (IsTransStream(Stream))
                {
                    streamer.ReadValue<int>();//signature
                    _TransType = (TransType)streamer.ReadValue<byte>();//TransType
                    _State = streamer.ReadValue<int>();//State
                }
                var value = streamer.ReadValue();
                return value;
            }
        }

        public T Read<T>(object Value)
        {
            try
            {
                if (Value == null)
                    return default(T);
                if (typeof(T) == typeof(string) && typeof(T) == Value.GetType())
                    return GenericTypes.Cast<T>(Value.ToString());
                if (typeof(T) == typeof(int) && typeof(T) == Value.GetType())
                    return GenericTypes.Cast<T>(Types.ToInt(Value));
                if (typeof(T) == typeof(NetStream) && typeof(T) == Value.GetType())
                    return GenericTypes.Cast<T>(Value);
                if (typeof(T) == typeof(Stream) && typeof(T) == Value.GetType())
                    return GenericTypes.Cast<T>(NetStream.CopyStream((Stream)Value));//GenericTypes.Cast<T>(BinarySerializer.SerializeToStream(Value));
                else
                    return GenericTypes.Cast<T>(Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                _State = -1;
                return default(T);
            }
        }

        object ReadTrans()
        {

            if (IsEmpty)
            {
                _TransType = TransType.None;
                _State = -1;
                Console.WriteLine("ReadTrans IsEmpty.");
                return null;
            }
            using (IBinaryStreamer streamer = new BinaryStreamer(Stream))
            {
                if (IsTransStream(Stream))
                {
                    streamer.ReadValue<int>();//signature
                    _TransType = (TransType)streamer.ReadValue<byte>();//TransType
                    _State = streamer.ReadValue<int>();//State
                }
                var value = streamer.ReadValue();
                return value;
            }
        }
        T ReadTrans<T>()
        {
            object val = ReadTrans();
            return GenericTypes.Cast<T>(val, true);
        }
    #endregion

    #region read

        public object ReadValue(Action<string> onFault)
        {
            try
            {
                return ReadTrans();
            }
            catch (Exception ex)
            {
                _State = -1;
                onFault(ex.Message);
                return null;
            }
        }

        public T ReadValue<T>(Action<string> onFault)
        {
            try
            {
                return ReadTrans<T>();
            }
            catch (Exception ex)
            {
                _State = -1;
                onFault(ex.Message);
                return default(T);
            }
        }

        public T ReadValue<T>()
        {
            try
            {
                return ReadTrans<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _State = -1;
                return default(T);
            }
        }
         

        public int ReadState()
        {
            return PeekState();
        }

        /*
        public TransType TryRead(Action<string> onFault, out int State, out object Value)
        {
            TransType TransType = TransType.None;
            //Value = null;
            //int State = 0;

            try
            {

                if (IsEmpty)
                {
                    Value = null;
                    State = (int)ChannelState.ItemNotFound;
                    return TransType.None;
                }

                using (IBinaryStreamer streamer = new BinaryStreamer(_Stream))
                {
                    int signature = streamer.ReadValue<int>();
                    TransType = (TransType)streamer.ReadValue<byte>();
                    State = streamer.ReadValue<int>();
                    Value = streamer.ReadValue();

                    switch (TransType)
                    {
                        //case TransType.Info:
                        //case TransType.Error:
                        //    State = TransType == TransType.Error ? -1 : 0;
                        //    break;
                        //case TransType.State:
                        //    State = GenericTypes.Convert<int>(Value, -1);
                        //    break;
                        case TransType.None:
                            State = 0;
                            break;
                        case TransType.Ack:
                            if (Value is NetStream)
                                Value = ReadValue((NetStream)Value);
                            //if (Value is TransAck)
                            //    State = ((TransAck)Value).State;
                            else if (Value is MessageAck)
                                State = (int)((MessageAck)Value).State;
                            else
                                State = -1;
                            break;
                        case TransType.Stream:
                            State = Value == null ? -1 : 0;
                            break;
                        case TransType.Text:
                            if (Value is NetStream)
                                Value = ReadValue((NetStream)Value);
                            if (!(Value is String))
                            {
                                Value = JsonSerializer.Serialize(Value);
                            }
                            State = (Value is String) ? 0 : -1;
                            break;
                        case TransType.Base64:
                            if (Value is NetStream)
                                Value = ReadValue((NetStream)Value);
                            State = (Value is String) ? 0 : -1;
                            break;
                        case TransType.Json:
                            if (Value is NetStream)
                                Value = ReadValue((NetStream)Value);
                            if (!(Value is String))
                            {
                                Value = JsonSerializer.Serialize(Value);
                            }
                            State = Value == null ? -1 : 0;
                            break;
                        case TransType.Object:
                            if (Value is NetStream)
                                Value = ReadValue((NetStream)Value);
                            State = Value == null ? -1 : 0;
                            break;
                        default:
                            State = -1;
                            break;
                    }

                    if (Value == null || State < 0)
                    {
                        TransType = TransType.None;
                        if (onFault != null)
                        {
                            onFault("TransReader value is null for trans type " + TransType.ToString());
                        }
                        Console.WriteLine("TransReader value is null for trans type " + TransType.ToString());
                    }
                    else if (State < 0)
                    {
                        TransType = TransType.None;
                        if (onFault != null)
                        {
                            onFault("TransReader value is incorrect for trans type " + TransType.ToString());
                        }
                        Console.WriteLine("TransReader value is incorrect for trans type " + TransType.ToString());
                    }
                    return TransType;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                Value = null;
                State = -1;
                return TransType.None;
            }

        }
                
        public object ReadValue(Action<string> onFault)
        {
            object Value;
            int state = 0;
            var transType = TryRead(onFault, out state, out Value);
            if (transType == TransType.None)
                return null;
            return Value;
        }

         public T ReadValue<T>()
        {
            return ReadValue<T>((message) => { throw new Exception(message); });
        }

        /// <summary>
        /// ReadValue, Exception if failed
        /// </summary>
        /// <returns></returns>
        public object ReadValue()
        {
            return ReadValue((message) => { throw new Exception(message); });
        }

        public T ReadValue<T>(Action<string> onFault)
        {
            object Value;
            int state = 0;
            var transType = TryRead(onFault, out state, out Value);
            if (transType == TransType.None)
                return default(T);
            if (Value == null)
                return default(T);
            if (Value is NetStream)
            {
                if (typeof(T) == typeof(NetStream))
                    return GenericTypes.Cast<T>(Value, onFault);
                else
                    return TransStream.ReadValue<T>((NetStream)Value);
            }
            //T val;
            //if (GenericTypes.TryConvert<T>(Value, out val))
            //    return val;

            return GenericTypes.Cast<T>(Value, onFault);
        }
        */
        /*
        public NetStream ReadStream()
        {
            return _Stream;
        }

        public string ReadJson(Action<string> onFault)
        {
            object Value;
            int state = 0;
            var transType = TryRead(onFault, out state, out Value);
            if (transType == TransType.None)
                return null;

            switch (transType)
            {
                case TransType.Stream:
                    {
                        var o = ReadValue((NetStream)Value);
                        return JsonSerializer.Serialize(o);
                    }
                case TransType.Text:
                case TransType.Json:
                    {
                        if (Value is string)
                        {
                            return (string)Value;
                        }
                        else
                        {
                            return JsonSerializer.Serialize(Value);
                        }
                    }
                case TransType.Object:
                    return JsonSerializer.Serialize(Value);
                default:
                    if (onFault != null)
                        onFault("No valid json");
                    return null;
            }
        }
        /// <summary>
        /// ReadJson, Exception if failed
        /// </summary>
        /// <returns></returns>
        public string ReadJson()
        {
            return ReadJson((message) => { throw new Exception(message); });
        }

        public string ReadString(Action<string> onFault)
        {
            object Value;
            int state = 0;
            var transType = TryRead(onFault, out state, out Value);
            if (transType == TransType.None)
                return null;

            switch (transType)
            {
                case TransType.Text:
                    if (Value is string)
                    {
                        return (string)Value;
                    }
                    return Value.ToString();
                case TransType.Json:
                    if (Value is string)
                    {
                        return (string)Value;
                    }
                    else
                    {
                        return JsonSerializer.Serialize(Value);
                    }
                case TransType.Stream:
                    if (Value is string)
                        return Value.ToString();
                    if (Value is NetStream)
                    {
                        byte[] b = ((NetStream)Value).ToArray();
                        return Convert.ToBase64String(b, 0, b.Length);
                    }
                    return BinarySerializer.SerializeToBase64(Value);
                case TransType.Object:
                    if (SerializeTools.IsPrimitiveOrString(Value.GetType()))
                        return Value.ToString();
                    if (Value is NetStream)
                    {
                        var o = ReadValue((NetStream)Value);
                        if (o != null && SerializeTools.IsPrimitiveOrString(o.GetType()))
                            return o.ToString();
                    }
                    return BinarySerializer.SerializeToBase64(Value);
                default:
                    return Value.ToString();
            }
        }
        /// <summary>
        /// ReadString, Exception if failed
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return ReadString((message) => { throw new Exception(message); });
        }
        */
    #endregion

    #region ReadTo

        public object ReadValue()
        {
            try
            {
                return ReadTrans();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                _State = -1;
                return null;
            }
        }

        public T ReadTo<T>() where T : class
        {
            try
            {
                var o = ReadTrans();
                if (o == null)
                    return default(T);
                if (typeof(T) == o.GetType())
                    return Nistec.GenericTypes.Convert<T>(o);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
            }
            return default(T);
        }

        public NetStream ReadToStream()
        {
            try
            {
                object Value = ReadTrans();
                if (Value != null)
                {
                    if (Value is string)
                        return NetStream.WriteTo(Value.ToString());
                    if (Value is NetStream)
                        return (NetStream)Value;
                    else if (Value is Stream)
                        return NetStream.CopyStream((Stream)Value);
                    else
                        return BinarySerializer.SerializeToStream(Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                _State = -1;
                return null;
            }
            return null;
        }

        public string ReadToText()
        {
            try
            {
                object Value = ReadTrans();
                if (Value != null)
                {
                    if (Value is string)
                        return Value.ToString();
                    if (Value is NetStream)
                        return NetStream.ReadTo((NetStream)Value);
                    if (typeof(IAck).IsAssignableFrom(Value.GetType()))
                        return ((IAck)Value).Display();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                _State = -1;
                return null;
            }
            return null;
        }
        public string ReadToBase64String()
        {
            try
            {
                object Value = ReadTrans();
                if (Value != null)
                {
                    if (Value is NetStream)
                    {
                        byte[] b = ((NetStream)Value).ToArray();
                        return Convert.ToBase64String(b, 0, b.Length);
                    }
                    return BinarySerializer.SerializeToBase64(Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                _State = -1;
                return null;
            }
            return null;
        }
        public string ReadToJson()
        {
            try
            {
                object Value = ReadTrans();
                if (Value != null)
                {
                    if (typeof(IAck).IsAssignableFrom(Value.GetType()))
                        return ((IAck)Value).ToJson();

                    return JsonSerializer.Serialize(Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadTo error: " + ex.Message);
                _State = -1;
                return null;
            }
            return null;
        }
    #endregion

    #region static reader

        /*
        public static TransType TryRead(NetStream stream, Action<string> onFault, out int State, out object Value)
        {
            TransType TransType = TransType.None;
            //Value = null;
            //int State = 0;

            try
            {

                if (stream == null || stream.Length < HeaderBytes)
                {
                    Value = null;
                    State = (int)ChannelState.ItemNotFound;
                    return TransType.None;
                }

                using (IBinaryStreamer streamer = new BinaryStreamer(stream))
                {
                    int signature = streamer.ReadValue<int>();
                    TransType = (TransType)streamer.ReadValue<byte>();
                    State = streamer.ReadValue<int>();
                    Value = streamer.ReadValue();

                    switch (TransType)
                    {
                        //case TransType.Info:
                        //case TransType.Error:
                        //    State = TransType == TransType.Error ? -1 : 0;
                        //    break;
                        //case TransType.State:
                        //    State = GenericTypes.Convert<int>(Value, -1);
                        //    break;
                        case TransType.None:
                            State = 0;
                            break;
                        case TransType.Ack:
                            if (Value is NetStream)
                                Value = ((BinaryStreamer)streamer).StreamToValue((NetStream)Value);
                            //if (Value is TransAck)
                            //    State = ((TransAck)Value).State;
                            else if (Value is MessageAck)
                                State = (int)((MessageAck)Value).State;
                            else
                                State = -1;
                            break;
                        case TransType.Stream:
                            State = Value == null ? -1 : 0;
                            break;
                        case TransType.Text:
                            if (Value is NetStream)
                                Value = ((BinaryStreamer)streamer).StreamToValue((NetStream)Value);
                            if (!(Value is String))
                            {
                                Value = JsonSerializer.Serialize(Value);
                            }
                            State = (Value is String) ? 0 : -1;
                            break;
                        case TransType.Base64:
                            if (Value is NetStream)
                                Value = ((BinaryStreamer)streamer).StreamToValue((NetStream)Value);
                            State = (Value is String) ? 0 : -1;
                            break;
                        case TransType.Json:
                            if (Value is NetStream)
                                Value = ((BinaryStreamer)streamer).StreamToValue((NetStream)Value);
                            if (!(Value is String))
                            {
                                Value = JsonSerializer.Serialize(Value);
                            }
                            State = Value == null ? -1 : 0;
                            break;
                        case TransType.Object:
                            if (Value is NetStream)
                                Value = ((BinaryStreamer)streamer).StreamToValue((NetStream)Value);
                            State = Value == null ? -1 : 0;
                            break;
                        default:
                            State = -1;
                            break;
                    }

                    if (Value == null || State < 0)
                    {
                        TransType = TransType.None;
                        if (onFault != null)
                        {
                            onFault("TransReader value is null for trans type " + TransType.ToString());
                        }
                        Console.WriteLine("TransReader value is null for trans type " + TransType.ToString());
                    }
                    else if (State < 0)
                    {
                        TransType = TransType.None;
                        if (onFault != null)
                        {
                            onFault("TransReader value is incorrect for trans type " + TransType.ToString());
                        }
                        Console.WriteLine("TransReader value is incorrect for trans type " + TransType.ToString());
                    }
                    return TransType;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                Value = null;
                State = -1;
                return TransType.None;
            }

        }

        public static object ReadValue(NetStream stream, Action<string> onFault)
        {
            object Value = null;
            int State = 0;

            TransType transType = TryRead(stream, onFault, out State, out Value);
            if (transType == TransType.None)
                return null;
            return Value;
        }
        public static object ReadValue(NetStream stream)
        {
            object Value = null;
            int State = 0;

            TransType transType = TryRead(stream, (message) => {
                throw new Exception(message);
            }, out State, out Value);

            return Value;
        }
        public static T ReadValue<T>(NetStream stream, Action<string> onFault)
        {
            return GenericTypes.Cast<T>(ReadValue(stream, onFault), onFault);
        }
        public static T ReadValue<T>(NetStream stream)
        {
            return GenericTypes.Cast<T>(ReadValue(stream, (message) => {
                throw new Exception(message);
            }), true);
        }

        public static T ReadValue<T>(NetStream stream, T defaultValue)
        {
            T val = defaultValue;
            val = TransStream.ReadValue<T>(stream, (message) => { val = defaultValue; });
            return val;
        }
       
        public static string ReadJson(NetStream stream, Action<string> onFault)
        {
            object Value;
            int state = 0;
            var transType = TransStream.TryRead(stream, onFault, out state, out Value);
            if (transType == TransType.None)
                return null;

            switch (transType)
            {
                case TransType.Stream:
                    {
                        var o = StreamToValue((NetStream)Value);
                        return JsonSerializer.Serialize(o);
                    }
                case TransType.Json:
                    {
                        if (Value is string)
                        {
                            return (string)Value;
                        }
                        else
                        {
                            return JsonSerializer.Serialize(Value);
                        }
                    }
                case TransType.Object:
                    return JsonSerializer.Serialize(Value);
                default:
                    if (onFault != null)
                        onFault("No valid json");
                    return null;
            }
        }
        public static string ReadJson(NetStream stream)
        {
            return TransStream.ReadJson(stream, (message) => { throw new Exception(message); });
        }
         */

    #endregion

    #region old reader

        /*
        public ITransResult Read(Action<string> onFault)
        {
            var reader = new TransReader(this.Stream, onFault);
            return reader;
        }

        public NetStream ReadStream(Action<string> onFault)
        {
            return TransReader.ReadStream(this.Stream, onFault);
        }

        public object ReadValue(Action<string> onFault)
        {
            return TransReader.ReadValue(this.Stream, onFault);
        }

        public object ReadValue()
        {
            TransReader reader = new TransReader(this.Stream);
            if (!reader.IsValue)
            {
                throw new Exception(reader.Message);
            }
            return reader.GetValue();
        }

        public object ReadValue(Type type)
        {
            TransReader reader = new TransReader(this.Stream);
            if (!reader.IsValue)
            {
                throw new Exception(reader.Message);
            }
            return reader.GetValue(type);
        }
        public T ReadValue<T>(Action<string> onFault)
        {
            return TransReader.ReadValue<T>(this.Stream, onFault);
        }
        public T ReadValue<T>()
        {
            TransReader reader = new TransReader(this.Stream);
            if (!reader.IsValue)
            {
                throw new Exception(reader.Message);
            }
            return reader.GetValue<T>();
        }
        public string ReadJson()
        {
            TransReader reader = new TransReader(this.Stream);
            if (!reader.IsValue)
            {
                throw new Exception(reader.Message);
            }
            return reader.GetJson();
        }
        public string ReadString()
        {
            TransReader reader = new TransReader(this.Stream);
            if (!reader.IsValue)
            {
                throw new Exception(reader.Message);
            }
            return reader.GetString();
        }
        public int ReadState()
        {
            TransReader reader = new TransReader(this.Stream);
            return reader.State;
        }
        public string ReadMessage()
        {
            TransReader reader = new TransReader(this.Stream);
            return reader.Message;
        }
        */
    #endregion

        TransBinary ToTransBinary()
        {
            return new TransBinary(Stream, "", this._TransType);
        }
    }

#endif
}
