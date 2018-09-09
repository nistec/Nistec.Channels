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

    #region interface
    public enum TransType : byte { None = 0, Object = 100, Stream = 101, Json = 102, State = 121, Info = 122, Error = 123 }


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
    #endregion

    public static class TransStreamExtension
    {

        public static TransType ToTransType(this TransformType type)
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
            else if(SerializeTools.IsStream(type))
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
                        if (o!=null && SerializeTools.IsPrimitiveOrString(o.GetType()))
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
                        return Value == null ? "Value is null!" : TransType.ToString() + " is "+ Value.GetType().Name;
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
                            State = Value == null ? -1:0;
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

    /// <summary>
    /// Represent a ack stream for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class TransStream : IDisposable
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
            if (stream == null)
                return TransType.None;
          
            if (stream.PeekByte(0) == (byte)2)
            {
                return (TransType)stream.PeekByte(1);
            }
            return TransType.None;
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

        public static TransStream CopyFrom(NetworkStream stream, int readTimeout, int bufferSize=8192)
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

        public TransStream(string message, TransType type)
        {
            WriteTrans(message, type);
        }

        public TransStream(NetworkStream stream, int readTimeout, int bufferSize, TransformType transform , bool isTransStream)
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

        public TransStream(PipeStream stream, int bufferSize, TransformType transform ,bool isTransStream)
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

        public TransStream(byte[] data, int offset, int count, TransType type = TransType.Object)
        {
            WriteTrans(new NetStream(data, offset,count), type);
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


        void WriteTrans(object value, TransType type)
        {
            IBinaryStreamer streamer = new BinaryStreamer(Stream);

            Stream.Clear();
            streamer.WriteValue((byte)type);
            streamer.WriteValue(value);
            streamer.Flush();
        }


        #endregion

        #region reader
      
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
        #endregion

    }

}
