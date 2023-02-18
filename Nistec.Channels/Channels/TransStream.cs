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

    //public enum TransType : byte { None = 0, Object = 100, Stream = 101, Json = 102, State = 121, Info = 122, Error = 123 }

    public enum TransType : byte { None = 0, Object = 100, Stream = 101, Json = 102, Base64 = 103, Text = 104, Ack = 105, State = 106, Csv = 107, Xml=108 }
    public enum StringFormatType : byte { None = 0, Json = 102, Base64 = 103, Text = 104, Csv = 107, Xml = 108 }

    /// <summary>
    /// Represent a ack stream for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class TransStream : ITransformResponse// IDisposable
    {

        #region Stream

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

        #region internal 
        internal static object StreamToValue(NetStream stream)
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
        internal static T StreamToValue<T>(NetStream stream)
        {
            object val = StreamToValue(stream);
            return GenericTypes.Cast<T>(val, true);
        }
        #endregion

        #region static

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

        public static TransType ToTransType(Type type, StringFormatType format= StringFormatType.Json)
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
            return new TransStream(new TransAck(state, message), TransType.Ack);
        }

        public static TransStream WriteBody(IBodyStream bs, string action, TransformType transformType)
        {
            if (bs == null)
            {
                return new TransStream(TransAck.DoAck((int)MessageState.ItemNotFound, action + ", Item Not Found"), TransType.Ack);
                //return new TransStream(action + ", Item Not Found", TransType.Error);
            }
            else
                return new TransStream(bs.GetStream(), transformType);
        }
        public static TransStream Write(object item, string action, TransformType transformType)
        {
            if (item == null)
                return new TransStream(TransAck.DoAck((int)MessageState.ItemNotFound, action + ", Item Not Found"), TransType.Ack);  //return new TransStream(action + ", Item Not Found", TransType.Error);
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

        #region  WriteTrans
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


        #endregion

        #region read

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
                    State = (int)MessageState.ItemNotFound;
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
                                Value = StreamToValue((NetStream)Value);
                            if (Value is TransAck)
                                State = ((TransAck)Value).State;
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
                                Value = StreamToValue((NetStream)Value);
                            if (!(Value is String))
                            {
                                Value = JsonSerializer.Serialize(Value);
                            }
                            State = (Value is String) ? 0 : -1;
                            break;
                        case TransType.Base64:
                            if (Value is NetStream)
                                Value = StreamToValue((NetStream)Value);
                            State = (Value is String) ? 0 : -1;
                            break;
                        case TransType.Json:
                            if (Value is NetStream)
                                Value = StreamToValue((NetStream)Value);
                            if (!(Value is String))
                            {
                                Value = JsonSerializer.Serialize(Value);
                            }
                            State = Value == null ? -1 : 0;
                            break;
                        case TransType.Object:
                            if (Value is NetStream)
                                Value = StreamToValue((NetStream)Value);
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
                    return TransStream.StreamToValue<T>((NetStream)Value);
            }
            //T val;
            //if (GenericTypes.TryConvert<T>(Value, out val))
            //    return val;

            return GenericTypes.Cast<T>(Value, onFault);
        }
        /// <summary>
        /// ReadValue, Exception if failed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadValue<T>()
        {
            return ReadValue<T>((message) => { throw new Exception(message); });
        }

        public int ReadState()
        {
            return PeekState();
        }
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
                        var o = StreamToValue((NetStream)Value);
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
                        var o = StreamToValue((NetStream)Value);
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


        #endregion

        #region static reader

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
                    State = (int)MessageState.ItemNotFound;
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
                            if (Value is TransAck)
                                State = ((TransAck)Value).State;
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


        #endregion

        #region reader

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

    }

    [Serializable]
    public class TransAck: ISerialEntity, IDisposable
    {
        public static TransAck DoAck(int state, string message) { return new TransAck(state, message); }

        #region properties
        public int State { get; set; }

        public string Message { get; set; }
        #endregion

        #region ctor

        public TransAck()
        {
        }

        public TransAck(int state, string message)
        {
            State = state;
            Message = message;
        }

        public TransAck(NetStream stream)
        {
            EntityRead(stream, null);
        }

        protected TransAck(Stream stream, IBinaryStreamer streamer)
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
            streamer.Flush();
        }

        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            State = streamer.ReadValue<int>();
            Message = streamer.ReadString();
        }

        #endregion

    }

    public static class TransStreamExtension
    {
        public static TransType ToTransType(this TransformType type)
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

    }



    #region interface
    /*
    public interface ITransResult
    {
        TransType TransType { get; }
        object Value { get; }
        string Message { get; }
        int State { get; }

        NetStream GetStream();
        T GetValue<T>();
        T GetValue<T>(T defaultValue);
        object GetValue(Type type);
        object GetValue();
        string GetJson();
        bool IsValue { get; }
        bool IsMessage { get; }
    }
    
       

*/
    #endregion

#if (false)
    /// <summary>
    /// TransPack
    /// </summary>
    [Serializable]
    public class TransPack : ISerialEntity
    {
        public static string ToJson(object Value, int State)
        {
            return new TransPack() { State = State, Value = Value }.ToJson();
        }
        public static string ToAck(MessageState state)
        {
            return string.Format("State:{0}, Value:{1}", (int)state, state.ToString());
        }

        public static TransPack Get(object Value, int State, TransformType transformType = TransformType.Object)
        {
            return new TransPack() { State = State, Value = Value, TransformType = transformType };
        }

        //public TransType TransType { get; set; }
        public TransformType TransformType { get; set; }

        public int State { get; set; }

        public object Value { get; set; }
        //public string Reason { get; set; }

        public T GetValue<T>()
        {
            return TransReader.GetValue<T>(Value);
        }

        public string ToJson()
        {

            return JsonSerializer.Serialize(this);
        }

    #region  ISerialEntity

        /// <summary>
        /// Write the current object include the body and properties to stream using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public virtual void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteValue((byte)TransformType);
            streamer.WriteValue((int)State);
            streamer.WriteValue(Value);
            streamer.Flush();
        }


        /// <summary>
        /// Read stream to the current object include the body and properties using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public virtual void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            TransformType = (TransformType)streamer.ReadValue<byte>();
            State = streamer.ReadValue<int>();
            Value = streamer.ReadValue();
        }

    #endregion
    }

    public class TransReader : ITransResult
    {
    #region helper
        internal static object StreamToValue(NetStream stream)
        {
            using (IBinaryStreamer streamer = new BinaryStreamer(stream))
            {
                var value = streamer.ReadValue();
                return value;
            }
        }
        internal static T StreamToValue<T>(NetStream stream)
        {
            object val = StreamToValue(stream);
            return GenericTypes.Cast<T>(val, true);
        }
    #endregion

    #region static reader
        public static TransReader Get(NetStream stream)
        {
            return new TransReader(stream);
        }

        public static NetStream ReadStream(NetStream stream)
        {
            return ReadStream(stream, null);
        }

        public static NetStream ReadStream(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream, onFault);
                {
                    if (reader.IsValue)
                        return reader.GetStream();
                    else if (onFault != null)
                        onFault(reader.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return null;
            }
        }
        public static object ReadValue(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream);
                {
                    if (reader.IsValue)
                        return reader.GetValue();
                    else if (onFault != null)
                        onFault(reader.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return null;
            }
        }

        public static object ReadValue(NetStream stream)
        {
            if (stream == null)
                return null;
            var reader = new TransReader(stream);
            {
                return reader.GetValue();
            }
        }

        public static T ReadValue<T>(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream, onFault);
                {
                    if (reader.IsValue)
                        return reader.GetValue<T>();
                    else if (onFault != null)
                        onFault(reader.Message);
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return default(T);
            }
        }
        public static T ReadValue<T>(NetStream stream, T defaultValue)
        {
            try
            {
                var reader = new TransReader(stream);
                {
                    if (reader.IsValue)
                        return reader.GetValue<T>(defaultValue);
                    else
                        return defaultValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return defaultValue;
            }
        }
        public static T ReadValue<T>(NetStream stream)
        {
            if (stream == null)
                return default(T);

            var reader = new TransReader(stream);
            {
                return reader.GetValue<T>();
            }
        }

        public static string ReadString(NetStream stream)
        {
            if (stream == null)
                return null;

            var reader = new TransReader(stream);
            {
                return reader.GetString();
            }
        }

        public static string ReadJson(NetStream stream)
        {
            if (stream == null)
                return null;

            var reader = new TransReader(stream);
            {
                return reader.GetJson();
            }
        }
        public static string ReadJson(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream);
                {
                    if (reader.IsValue)
                        return reader.GetJson();
                    else if (onFault != null)
                    {
                        onFault(reader.Message);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return null;
            }
        }

        public static int ReadState(NetStream stream)
        {
            if (stream == null)
                return -1;

            var reader = new TransReader(stream);
            {
                return reader.State;
            }
        }
        public static bool IsTransStream(Type type)
        {
            return type == typeof(TransStream);
        }
        public static TransType ToTransType(Type type)
        {
            if (type == typeof(TransStream))
                return TransType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransType.Stream;
            else if (type == typeof(string))
                return TransType.Json;
            else //if (type == typeof(object))
                return TransType.Object;

        }
        public static TransformType ToTransformType(Type type)
        {
            if (type == typeof(TransStream))
                return TransformType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransformType.Stream;
            else if (type == typeof(string))
                return TransformType.Json;
            else //if (type == typeof(object))
                return TransformType.Object;

        }
    #endregion

    #region properties
        public TransType TransType { get; private set; }
        public object Value { get; private set; }
        public int State { get; private set; }

    #endregion

    #region public Methods

        public NetStream GetStream()
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return (NetStream)Value;
                    return BinarySerializer.SerializeToStream(Value);
                case TransType.Json:
                    return BinarySerializer.SerializeToStream(Value);
                case TransType.Object:
                    if (Value is NetStream)
                        return (NetStream)Value;
                    return BinarySerializer.SerializeToStream(Value);
                default:
                    return null;
            }
        }

        public T GetValue<T>()
        {
            if (Value == null)
                return default(T);
            switch (TransType)
            {
                case TransType.Json:
                    return JsonSerializer.Deserialize<T>(Value.ToString());
                case TransType.Stream:
                case TransType.Object:
                    if (Value is NetStream)
                    {
                        if (typeof(T) == typeof(NetStream))
                            return GenericTypes.Cast<T>(Value, true);
                        else
                            return TransReader.StreamToValue<T>((NetStream)Value);
                    }
                    return GenericTypes.Cast<T>(Value, true);
                default:
                    return default(T);
            }
        }

        public T GetValue<T>(T defaultValue)
        {
            if (Value == null)
                return defaultValue;
            switch (TransType)
            {
                case TransType.Json:
                    return JsonSerializer.Deserialize<T>(Value.ToString());
                case TransType.Stream:
                case TransType.Object:

                    if (Value is NetStream)
                    {
                        if (typeof(T) == typeof(NetStream))
                            return GenericTypes.Cast<T>(Value);
                        else
                            return TransReader.StreamToValue<T>((NetStream)Value);
                    }
                    return GenericTypes.Cast<T>(Value, true);
                default:
                    return defaultValue;
            }
        }

        public object GetValue(Type type)
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return TransReader.StreamToValue((NetStream)Value);
                    return Value;
                case TransType.Json:
                    if (Value == null)
                        return null;
                    return JsonSerializer.Deserialize(Value.ToString(), type);
                case TransType.Object:
                    if (Value is NetStream)
                    {
                        if (type == typeof(NetStream))
                            return Value;
                        else
                            return TransReader.StreamToValue((NetStream)Value);
                    }
                    return Value;
                default:
                    return null;
            }
        }

        public object GetValue()
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return TransReader.StreamToValue((NetStream)Value);
                    return Value;
                case TransType.Json:
                    if (Value == null)
                        return null;
                    var type = Value.GetType();
                    return JsonSerializer.Deserialize(Value.ToString(), type);
                case TransType.Object:
                    return Value;
                default:
                    return null;
            }
        }

        public string GetString()
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Json:
                    return GetJson();
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
                        var o = TransReader.StreamToValue((NetStream)Value);
                        if (o != null && SerializeTools.IsPrimitiveOrString(o.GetType()))
                            return o.ToString();
                    }
                    return BinarySerializer.SerializeToBase64(Value);
                default:
                    return Value.ToString();
            }
        }

        public string GetJson()
        {
            if (Value == null)
                return null;

            switch (TransType)
            {
                case TransType.Stream:
                    {
                        var o = TransReader.StreamToValue((NetStream)Value);
                        return JsonSerializer.Serialize(o);
                    }
                case TransType.Json:
                    {
                        if (Value is NetStream)
                        {
                            var o = TransReader.StreamToValue((NetStream)Value);
                            return JsonSerializer.Serialize(o);
                        }
                        if (Value is string)
                        {
                            return (string)Value;
                        }
                        else
                        {
                            return ToJson();
                        }
                    }
                case TransType.Object:
                    return ToJson();
                default:
                    return null;
            }
        }

        public bool IsValue
        {
            get
            {
                switch (TransType)
                {
                    case TransType.Stream:
                    case TransType.Json:
                    case TransType.Object:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsMessage
        {
            get
            {
                switch (TransType)
                {
                    case TransType.State:
                    case TransType.Error:
                    case TransType.Info:
                        return true;
                    default:
                        return false;
                }
            }
        }
        public string Message
        {
            get
            {
                switch (TransType)
                {
                    case TransType.Info:
                    case TransType.Error:
                        return Value == null ? "Value is null!" : Value.ToString();
                    case TransType.State:
                        return Value == null ? "State: " + State.ToString() : "State: " + Value.ToString();
                    case TransType.None:
                        return TransType.ToString();
                    case TransType.Stream:
                    case TransType.Json:
                    case TransType.Object:
                        return Value == null ? "Value is null!" : TransType.ToString() + " is " + Value.GetType().Name;
                    default:
                        return "TransType not supported!" + TransType.ToString();
                }
            }
        }

        public string ToJson()
        {
            if (Value == null)
                return null;
            return JsonSerializer.Serialize(Value);
        }


    #endregion

    #region ctor

        public TransReader(NetStream stream)
        {
            Read(stream, null);
        }
        public TransReader(NetStream stream, Action<string> onFault)
        {
            Read(stream, onFault);
        }

        void Read(NetStream stream, Action<string> onFault)
        {
            try
            {
                using (IBinaryStreamer streamer = new BinaryStreamer(stream))
                {
                    TransType = (TransType)streamer.ReadValue<byte>();
                    Value = streamer.ReadValue();

                    switch (TransType)
                    {
                        case TransType.Info:
                        case TransType.Error:
                            State = TransType == TransType.Error ? -1 : 0;
                            break;
                        case TransType.State:
                            State = GenericTypes.Convert<int>(Value, -1);
                            break;
                        case TransType.None:
                            State = 0;
                            break;
                        case TransType.Stream:
                            State = Value == null ? -1 : 0;
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
                    if (Value == null)
                    {
                        Console.WriteLine("TransReader value is null for trans type " + TransType.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
            }

        }


    #endregion

    #region static internal reader
        internal static NetStream GetStreamValue(object Value)
        {
            if (Value == null)
                return null;
            if (Value is NetStream)
                return (NetStream)Value;
            else
                return BinarySerializer.SerializeToStream(Value);
        }

        internal static object GetValue(object Value)
        {
            if (Value == null)
                return null;
            if (Value is NetStream)
                return TransReader.StreamToValue((NetStream)Value);
            else
                return Value;
        }

        internal static T GetValue<T>(object Value)
        {
            if (Value == null)
                return default(T);
            //if (typeof(T) == typeof(NetStream))
            //    return GenericTypes.Cast<T>(Value, true);
            else if (Value is NetStream)
                if (typeof(T) == typeof(NetStream))
                    return GenericTypes.Cast<T>(Value, true);
                else
                    return TransReader.StreamToValue<T>((NetStream)Value);
            else if (Value is T)
                return GenericTypes.Cast<T>(Value, true);
            else
                return GenericTypes.Convert<T>(Value);
        }
        internal static string GetString(object Value)
        {
            if (Value == null)
                return null;
            if (Value is string)
                return Value.ToString();
            if (SerializeTools.IsPrimitiveOrString(Value.GetType()))
                return Value.ToString();
            if (Value is NetStream)
            {
                var o = TransReader.StreamToValue((NetStream)Value);
                if (o != null && SerializeTools.IsPrimitiveOrString(o.GetType()))
                    return o.ToString();
            }
            return BinarySerializer.SerializeToBase64(Value);

        }
        internal static string GetJson(object Value)
        {
            if (Value == null)
                return null;
            if (Value is string)
                return Value.ToString();
            if (Value is NetStream)
            {
                var o = TransReader.StreamToValue((NetStream)Value);
                if (o != null)
                    return JsonSerializer.Serialize(o);
            }
            return ToJsonValue(Value);
        }
        internal static string GetReasone(object Value, int State)
        {
            return Value == null ? "State: " + State.ToString() : "State: " + Value.ToString();
        }
    #endregion

    #region static internal Methods

        internal static NetStream GetStreamValue(object Value, TransType TransType)
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return (NetStream)Value;
                    return BinarySerializer.SerializeToStream(Value);
                case TransType.Json:
                    return BinarySerializer.SerializeToStream(Value);
                case TransType.Object:
                    if (Value is NetStream)
                        return (NetStream)Value;
                    return BinarySerializer.SerializeToStream(Value);
                default:
                    return null;
            }
        }

        internal static T GetValue<T>(object Value, TransType TransType)
        {
            if (Value == null)
                return default(T);
            switch (TransType)
            {
                case TransType.Json:
                    return JsonSerializer.Deserialize<T>(Value.ToString());
                case TransType.Stream:
                case TransType.Object:
                    if (Value is NetStream)
                    {
                        if (typeof(T) == typeof(NetStream))
                            return GenericTypes.Cast<T>(Value, true);
                        else
                            return TransReader.StreamToValue<T>((NetStream)Value);
                    }
                    return GenericTypes.Cast<T>(Value, true);
                default:
                    return default(T);
            }
        }

        internal static T GetValue<T>(object Value, TransType TransType, T defaultValue)
        {
            if (Value == null)
                return defaultValue;
            switch (TransType)
            {
                case TransType.Json:
                    return JsonSerializer.Deserialize<T>(Value.ToString());
                case TransType.Stream:
                case TransType.Object:

                    if (Value is NetStream)
                    {
                        if (typeof(T) == typeof(NetStream))
                            return GenericTypes.Cast<T>(Value);
                        else
                            return TransReader.StreamToValue<T>((NetStream)Value);
                    }
                    return GenericTypes.Cast<T>(Value, true);
                default:
                    return defaultValue;
            }
        }

        internal static object GetValue(object Value, TransType TransType, Type type)
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return TransReader.StreamToValue((NetStream)Value);
                    return Value;
                case TransType.Json:
                    if (Value == null)
                        return null;
                    return JsonSerializer.Deserialize(Value.ToString(), type);
                case TransType.Object:
                    if (Value is NetStream)
                    {
                        if (type == typeof(NetStream))
                            return Value;
                        else
                            return TransReader.StreamToValue((NetStream)Value);
                    }
                    return Value;
                default:
                    return null;
            }
        }

        internal static object GetValue(object Value, TransType TransType)
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return TransReader.StreamToValue((NetStream)Value);
                    return Value;
                case TransType.Json:
                    if (Value == null)
                        return null;
                    var type = Value.GetType();
                    return JsonSerializer.Deserialize(Value.ToString(), type);
                case TransType.Object:
                    return Value;
                default:
                    return null;
            }
        }

        internal static string GetString(object Value, TransType TransType)
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Json:
                    return GetJson(Value, TransType);
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
                        var o = TransReader.StreamToValue((NetStream)Value);
                        if (o != null && SerializeTools.IsPrimitiveOrString(o.GetType()))
                            return o.ToString();
                    }
                    return BinarySerializer.SerializeToBase64(Value);
                default:
                    return Value.ToString();
            }
        }
        internal static string GetJson(object Value, TransType TransType)
        {
            if (Value == null)
                return null;

            switch (TransType)
            {
                case TransType.Stream:
                    {
                        var o = TransReader.StreamToValue((NetStream)Value);
                        return JsonSerializer.Serialize(o);
                    }
                case TransType.Json:
                    {
                        if (Value is NetStream)
                        {
                            var o = TransReader.StreamToValue((NetStream)Value);
                            return JsonSerializer.Serialize(o);
                        }
                        if (Value is string)
                        {
                            return (string)Value;
                        }
                        else
                        {
                            return ToJsonValue(Value);
                        }
                    }
                case TransType.Object:
                    return ToJsonValue(Value);
                default:
                    return null;
            }
        }

        internal static bool IsTransValue(TransType TransType)
        {

            switch (TransType)
            {
                case TransType.Stream:
                case TransType.Json:
                case TransType.Object:
                    return true;
                default:
                    return false;
            }

        }

        internal static bool IsTransMessage(TransType TransType)
        {

            switch (TransType)
            {
                case TransType.State:
                case TransType.Error:
                case TransType.Info:
                    return true;
                default:
                    return false;
            }

        }

        internal static string GetReasone(object Value, TransType TransType, int State)
        {

            switch (TransType)
            {
                case TransType.Info:
                case TransType.Error:
                    return Value == null ? "Value is null!" : Value.ToString();
                case TransType.State:
                    return Value == null ? "State: " + State.ToString() : "State: " + Value.ToString();
                case TransType.None:
                    return TransType.ToString();
                case TransType.Stream:
                case TransType.Json:
                case TransType.Object:
                    return Value == null ? "Value is null!" : TransType.ToString() + " is " + Value.GetType().Name;
                default:
                    return "TransType not supported!" + TransType.ToString();
            }

        }

        internal static string ToJsonValue(object Value)
        {
            if (Value == null)
                return null;
            return JsonSerializer.Serialize(Value);
        }


    #endregion
    }

    /// <summary>
    /// Represent a ack stream for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class TransStream : IDisposable
    {

        //TransPack Pack;

    #region Stream

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
            if (stream == null)
                return TransType.None;

            if (stream.PeekByte(0) == (byte)2)
            {
                return (TransType)stream.PeekByte(1);
            }
            return TransType.None;
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

    #region static

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
            switch (type)
            {
                case TransformType.Stream:
                    return TransType.Stream;
                case TransformType.Json:
                    return TransType.Json;
                default:
                    return TransType.Object;
            }
        }

        public static TransType ToTransType(MessageState state)
        {
            switch (state)
            {
                case MessageState.None:
                case MessageState.Ok:
                    return TransType.Info;
                default:
                    return TransType.Error;
            }
        }

        public static TransStream Write(object value)
        {
            return new TransStream(value);
        }
        public static TransStream Write(object Value, int State = 0, TransformType transformType = TransformType.Object)
        {
            return new TransStream(Value, State, transformType);
        }
        public static TransStream Write(int State, string reason)
        {
            return new TransStream(State, reason);
        }
        //NISSIM-EDIT
        /*
        public static TransStream Write(byte[] data, int offset, int count, bool isPack = false)
        {
            return new TransStream(data, offset, count, isPack);
        }
        */
        public static TransStream Write(byte[] data, int offset, int count)//, bool isPack = false)
        {
            return new TransStream(data, offset, count);
        }

        //public static TransStream Write(object value, TransType type)
        //{
        //    return new TransStream(value, type);
        //}

        public static TransStream Write(NetStream stream, TransformType transformType)
        {
            if (stream != null)
                stream.Position = 0;
            return new TransStream(stream);//, false);// transformType);
        }
        /*
        public static TransStream WriteBody(IBodyStream bs, string action, TransformType transformType)
        {
            if (bs == null)
            {
                return new TransStream(action + ", Item Not Found", TransType.Error);
            }
            else
                return new TransStream(bs.GetStream(), transformType);
        }
        public static TransStream Write(object item, string action, TransformType transformType)
        {
            if (item == null)
                return new TransStream(action + ", Item Not Found", TransType.Error);
            else
                return new TransStream(item, ToTransType(transformType));
        }
        */
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
        public TransStream()
        {
            //Pack = new TransPack();
        }

        public TransStream(object value)
        {
            
            if (value is TransPack)
                WriteTrans(value);
            else
            {
                var pack = new TransPack()
                {
                    State = 0,
                    Value = value,
                    TransformType =  TransformType.Object
                };
                WriteTrans(pack);
            }

            //NISSIM-EDIT
            //WriteTrans(value, type);
        }
        public TransStream(int State, string reason)
        {
            var pack = new TransPack()
            {
                State = State,
                Value = reason,
                TransformType = TransformType.Ack,
                //Reason = reason
            };
            WriteTrans(pack);
        }
        public TransStream(object value, int State = 0, TransformType transformType = TransformType.Object)
        {
            var pack = new TransPack()
            {
                State = State,
                Value = value,
                TransformType = transformType
            };
            WriteTrans(pack);
        }
        //NISSIM-EDIT
        public TransStream(NetworkStream stream, int readTimeout, int bufferSize)
        {
            var ns = new NetStream();
            ns.CopyBlock(stream, readTimeout, bufferSize);
            WriteTransPack(ns, 0, TransformType.Stream);
        }
        public TransStream(PipeStream stream, int bufferSize)
        {
            var ns = new NetStream();
            ns.CopyBlock(stream, bufferSize);
            WriteTransPack(ns, 0, TransformType.Stream);
        }
        public TransStream(NetStream stream)
        {
            WriteTransPack(stream, 0, TransformType.Stream);
        }

        //NISSIM-EDIT
        /*
        public TransStream(NetworkStream stream, int readTimeout, int bufferSize, bool isPack = false)
        {
            TransformType transform = TransformType.Object;
            if (isPack)
            {
                Stream.CopyBlock(stream, readTimeout, bufferSize);
            }
            else
            {
                var ns = new NetStream();
                ns.CopyBlock(stream, readTimeout, bufferSize);
                WriteStreamInternal(ns, 0, transform);
            }
            //stream contains transType
            //if (isTransStream)//transform == TransformType.Stream)
            //Stream.CopyBlock(stream, readTimeout, bufferSize);
            //else
            //{
            //    var ns = new NetStream();
            //    ns.CopyBlock(stream, readTimeout, bufferSize);
            //    WriteTrans(ns);//, ToTransType(transform));
            //}
        }
        
        public TransStream(PipeStream stream, int bufferSize, bool isPack = false)
        {
            TransformType transform = TransformType.Object;
            if (isPack)
            {
                Stream.CopyBlock(stream, bufferSize);
            }
            else
            {
                var ns = new NetStream();
                ns.CopyBlock(stream, bufferSize);
                WriteStreamInternal(ns, 0, transform);
            }

            //stream contains transType
            //if (isTransStream)//transform == TransformType.Stream)
            //    Stream.CopyBlock(stream, bufferSize);
            //else
            //{
            //    var ns = new NetStream();
            //    ns.CopyBlock(stream, bufferSize);
            //    WriteTrans(ns);//, ToTransType(transform));
            //}
        }

        public TransStream(NetStream stream, bool isPack = false)
        {
            TransformType transform = TransformType.Object;
            if (isPack)
            {
                _Stream = stream;
            }
            else
            {
                WriteStreamInternal(stream, 0, transform);
            }

            //if (isTransStream)
            //    _Stream = stream;
            //else
            //    WriteTrans(stream);//, ToTransType(type));
        }
        */
        //NISSIM-EDIT
        /*
        public TransStream(byte[] data, int offset, int count, bool isPack = false)
        {
            if (isPack)
            {
                WriteTrans(new NetStream(data, offset, count));
            }
            else
            {
                WriteStreamInternal(new NetStream(data, offset, count));
            }
        }
        */
        public TransStream(byte[] data, int offset, int count)
        {
            WriteTrans(new NetStream(data, offset, count));

        }

        //NISSIM-EDIT
        /*
        private void WriteStreamInternal(NetStream ns, int State = 0, TransformType transformType = TransformType.Object, string reason = null)
        {
            var pack = new TransPack()
            {
                Value = ns,
                State = State,
                TransformType = transformType,
                Reason = reason
            };
            WriteTrans(pack);
        }
        */

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

    #region  WriteTrans

        void WriteTrans(object value)//, TransType type)
        {
            IBinaryStreamer streamer = new BinaryStreamer(Stream);

            Stream.Clear();
            //streamer.WriteValue((byte)type);
            streamer.WriteValue(value);
            streamer.Flush();
        }
        void WriteTransPack(NetStream ns, int State = 0, TransformType transformType = TransformType.Object)
        {
            var pack = new TransPack()
            {
                Value = ns,
                State = State,
                TransformType = transformType
                //Reason = reason
            };
            WriteTrans(pack);
        }

        //NISSIM-EDIT
        //void WriteTrans(object value, TransStreamType type)
        //{
        //    IBinaryStreamer streamer = new BinaryStreamer(Stream);

        //    Stream.Clear();
        //    streamer.WriteValue((byte)type);
        //    streamer.WriteValue(value);
        //    streamer.Flush();
        //}
        //NISSIM-EDIT
        /*
        TransStreamType TryRead(Action<string> onFault, out object Value)
        {
            TransStreamType type;
            try
            {
                using (IBinaryStreamer streamer = new BinaryStreamer(this.Stream))
                {

                    type = (TransStreamType)streamer.ReadValue<byte>();
                    Value = streamer.ReadValue();
                    //if (Value is NetStream)
                    //{
                    //    Value = ((BinaryStreamer)streamer).StreamToValue((NetStream)Value);
                    //}
                    if (Value == null)
                    {
                        Value = new TransPack()
                        {
                            Value = null,
                            State = -1,
                            Reason = "TransReader value is null."
                        };
                        Console.WriteLine("TransReader value is null.");
                    }

                    if (Value is TransPack)
                    {
                        //Value = ((TransPack)Value).Value;
                        return TransStreamType.Pack;
                    }
                    //else
                    //    Pack = new TransPack()
                    //    {
                    //        Value = Value
                    //    };
                    return type;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }

                Value = new TransPack()
                {
                    Value = null,
                    State = -1,
                    Reason = "TransReader value is null." + ex.Message
                };
                return TransStreamType.Pack;

            }
        }
        */
    #endregion

    #region Read

        bool TryRead(bool isValue, Action<string> onFault, out TransPack Pack)
        {

            try
            {
                using (IBinaryStreamer streamer = new BinaryStreamer(this.Stream))
                {

                    var value = streamer.ReadValue();
                    if (value is NetStream)
                    {
                        value = ((BinaryStreamer)streamer).StreamToValue((NetStream)value);
                    }
                    if (value == null)
                    {
                        Pack = new TransPack()
                        {
                            Value = "TransReader value is null.",
                            State = -1
                           // Reason = "TransReader value is null."
                        };
                        Console.WriteLine("TransReader value is null.");
                    }
                    if (value is TransPack)
                        Pack = (TransPack)value;
                    else
                        Pack = new TransPack()
                        {
                            Value = value
                        };
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
            }

            Pack = null;

            return false;

        }
        public TransPack ReadPack()
        {
            TransPack Pack;
            if (TryRead(true, null, out Pack))
            {
                return Pack;
            }
            return null;
        }
        public string ReadJson()
        {
            TransPack Pack;
            if (TryRead(true, null, out Pack))
            {
                return Pack.ToJson();
            }
            return null;
        }
        public string ReadString()
        {
            TransPack Pack;
            if (TryRead(true, null, out Pack))
            {
                return (Pack.Value==null) ? null: Pack.Value.ToString();
            }
            return null;
        }
        public int ReadState()
        {
            TransPack Pack;
            if (TryRead(true, null, out Pack))
            {
                return Pack.State;
            }
            return -255;
        }

        public object ReadValue(Action<string> onFault)
        {
            //NISSIM-EDIT
            //object val;
            //TransStreamType type = TryRead(onFault, out val);
            
            //return val;


            TransPack Pack;
            if (TryRead(true, onFault, out Pack))
            {
                return Pack.Value;
            }
            return null;
        }
        public object ReadValue()
        {
            TransPack Pack;
            if (TryRead(true, null, out Pack))
            {
                return Pack.Value;
            }
            return null;
        }
        public T ReadValue<T>(Action<string> onFault)
        {
            //NISSIM-EDIT
            //object val;
            //var transType = TryRead(onFault, out val);
            
            //return TransReader.GetValue<T>(val);


            TransPack Pack;
            if (TryRead(true, onFault, out Pack))
            {
                return Pack.GetValue<T>();
            }
            return default(T);
        }
        public T ReadValue<T>()
        {
            TransPack Pack;
            if (TryRead(true, null, out Pack))
            {
                return Pack.GetValue<T>();
            }
            return default(T);
        }
    #endregion

    #region reader
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

    }
#endif

#if (false)

    public class TransReader : ITransResult
    {
    #region helper
        internal static object StreamToValue(NetStream stream)
        {
            using (IBinaryStreamer streamer = new BinaryStreamer(stream))
            {
                var value = streamer.ReadValue();
                return value;
            }
        }
        internal static T StreamToValue<T>(NetStream stream)
        {
            object val = StreamToValue(stream);
            return GenericTypes.Cast<T>(val, true);
        }
    #endregion

    #region static reader
        public static TransReader Get(NetStream stream)
        {
            return new TransReader(stream);
        }

        public static NetStream ReadStream(NetStream stream)
        {
            return ReadStream(stream, null);
        }

        public static NetStream ReadStream(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream, onFault);
                {
                    if (reader.IsValue)
                        return reader.GetStream();
                    else if (onFault != null)
                        onFault(reader.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return null;
            }
        }
        public static object ReadValue(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream);
                {
                    if (reader.IsValue)
                        return reader.GetValue();
                    else if (onFault != null)
                        onFault(reader.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return null;
            }
        }

        public static object ReadValue(NetStream stream)
        {
            if (stream == null)
                return null;
            var reader = new TransReader(stream);
            {
                return reader.GetValue();
            }
        }

        public static T ReadValue<T>(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream, onFault);
                {
                    if (reader.IsValue)
                        return reader.GetValue<T>();
                    else if (onFault != null)
                        onFault(reader.Message);
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return default(T);
            }
        }
        public static T ReadValue<T>(NetStream stream, T defaultValue)
        {
            try
            {
                var reader = new TransReader(stream);
                {
                    if (reader.IsValue)
                        return reader.GetValue<T>(defaultValue);
                    else
                        return defaultValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return defaultValue;
            }
        }
        public static T ReadValue<T>(NetStream stream)
        {
            if (stream == null)
                return default(T);

            var reader = new TransReader(stream);
            {
                return reader.GetValue<T>();
            }
        }

        public static string ReadString(NetStream stream)
        {
            if (stream == null)
                return null;

            var reader = new TransReader(stream);
            {
                return reader.GetString();
            }
        }

        public static string ReadJson(NetStream stream)
        {
            if (stream == null)
                return null;

            var reader = new TransReader(stream);
            {
                return reader.GetJson();
            }
        }
        public static string ReadJson(NetStream stream, Action<string> onFault)
        {
            try
            {
                var reader = new TransReader(stream);
                {
                    if (reader.IsValue)
                        return reader.GetJson();
                    else if (onFault != null)
                    {
                        onFault(reader.Message);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
                return null;
            }
        }

        public static int ReadState(NetStream stream)
        {
            if (stream == null)
                return -1;

            var reader = new TransReader(stream);
            {
                return reader.State;
            }
        }
        public static bool IsTransStream(Type type)
        {
            return type == typeof(TransStream);
        }
        public static TransType ToTransType(Type type)
        {
            if (type == typeof(TransStream))
                return TransType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransType.Stream;
            else if (type == typeof(string))
                return TransType.Json;
            else //if (type == typeof(object))
                return TransType.Object;

        }
        public static TransformType ToTransformType(Type type)
        {
            if (type == typeof(TransStream))
                return TransformType.Stream;
            else if (SerializeTools.IsStream(type))
                return TransformType.Stream;
            else if (type == typeof(string))
                return TransformType.Json;
            else //if (type == typeof(object))
                return TransformType.Object;

        }
    #endregion

    #region properties
        public TransType TransType { get; private set; }
        public object Value { get; private set; }
        public int State { get; private set; }

    #endregion

    #region public Methods

        public NetStream GetStream()
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return (NetStream)Value;
                    return BinarySerializer.SerializeToStream(Value);
                case TransType.Json:
                    return BinarySerializer.SerializeToStream(Value);
                case TransType.Object:
                    if (Value is NetStream)
                        return (NetStream)Value;
                    return BinarySerializer.SerializeToStream(Value);
                default:
                    return null;
            }
        }

        public T GetValue<T>()
        {
            if (Value == null)
                return default(T);
            switch (TransType)
            {
                case TransType.Json:
                    return JsonSerializer.Deserialize<T>(Value.ToString());
                case TransType.Stream:
                case TransType.Object:
                    if (Value is NetStream)
                    {
                        if (typeof(T) == typeof(NetStream))
                            return GenericTypes.Cast<T>(Value, true);
                        else
                            return TransReader.StreamToValue<T>((NetStream)Value);
                    }
                    return GenericTypes.Cast<T>(Value, true);
                default:
                    return default(T);
            }
        }

        public T GetValue<T>(T defaultValue)
        {
            if (Value == null)
                return defaultValue;
            switch (TransType)
            {
                case TransType.Json:
                    return JsonSerializer.Deserialize<T>(Value.ToString());
                case TransType.Stream:
                case TransType.Object:

                    if (Value is NetStream)
                    {
                        if (typeof(T) == typeof(NetStream))
                            return GenericTypes.Cast<T>(Value);
                        else
                            return TransReader.StreamToValue<T>((NetStream)Value);
                    }
                    return GenericTypes.Cast<T>(Value, true);
                default:
                    return defaultValue;
            }
        }

        public object GetValue(Type type)
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return TransReader.StreamToValue((NetStream)Value);
                    return Value;
                case TransType.Json:
                    if (Value == null)
                        return null;
                    return JsonSerializer.Deserialize(Value.ToString(), type);
                case TransType.Object:
                    if (Value is NetStream)
                    {
                        if (type == typeof(NetStream))
                            return Value;
                        else
                            return TransReader.StreamToValue((NetStream)Value);
                    }
                    return Value;
                default:
                    return null;
            }
        }

        public object GetValue()
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Stream:
                    if (Value is NetStream)
                        return TransReader.StreamToValue((NetStream)Value);
                    return Value;
                case TransType.Json:
                    if (Value == null)
                        return null;
                    var type = Value.GetType();
                    return JsonSerializer.Deserialize(Value.ToString(), type);
                case TransType.Object:
                    return Value;
                default:
                    return null;
            }
        }

        public string GetString()
        {
            if (Value == null)
                return null;
            switch (TransType)
            {
                case TransType.Json:
                    return GetJson();
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
                        var o = TransReader.StreamToValue((NetStream)Value);
                        if (o != null && SerializeTools.IsPrimitiveOrString(o.GetType()))
                            return o.ToString();
                    }
                    return BinarySerializer.SerializeToBase64(Value);
                default:
                    return Value.ToString();
            }
        }
        public string GetJson()
        {
            if (Value == null)
                return null;

            switch (TransType)
            {
                case TransType.Stream:
                    {
                        var o = TransReader.StreamToValue((NetStream)Value);
                        return JsonSerializer.Serialize(o);
                    }
                case TransType.Json:
                    {
                        if (Value is NetStream)
                        {
                            var o = TransReader.StreamToValue((NetStream)Value);
                            return JsonSerializer.Serialize(o);
                        }
                        if (Value is string)
                        {
                            return (string)Value;
                        }
                        else
                        {
                            return ToJson();
                        }
                    }
                case TransType.Object:
                    return ToJson();
                default:
                    return null;
            }
        }

        public bool IsValue
        {
            get
            {
                switch (TransType)
                {
                    case TransType.Stream:
                    case TransType.Json:
                    case TransType.Object:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsMessage
        {
            get
            {
                switch (TransType)
                {
                    case TransType.State:
                    case TransType.Error:
                    case TransType.Info:
                        return true;
                    default:
                        return false;
                }
            }
        }
        public string Message
        {
            get
            {
                switch (TransType)
                {
                    case TransType.Info:
                    case TransType.Error:
                        return Value == null ? "Value is null!" : Value.ToString();
                    case TransType.State:
                        return Value == null ? "State: " + State.ToString() : "State: " + Value.ToString();
                    case TransType.None:
                        return TransType.ToString();
                    case TransType.Stream:
                    case TransType.Json:
                    case TransType.Object:
                        return Value == null ? "Value is null!" : TransType.ToString() + " is " + Value.GetType().Name;
                    default:
                        return "TransType not supported!" + TransType.ToString();
                }
            }
        }

        public string ToJson()
        {
            if (Value == null)
                return null;
            return JsonSerializer.Serialize(Value);
        }


    #endregion

    #region ctor

        public TransReader(NetStream stream)
        {
            Read(stream, null);
        }
        public TransReader(NetStream stream, Action<string> onFault)
        {
            Read(stream, onFault);
        }

        void Read(NetStream stream, Action<string> onFault)
        {
            try
            {
                using (IBinaryStreamer streamer = new BinaryStreamer(stream))
                {
                    TransType = (TransType)streamer.ReadValue<byte>();
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
                            if (Value is TransAck)
                                State = ((TransAck)Value).State;
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
                    if (Value == null)
                    {
                        Console.WriteLine("TransReader value is null for trans type " + TransType.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (onFault != null)
                {
                    onFault(ex.Message);
                }
            }

        }


    #endregion
    }
#endif

}
