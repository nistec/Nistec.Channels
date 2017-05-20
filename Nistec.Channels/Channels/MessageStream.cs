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
using System.Collections;
using System.Threading.Tasks;
using Nistec.Generic;
using Nistec.Runtime;
using Nistec.IO;
using System.Net.Sockets;
using System.IO.Pipes;
using Nistec.Serialization;

namespace Nistec.Channels
{
    /// <summary>
    /// Represent a message stream for network communication like namedPipe or Tcp.
    /// This message can serialize/desrialize fast and easly using the <see cref="BinaryStreamer"/>
    /// </summary>
    [Serializable]
    public abstract class MessageStream : ISerialEntity, IMessageStream, ISerialJson, IDisposable
    {
        #region properties
        /// <summary>
        /// Get the default formatter.
        /// </summary>
        public static Formatters DefaultFormatter { get { return Formatters.BinarySerializer; } }

        /// <summary>
        /// Get or Set The message key.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Get or Set The message body stream.
        /// </summary>
        public NetStream BodyStream { get; set; }
        /// <summary>
        ///  Get or Set The type name of body stream.
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// Get or Set The serializer formatter.
        /// </summary>
        public Formatters Formatter { get; set; }
        /// <summary>
        /// Get or Set The message key.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Get or Set The message command.
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// Get or Set indicate wether the message is a duplex type.
        /// </summary>
        public bool IsDuplex { get; set; }
        /// <summary>
        ///  Get or Set The message expiration.
        /// </summary>
        public int Expiration { get; set; }
        /// <summary>
        /// Get or Set The last time that message was modified.
        /// </summary>
        public DateTime Modified { get; set; }
        /// <summary>
        /// Get or Set The extra arguments for current message.
        /// </summary>
        public GenericNameValue Args { get; set; }
        #endregion

        #region ctor
        /// <summary>
        /// Initialize a new instance of MessageStream
        /// </summary>
        public MessageStream()
        {
            Modified = DateTime.Now;
        }
        /// <summary>
        /// Initialize a new instance of MessageStream from stream using for <see cref="ISerialEntity"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        protected MessageStream(Stream stream, IBinaryStreamer streamer)
        {
            EntityRead(stream, streamer);
        }
        /// <summary>
        /// Initialize a new instance of MessageStream from <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="info"></param>
        protected MessageStream(SerializeInfo info)
        {
            Key = info.GetValue<string>("Key");
            BodyStream = (NetStream)info.GetValue("BodyStream");
            TypeName = info.GetValue<string>("TypeName");
            Formatter = (Formatters)info.GetValue<int>("Formatter");
            Id = info.GetValue<string>("Id");
            Command = info.GetValue<string>("Command");
            IsDuplex = info.GetValue<bool>("IsDuplex");
            Expiration = info.GetValue<int>("Expiration");
            Modified = info.GetValue<DateTime>("Modified");
            Args = (GenericNameValue)info.GetValue("Args");
        }
        #endregion

        #region Dispose

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        bool disposed = false;
        /// <summary>
        /// Get indicate wether the current instance is Disposed.
        /// </summary>
        protected bool IsDisposed
        {
            get { return disposed; }
        }
        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Command = null;
                Args = null;
                Key = null;
                TypeName = null;
                Id = null;
                if (BodyStream != null)
                {
                    BodyStream.Dispose();
                    BodyStream = null;
                }
            }
            disposed = true;
        }
        #endregion

        #region methods

        int GetSize()
        {
            if (BodyStream == null)
                return 0;// GetInternalSize();
            return BodyStream.iLength;
        }

        /// <summary>
        /// Get Body Size in bytes
        /// </summary>
        public int Size
        {
            get { return GetSize(); }
        }


        /// <summary>
        /// Get indicate wether the item is empty 
        /// </summary>
        public virtual bool IsEmpty
        {
            get
            {
                return BodyStream == null || BodyStream.Length == 0;
            }
        }

        /// <summary>
        /// Get Type of body
        /// </summary>
        public Type BodyType
        {
            get
            {
                return  SerializeTools.GetQualifiedType(TypeName);
            }
        }
        /// <summary>
        /// Get indicate wether the current body type is a known object type.
        /// </summary>
        public bool IsKnownType
        {
            get
            {
                return !string.IsNullOrEmpty(TypeName) && BodyType != null && !typeof(object).Equals(BodyType);
            }
        }

        #endregion

        #region Args
        /// <summary>
        /// Create arguments helper.
        /// </summary>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        public static GenericNameValue CreateArgs(params string[] keyValues)
        {
            if (keyValues == null)
                return null;
            GenericNameValue args = new GenericNameValue(keyValues);
            return args;
        }
        /// <summary>
        /// Get or create a collection of arguments.
        /// </summary>
        /// <returns></returns>
        public GenericNameValue GetArgs()
        {
            if (Args == null)
                return new GenericNameValue();
            return Args;
        }
        #endregion

        #region Convert
        /// <summary>
        /// Convert body to base 64 string.
        /// </summary>
        /// <returns></returns>
        public string ConvertToBase64()
        {
            if (BodyStream == null)
                return null;
            return BinarySerializer.SerializeToBase64(this.BodyStream.ToArray());
        }
        /// <summary>
        /// Convert from base 64 string to generic object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static T ConvertFromBase64<T>(string base64)
        {
            return BinarySerializer.DeserializeFromBase64<T>(base64);
        }

        #endregion

        #region  ISerialEntity

 
        /// <summary>
        /// Write the current object include the body and properties to stream using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteString(Key);
            streamer.WriteValue(BodyStream);
            streamer.WriteString(TypeName);
            streamer.WriteValue((int)Formatter);
            streamer.WriteString(Id);
            streamer.WriteString(Command);
            streamer.WriteValue(IsDuplex);
            streamer.WriteValue(Expiration);
            streamer.WriteValue(Modified);
            streamer.WriteValue(Args);
            streamer.Flush();
        }

        
        /// <summary>
        /// Read stream to the current object include the body and properties using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            Key = streamer.ReadString();
            BodyStream = (NetStream)streamer.ReadValue();
            TypeName = streamer.ReadString();
            Formatter = (Formatters)streamer.ReadValue<int>();
            Id = streamer.ReadString();
            Command = streamer.ReadString();
            IsDuplex = streamer.ReadValue<bool>();
            Expiration = streamer.ReadValue<int>();
            Modified = streamer.ReadValue<DateTime>();
            Args = (GenericNameValue)streamer.ReadValue();
        }
        /// <summary>
        /// Write the current object include the body and properties to <see cref="ISerializerContext"/> using <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        public void WriteContext(ISerializerContext context)
        {
            SerializeInfo info = new SerializeInfo();
            info.Add("Key", Key);
            info.Add("BodyStream", BodyStream);
            info.Add("TypeName", TypeName);
            info.Add("Formatter", (int)Formatter);
            info.Add("Id", Id);
            info.Add("Command", Command);
            info.Add("IsDuplex", IsDuplex);
            info.Add("Expiration", Expiration);
            info.Add("Modified", Modified);
            info.Add("Args",Args);
            context.WriteSerializeInfo(info);
        }

        
        /// <summary>
        /// Read <see cref="ISerializerContext"/> context to the current object include the body and properties using <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        public void ReadContext(ISerializerContext context)
        {
            SerializeInfo info = context.ReadSerializeInfo();

            Key = info.GetValue<string>("Key");
            BodyStream = (NetStream)info.GetValue("BodyStream");
            TypeName = info.GetValue<string>("TypeName");
            Formatter = (Formatters)info.GetValue<int>("Formatter");
            Id = info.GetValue<string>("Id");
            Command = info.GetValue<string>("Command");
            IsDuplex = info.GetValue<bool>("IsDuplex");
            Expiration = info.GetValue<int>("Expiration");
            Modified = info.GetValue<DateTime>("Modified");
            Args = (GenericNameValue)info.GetValue("Args");
        }


        #endregion

        #region IMessageStream

        /// <summary>
        /// Get body stream after set the position to first byte in buffer, This method is a part of <see cref="IMessageStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public NetStream GetBodyStream()
        {
            if (BodyStream == null)
                return null;
            if (BodyStream.Position > 0)
                BodyStream.Position = 0;
            return BodyStream;
        }
        /// <summary>
        /// Set the given value to body stream using <see cref="BinarySerializer"/>, This method is a part of <see cref="IMessageStream"/> implementation..
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetBody(object value)
        {

            if (value != null)
            {
                TypeName = value.GetType().FullName;

                NetStream ns = new NetStream();
                var ser = new BinarySerializer();
                ser.Serialize(ns, value);
                ns.Position = 0;
                BodyStream = ns;
            }
            else
            {
                TypeName = typeof(object).FullName;
                BodyStream = null;
            }
        }
        /// <summary>
        /// Set the given byte array to body stream using <see cref="NetStream"/>, This method is a part of <see cref="IMessageStream"/> implementation
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public virtual void SetBody(byte[] value, Type type)
        {
            TypeName = (type != null) ? type.FullName : typeof(object).FullName;
            if (value != null)
            {
                BodyStream = new NetStream(value);
            }
        }
        /// <summary>
        /// Deserialize body stream to object, This method is a part of <see cref="IMessageStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public virtual object DecodeBody()
        {
            if (BodyStream == null)
                return null;
            BodyStream.Position = 0;
            var ser = new BinarySerializer();
            return ser.Deserialize(BodyStream);
        }
        /// <summary>
        ///  Deserialize body stream to generic object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T DecodeBody<T>()
        {
            return GenericTypes.Cast<T>(DecodeBody());
        }
        /// <summary>
        /// Read stream to object.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static object ReadBodyStream(Type type, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("ReadBodyStream.stream");
            }
            if (type == null)
            {
                throw new ArgumentNullException("ReadBodyStream.type");
            }

            BinarySerializer reader = new BinarySerializer();
            return reader.Deserialize(stream);
        }
        /// <summary>
        /// Write object to stream
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="stream"></param>
        public static void WriteBodyStream(object entity, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("WriteBodyStream.stream");
            }
            if (entity == null)
            {
                throw new ArgumentNullException("WriteBodyStream.entity");
            }

            BinarySerializer writer = new BinarySerializer();
            writer.Serialize(stream,entity);
            //writer.Flush();
        }

        #endregion

        #region IBodyFormatter extend
 
        /// <summary>
        /// Set the given byte array to body stream.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="typeName"></param>
        public void SetBody(byte[] value, string typeName)
        {
            TypeName = (!string.IsNullOrEmpty(typeName)) ? typeName : typeof(object).FullName;
            if (value != null)
            {
                BodyStream = new NetStream(value);
            }
        }
        /// <summary>
        /// Set the given stream to body stream.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="typeName"></param>
        /// <param name="copy"></param>
        public void SetBody(NetStream ns, string typeName, bool copy = true)
        {
            TypeName = (!string.IsNullOrEmpty(typeName)) ? typeName : typeof(object).FullName;
            if (ns != null)
            {
                if (copy)
                    ns.CopyTo(BodyStream);
                else
                    BodyStream = ns;
            }
        }

        #endregion

        #region ExecuteTask
        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public AckStream AsyncTask(Func<MessageStream> action, string actionName)
        {
            using (Task<IMessageStream> task = Task.Factory.StartNew<IMessageStream>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return AckStream.GetAckStream(false, actionName);//null;
                    return new AckStream(task.Result.BodyStream,task.Result.TypeName);
                }
            }
            return new AckStream(MessageState.ItemNotFound,"ItemNotFound");//null;
        }
        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public AckStream AsyncTask(Func<ISerialEntity> action, string actionName)//,Formatters formatter)
        {
            using (Task<ISerialEntity> task = Task.Factory.StartNew<ISerialEntity>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return AckStream.GetAckStream(false,actionName);//null;

                    return new AckStream(task.Result);
                }
            }
            return new AckStream(MessageState.ItemNotFound, "ItemNotFound");// null;
        }

        /// <summary>
        /// Execute async one way task request.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        public void AsyncTask(Func<ISerialEntity> action, Stream stream)
        {
            using (Task<ISerialEntity> task = Task.Factory.StartNew<ISerialEntity>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return;
                    task.Result.EntityWrite(stream,null);//..GetEntityStream();
                }
            }
            //return null;
        }

        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public AckStream AsyncTask(Func<object> action, string actionName)
        {
            using (Task<object> task = Task.Factory.StartNew<object>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return AckStream.GetAckStream(false, actionName);//null;
                    return new AckStream(task.Result);
                }
            }
            return AckStream.GetAckStream(false, actionName);// null;
        }

        
        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="typeName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public AckStream AsyncTask(Func<NetStream> action, string typeName, string actionName)
        {
            using (Task<NetStream> task = Task.Factory.StartNew<NetStream>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return AckStream.GetAckStream(false, actionName);//null;
                    return new AckStream(task.Result,typeName);
                }
            }
            return AckStream.GetAckStream(false, actionName);// null;
        }

        /// <summary>
        /// Execute async task request and return the response as<see cref="AckStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public AckStream AsyncAckTask(Func<AckStream> action, string actionName)
        {
            using (Task<AckStream> task = Task.Factory.StartNew<AckStream>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return AckStream.GetAckStream(false, actionName);//null;
                    return task.Result;
                }
            }
            return AckStream.GetAckStream(false, actionName);// null;
        }

        public void AsyncTask(Action action)
        {
            using (Task task = Task.Factory.StartNew(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                }
            }
        }

        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public AckStream AsyncBinaryTask(Func<byte[]> action, string actionName)
        {
            using (Task<byte[]> task = Task.Factory.StartNew<byte[]>(action))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result == null)
                        return AckStream.GetAckStream(false, actionName);//null;
                    return new AckStream(task.Result);
                }
            }
            return AckStream.GetAckStream(false, actionName);//null;
        }
        #endregion
/*

        #region ReadAck tcp

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="InBufferSize"></param>
        public object ReadAck(NetworkStream stream, int readTimeout, int InBufferSize)
        {
            using (AckStream ack = AckStream.Read(stream,typeof(object), readTimeout, InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new MessageException(ack);
                }
                return ack.Value;
            }
        }

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="type"></param>
        /// <param name="readTimeout"></param>
        /// <param name="InBufferSize"></param>
        /// <returns></returns>
        public object ReadAck(NetworkStream stream, Type type, int readTimeout, int InBufferSize)
        {

            using (AckStream ack = AckStream.Read(stream,type, readTimeout, InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new MessageException(ack);
                }
                return ack.Value;
            }
        }

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="InBufferSize"></param>
        /// <returns></returns>
        public TResponse ReadAck<TResponse>(NetworkStream stream, int readTimeout, int InBufferSize)
        {

            using (AckStream ack = AckStream.Read(stream, typeof(TResponse), readTimeout, InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new MessageException(ack);
                }
                return ack.GetValue<TResponse>();
            }
        }

        #endregion

        #region ReadAck pipe

        public object ReadAck(NamedPipeClientStream stream, Type type, int InBufferSize = 8192)
        {

            using (AckStream ack = AckStream.Read(stream, type, InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new Exception(ack.Message);
                }
                return ack.Value;
            }
        }

        public TResponse ReadAck<TResponse>(NamedPipeClientStream stream, int InBufferSize = 8192)
        {

            using (AckStream ack = AckStream.Read(stream, typeof(TResponse), InBufferSize))
            {
                if (ack.State > MessageState.Ok)
                {
                    throw new Exception(ack.Message);
                }
                return ack.GetValue<TResponse>();
            }
        }

 
        #endregion
*/
 
        #region extension

        internal static string[] SplitArg(IKeyValue dic, string key, string valueIfNull)
        {
            string val = dic.Get<string>(key, valueIfNull);
            if (val == null)
                return valueIfNull == null ? null : new string[] { valueIfNull };
            return val.SplitTrim('|');
        }

        internal static TimeSpan TimeArg(IKeyValue dic, string key, string valueIfNull)
        {
            string val = dic.Get<string>(key, valueIfNull);
            TimeSpan time = string.IsNullOrEmpty(val) ? TimeSpan.Zero : TimeSpan.Parse(val);
            return time;
        }

        internal static string[] SplitArg(IDictionary dic, string key, string valueIfNull)
        {
            string val = dic.Get<string>(key, valueIfNull);
            if (val == null)
                return valueIfNull == null ? null : new string[] { valueIfNull };
            return val.SplitTrim('|');
        }

        internal static TimeSpan TimeArg(IDictionary dic, string key, string valueIfNull)
        {
            string val = dic.Get<string>(key, valueIfNull);
            TimeSpan time = string.IsNullOrEmpty(val) ? TimeSpan.Zero : TimeSpan.Parse(val);
            return time;
        }
        /// <summary>
        /// Convert <see cref="MessageStream"/> to <see cref="IDictionary"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IDictionary ConvertTo(MessageStream message)
        {
            IDictionary dict = new Dictionary<string, object>();
            dict.Add("Command", message.Command);
            dict.Add("Key", message.Key);
            dict.Add("Id", message.Id);

            dict.Add("Expiration", message.Expiration);
            dict.Add("Modified", message.Modified);
            dict.Add("TypeName", message.TypeName);


            if (message.IsDuplex)
                dict.Add("IsDuplex", message.IsDuplex);

            if (message.Args != null)
                dict.Add("Args", message.Args);
            if (message.BodyStream != null)
                dict.Add("Body", message.BodyStream);

            return dict;
        }

        #endregion

        #region ISerialJson

        public string EntityWrite(IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Write, null);
            return serializer.Write(this, this.GetType().BaseType);
        }

        public object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, null);

            object o = serializer.Read<MessageStream>(json);

            if (o != null)
            {
                Copy((MessageStream)o);
            }
            return o;
        }

        #endregion

        void Copy(MessageStream copy)
        {
            Key = copy.Key;
            BodyStream = copy.BodyStream;
            TypeName = copy.TypeName;
            Formatter = copy.Formatter;
            Id = copy.Id;
            Command = copy.Command;
            IsDuplex = copy.IsDuplex;
            Expiration = copy.Expiration;
            Modified = copy.Modified;
            Args = copy.Args;
        }
 
    }

 }
