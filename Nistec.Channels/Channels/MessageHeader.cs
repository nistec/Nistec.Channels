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
using System.Diagnostics;
using Nistec.Channels.Tcp;
using Nistec.Channels.Http;
using System.Collections.Specialized;
using System.Net;
#pragma warning disable CS1591
namespace Nistec.Channels
{

    /// <summary>
    /// Represent a message stream for network communication like namedPipe or Tcp.
    /// This message can serialize/desrialize fast and easly using the <see cref="BinaryStreamer"/>
    /// </summary>
    [Serializable]
    public class MessageHeader : ISerialEntity, ISerialJson, INotify, IDisposable, ITransformMessage
    {

        #region properties
        /// <summary>
        /// Get the default formatter.
        /// </summary>
        public static Formatters DefaultFormatter { get { return Formatters.BinarySerializer; } }
        /// <summary>
        /// DefaultEncoding utf-8
        /// </summary>
        public const string DefaultEncoding = "utf-8";

        /// <summary>
        /// Get or Set The message Id.
        /// </summary>
        public string Identifier { get; protected set; }
 
        /// <summary>
        ///  Get or Set The type name of body stream.
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// Get or Set The serializer formatter.
        /// </summary>
        public Formatters Formatter { get; set; }
        /// <summary>
        /// Get or Set The message detail.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Get or Set The message command.
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// Get or Set who send the message.
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Get or Set The last time that message was modified.
        /// </summary>
        public DateTime Creation { get; set; }
        /// <summary>
        /// Get or Set The message CustomId.
        /// </summary>
        public string CustomId { get; set; }
        /// <summary>
        /// Get or Set The message SessionId.
        /// </summary>
        public string SessionId { get; set; }
        ///// <summary>
        ///// Get or set The message encoding, Default=utf-8.
        ///// </summary>
        //public string EncodingName { get; set; }
        /// <summary>
        ///  Get or Set The message expiration int minutes.
        /// </summary>
        public int Expiration { get; set; }
        #endregion
              
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

        #region ctor
        /// <summary>
        /// Initialize a new instance of MessageHeader
        /// </summary>
        public MessageHeader()
        {
            Identifier = UUID.Identifier();
            Creation = DateTime.Now;
            //mqh-EncodingName = "utf-8";
            _Args = new NameValueArgs();
            Formatter = Formatters.BinarySerializer;
        }
        protected MessageHeader(Guid itemId) : this(itemId.ToString())
        {
            //Identifier = itemId.ToString();
            //Modified = DateTime.Now;
            //EncodingName = "utf-8";
        }
        protected MessageHeader(string identifier)
        {
            Identifier = ValidIdentifier(identifier);
            Creation = DateTime.Now;
            //mqh-EncodingName = "utf-8";
            _Args = new NameValueArgs();

        }

        protected MessageHeader(HttpRequestInfo request) : this()
        {
            if (request.BodyStream != null)
            {
                EntityRead(request.BodyStream, null);
            }
            else
            {
                if (request.QueryString != null)//request.BodyType == HttpBodyType.QueryString)
                    EntityRead(request.QueryString, null);
                else if (request.Body != null)
                    EntityRead(request.Body, null);
                //else if (request.Url.LocalPath != null && request.Url.LocalPath.Length > 1)
                //    message.EntityRead(request.Url.LocalPath.TrimStart('/').TrimEnd('/'), null);
            }
        }

        /// <summary>
        /// Initialize a new instance of MessageHeader from stream using for <see cref="ISerialEntity"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        protected MessageHeader(Stream stream, IBinaryStreamer streamer) : this()
        {
            EntityRead(stream, streamer);
        }
        /// <summary>
        /// Initialize a new instance of MessageHeader from <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="info"></param>
        protected MessageHeader(SerializeInfo info) : this()
        {
            Identifier = info.GetValue<string>("Identifier");
            TypeName = info.GetValue<string>("TypeName");
            Formatter = (Formatters)info.GetValue<int>("Formatter");
            Label = info.GetValue<string>("Label");
            CustomId = info.GetValue<string>("CustomId");
            SessionId = info.GetValue<string>("SessionId");
            Command = info.GetValue<string>("Command");
            Source = info.GetValue<string>("Source ");
            DuplexType = (DuplexTypes)info.GetValue<byte>("DuplexType");
            Expiration = info.GetValue<int>("Expiration");
            Creation = info.GetValue<DateTime>("Creation");
            Args = (NameValueArgs)info.GetValue("Args");
            TransformType = (TransformType)info.GetValue<byte>("TransformType");
            //mqh-EncodingName = Types.NZorEmpty(info.GetValue<string>("EncodingName"), DefaultEncoding);
        }

        protected MessageHeader(IDictionary<string, object> dict) : this()
        {
            Identifier = dict.Get<string>("Identifier");
            TypeName = dict.Get<string>("TypeName");
            Formatter = (Formatters)dict.Get<byte>("Formatter");
            Label = dict.Get<string>("Label");
            CustomId = dict.Get<string>("CustomId");
            SessionId = dict.Get<string>("SessionId");
            Command = dict.Get<string>("Command");
            Source = dict.Get<string>("Source");
            DuplexType = (DuplexTypes)dict.Get<byte>("DuplexType", 0);
            Expiration = dict.Get<int>("Expiration", 0);
            Creation = dict.Get<DateTime>("Creation", DateTime.Now);
            Args = dict.Get<NameValueArgs>("Args");
            TransformType = (TransformType)dict.Get<byte>("TransformType");
            //mqh-EncodingName = Types.NZorEmpty(dict.Get<string>("EncodingName"), DefaultEncoding);
        }

        public MessageHeader(MessageHeader copy) : this()
        {
            Copy(copy);
        }

        void Copy(MessageHeader copy)
        {
            Identifier = copy.Identifier;
            TypeName = copy.TypeName;
            Formatter = copy.Formatter;
            Label = copy.Label;
            CustomId = copy.CustomId;
            SessionId = copy.SessionId;
            Command = copy.Command;
            Source = copy.Source;
            DuplexType = copy.DuplexType;
            Expiration = copy.Expiration;
            Creation = copy.Creation;
            Args = copy.Args;
            TransformType = copy.TransformType;
            //mqh-EncodingName = copy.EncodingName;
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
                Identifier = null;
                CustomId = null;
                SessionId = null;
                TypeName = null;
                Label = null;
             }
            disposed = true;
        }
        #endregion

        #region methods

        public static string ValidIdentifier(string identifier)
        {
            if (identifier == null)
                return UUID.Identifier();
            if (identifier.Length < 5 || identifier == Guid.Empty.ToString())
                return UUID.Identifier();
            return identifier;
        }

        public static string GetTypeName(object o, bool fullyQualifiedTypeName = true)
        {
            if (o == null)
                return null;
            if (o is Type)
                return SerializeTools.GetTypeName((Type)o, fullyQualifiedTypeName);

            return SerializeTools.GetTypeName(o.GetType(), fullyQualifiedTypeName);
        }

        /// <summary>
        /// Get Type of body
        /// </summary>
        public Type BodyType
        {
            get
            {
                return SerializeTools.GetQualifiedType(TypeName);
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

        public bool IsValidInfo()
        {
            return !string.IsNullOrEmpty(Identifier) && !string.IsNullOrEmpty(Label);
        }
        public void ValiddateInfo()
        {
            if (string.IsNullOrEmpty(Identifier) || string.IsNullOrEmpty(Label))
            {
                throw new ArgumentException("ComplexKey is null or empty");
            }
        }
        public string KeyInfo()
        {
            return string.Format("{0}{1}{2}", Identifier, KeySet.Separator, Label);
        }
        #endregion

        #region Args

        NameValueArgs _Args;
        /// <summary>
        /// Get or Set The extra arguments for current message.
        /// </summary>
        public NameValueArgs Args
        {
            get { return _Args; }
            set
            {
                if (value == null)
                    _Args.Clear();
                else
                {
                    _Args = value;
                }
            }
        }

        public void Set(string key, string value)
        {
            Args.Set(key, value);
        }
        public string GetArg(string key)
        {
            return Args.Get(key);
        }

        /*

                /// <summary>
                /// Create arguments helper.
                /// </summary>
                /// <param name="keyValues"></param>
                /// <returns></returns>
                public static NameValueArgs CreateArgs(params string[] keyValues)
                {
                    if (keyValues == null)
                        return null;
                    NameValueArgs args = new NameValueArgs(keyValues);
                    return args;
                }
                public NameValueArgs ArgsAdd(params string[] keyValues)
                {
                    if (keyValues == null)
                        return null;
                    int count = keyValues.Length;
                    if (count % 2 != 0)
                    {
                        throw new ArgumentException("values parameter not correct, Not match key value arguments");
                    }

                    if (Args == null)
                        Args= new NameValueArgs();

                    for (int i = 0; i < count; i++)
                    {
                        string key = keyValues[i].ToString();
                        string value = keyValues[++i];

                        if (Args.ContainsKey(key))
                            Args[key] = value;
                        else
                            Args.Add(key, value);
                    }
                    return Args;
                }
                /// <summary>
                /// Get or create a collection of arguments.
                /// </summary>
                /// <returns></returns>
                public NameValueArgs ArgsGet()
                {
                    if (Args == null)
                        return new NameValueArgs();
                    return Args;
                }
                public string ArgsGet(string name)
                {
                    if (Args == null)
                        return null;
                    return Args.Get(name);
                }
                public T ArgsGet<T>(string name)
                {
                    return ArgsGet().Get<T>(name);
                }
                public void ArgsSet(string name, string value)
                {
                    ArgsGet().Add(name, value);
                }
          */
        public void Notify(params string[] args)
        {
            Args.AddArgs(args);// ArgsAdd(args);
        }

        #endregion

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

            streamer.WriteString(Identifier);
            streamer.WriteString(TypeName);
            streamer.WriteValue((int)Formatter);
            streamer.WriteString(Label);
            streamer.WriteString(CustomId);
            streamer.WriteString(SessionId);
            streamer.WriteString(Command);
            streamer.WriteString(Source);
            streamer.WriteValue((byte)DuplexType);

            streamer.WriteValue(Expiration);
            streamer.WriteValue(Creation);
            streamer.WriteValue(Args);
            streamer.WriteValue((byte)TransformType);
            //mqh-streamer.WriteString(EncodingName);
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

            Identifier = streamer.ReadString();
            TypeName = streamer.ReadString();
            Formatter = (Formatters)streamer.ReadValue<int>();
            Label = streamer.ReadString();
            CustomId = streamer.ReadString();
            SessionId = streamer.ReadString();
            Command = streamer.ReadString();
            Source = streamer.ReadString();
            DuplexType = (DuplexTypes)streamer.ReadValue<byte>();
            Expiration = streamer.ReadValue<int>();
            Creation = streamer.ReadValue<DateTime>();
            Args = (NameValueArgs)streamer.ReadValue();
            TransformType = (TransformType)streamer.ReadValue<byte>();
            //mqh-EncodingName = Types.NZorEmpty(streamer.ReadString(), DefaultEncoding);
        }
        
        /// <summary>
        /// Write the current object include the body and properties to <see cref="ISerializerContext"/> using <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        public virtual void WriteContext(ISerializerContext context, SerializeInfo info = null)
        {
            if (info == null)
                info = new SerializeInfo();
            info.Add("Identifier", Identifier);
            info.Add("TypeName", TypeName);
            info.Add("Formatter", (int)Formatter);
            info.Add("Label", Label);
            info.Add("CustomId", CustomId);
            info.Add("SessionId", SessionId);
            info.Add("Command", Command);
            info.Add("Source", Source);
            info.Add("DuplexType", (byte)DuplexType);
            info.Add("Expiration", Expiration);
            info.Add("Creation", Creation);
            info.Add("Args", Args);
            info.Add("TransformType", (byte)TransformType);
            //mqh-info.Add("EncodingName", EncodingName);
            context.WriteSerializeInfo(info);
        }


        /// <summary>
        /// Read <see cref="ISerializerContext"/> context to the current object include the body and properties using <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        public virtual void ReadContext(ISerializerContext context, SerializeInfo info = null)
        {
            if (info == null)
                info = context.ReadSerializeInfo();

            Identifier = info.GetValue<string>("Identifier");
            TypeName = info.GetValue<string>("TypeName");
            Formatter = (Formatters)info.GetValue<int>("Formatter");
            Label = info.GetValue<string>("Label");
            CustomId = info.GetValue<string>("CustomId");
            SessionId = info.GetValue<string>("SessionId");
            Command = info.GetValue<string>("Command");
            Source = info.GetValue<string>("Source");
            DuplexType = (DuplexTypes)info.GetValue<byte>("DuplexType");
            Expiration = info.GetValue<int>("Expiration");
            Creation = info.GetValue<DateTime>("Creation");
            Args = (NameValueArgs)info.GetValue("Args");
            TransformType = (TransformType)info.GetValue<byte>("TransformType");
            //mqh-EncodingName = Types.NZorEmpty(info.GetValue<string>("EncodingName"), DefaultEncoding);
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


            serializer.WriteToken("Identifier", Identifier);
            serializer.WriteToken("TypeName", TypeName);
            serializer.WriteToken("Formatter", Formatter);
            serializer.WriteToken("Label", Label, null);
            serializer.WriteToken("CustomId", CustomId, null);
            serializer.WriteToken("SessionId", SessionId, null);
            serializer.WriteToken("Command", Command);
            serializer.WriteToken("Source", Source);

            serializer.WriteToken("DuplexType", (byte)DuplexType);
            serializer.WriteToken("Expiration", Expiration);
            serializer.WriteToken("Creation", Creation);
            serializer.WriteToken("Args", Args);
            serializer.WriteToken("TransformType", TransformType);
            //mqh-serializer.WriteToken("EncodingName", EncodingName);
            return serializer.WriteOutput(pretty);

        }
        //protected Dictionary<string, object> JsonReader;
        public virtual object EntityRead(Dictionary<string, object> JsonReader, IJsonSerializer serializer)
        {

            if (JsonReader != null)
            {
                Identifier = JsonReader.Get<string>("Identifier");
                TypeName = JsonReader.Get<string>("TypeName");
                Formatter = JsonReader.GetEnum<Formatters>("Formatter", Formatters.BinarySerializer);
                Label = JsonReader.Get<string>("Label");
                CustomId = JsonReader.Get<string>("CustomId");
                SessionId = JsonReader.Get<string>("SessionId");
                Command = JsonReader.Get<string>("Command");
                Source = JsonReader.Get<string>("Source");

                DuplexType = (DuplexTypes)JsonReader.Get<byte>("DuplexType");
                Expiration = JsonReader.Get<int>("Expiration");
                Creation = JsonReader.Get<DateTime>("Creation");
                Args = NameValueArgs.Convert((IDictionary<string, object>)JsonReader.Get("Args"));// dic.Get<NameValueArgs>("Args");
                TransformType = (TransformType)JsonReader.GetEnum<TransformType>("TransformType", TransformType.Object);
                //mqh-EncodingName = Types.NZorEmpty(JsonReader.Get<string>("EncodingName"), DefaultEncoding);

            }
            return this;
        }
        public virtual object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            //var queryParams = new Dictionary<string, string>(HtmlPage.Document.QueryString, StringComparer.InvariantCultureIgnoreCase);

            var JsonReader = serializer.Read<Dictionary<string, object>>(json);

            if (JsonReader != null)
            {
                Identifier = JsonReader.Get<string>("Identifier");
                TypeName = JsonReader.Get<string>("TypeName");
                Formatter = JsonReader.GetEnum<Formatters>("Formatter", Formatters.BinarySerializer);
                Label = JsonReader.Get<string>("Label");
                CustomId = JsonReader.Get<string>("CustomId");
                SessionId = JsonReader.Get<string>("SessionId");
                Command = JsonReader.Get<string>("Command");
                Source = JsonReader.Get<string>("Source");

                DuplexType = (DuplexTypes)JsonReader.Get<byte>("DuplexType");
                Expiration = JsonReader.Get<int>("Expiration");
                Creation = JsonReader.Get<DateTime>("Creation");
                Args = NameValueArgs.Convert((IDictionary<string, object>)JsonReader.Get("Args"));// dic.Get<NameValueArgs>("Args");
                TransformType = (TransformType)JsonReader.GetEnum<TransformType>("TransformType", TransformType.Object);
                //mqh-EncodingName = Types.NZorEmpty(JsonReader.Get<string>("EncodingName"), DefaultEncoding);
            }
            //JsonReader = null;
            return this;
        }

        public virtual object EntityRead(NameValueCollection queryString, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            if (queryString != null)
            {

                Identifier = queryString.Get<string>("Identifier");
                TypeName = queryString.Get<string>("TypeName");
                Formatter = queryString.GetEnum<Formatters>("Formatter", Formatters.Json);
                Label = queryString.Get<string>("Label");
                CustomId = queryString.Get<string>("CustomId");
                SessionId = queryString.Get<string>("SessionId");
                Command = queryString.Get<string>("Command");
                Source = queryString.Get<string>("Source");

                DuplexType = (DuplexTypes)queryString.Get<byte>("DuplexType");
                Expiration = queryString.Get<int>("Expiration");
                Creation = queryString.Get<DateTime>("Creation", DateTime.Now);
                var args = queryString.Get("Args");
                if (args != null)
                {
                    string[] nameValue = args.SplitTrim(':', ',', ';');
                    Args = NameValueArgs.Create(nameValue);
                }
                //Args = NameValueArgs.Convert((IDictionary<string, object>)queryString.Get("Args"));//queryString.Get<NameValueArgs>("Args");
                TransformType = (TransformType)queryString.GetEnum<TransformType>("TransformType", TransformType.Object);
                //mqh-EncodingName = Types.NZorEmpty(queryString.Get<string>("EncodingName"), DefaultEncoding);
            }

            return this;
        }

        #endregion

        #region Async Task

        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="messageOnError"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public TransStream AsyncTransStream(Func<NetStream> action, string messageOnError, TransformType transform = TransformType.Object)
        {
            Task<NetStream> task = Task.Factory.StartNew<NetStream>(action);
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    return TransStream.Write(task.Result, transform);
                }
            }
            //task.TryDispose();
            return TransStream.WriteState(-1, messageOnError);//, TransType.Error);
        }

        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="messageOnError"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public TransStream AsyncTransObject(Func<object> action, string messageOnError, TransformType transform = TransformType.Object)
        {
            Task<object> task = Task.Factory.StartNew<object>(action);
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result != null)
                        return TransStream.Write(task.Result, TransStream.ToTransType(TransformType));
                }
            }
            //task.TryDispose();
            return TransStream.WriteState(-1, messageOnError);//, TransType.Error);
        }


        public void AsyncTask(Action action)
        {
            Task task = Task.Factory.StartNew(action);
            {
                task.Wait();
                if (task.IsCompleted)
                {
                }
            }
            //task.TryDispose();
        }

        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionName"></param>
        /// <param name="nullState"></param>
        /// <returns></returns>
        public TransStream AsyncBinaryTask(Func<byte[]> action, string actionName, ChannelState nullState = ChannelState.ItemNotFound)
        {
            Task<byte[]> task = Task.Factory.StartNew<byte[]>(action);
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    if (task.Result != null)
                        return new TransStream(task.Result, 0, task.Result.Length, TransType.Stream);// TransWriter.Write(task.Result, TransType.Object);
                }
            }
            //task.TryDispose();
            return TransStream.WriteState((int)nullState, nullState.ToString());// TransType.State);  //TransStream.GetAckStream(nullState, actionName);//null;
        }
        #endregion

        #region ReadTransStream

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <param name="isTransStream"></param>
        public object ReadResponse(NetworkStream stream, int readTimeout, int ReceiveBufferSize, bool isTransStream)//TransformType transformType,
        {
            if (isTransStream)
            {
                return TransStream.CopyFrom(stream, readTimeout, ReceiveBufferSize);
            }

            using (TransStream ts = new TransStream(stream, readTimeout, ReceiveBufferSize, TransformType.Stream))//, transformType, isTransStream))
            {
                return ts.ReadValue();
            }
        }

        /// <summary>
        /// Convert an object of the specified type and whose value is equivalent to the specified object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public T Cast<T>(object o, bool enableException = false)
        {
            if (o is T)
            {
                return (T)o;
            }
            else
            {
                try
                {
                    return (T)System.Convert.ChangeType(o, typeof(T));
                }
                catch (InvalidCastException cex)
                {
                    if (enableException)
                        throw cex;
                    return default(T);
                }
            }

        }

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <returns></returns>
        public TResponse ReadResponse<TResponse>(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        {
            if (TransStream.IsTransStream(typeof(TResponse)))
            {
                TransStream ts = new TransStream(stream, readTimeout, ReceiveBufferSize, TransformType.Stream);// , TransformType.Stream,true);

                //TransStream ts = TransStream.CopyFrom(stream, readTimeout, ReceiveBufferSize);
                return Cast<TResponse>(ts, true);

            }
            using (TransStream ts = new TransStream(stream, readTimeout, ReceiveBufferSize, TransformType.Stream)) //, TransReader.ToTransformType(typeof(TResponse)), false))
            {
                return ts.ReadValue<TResponse>();
            }
        }

        //public object ReadTransStream(NamedPipeClientStream stream, int ReceiveBufferSize = 8192)
        //{
        //    return new TransStream(stream, ReceiveBufferSize);
        //}

        public object ReadResponse(NamedPipeClientStream stream, int ReceiveBufferSize, TransformType transformType, bool isTransStream)
        {
            if (isTransStream)
            {
                return TransStream.CopyFrom(stream, ReceiveBufferSize);
            }

            using (TransStream ts = new TransStream(stream, ReceiveBufferSize, transformType))//, transformType, isTransStream))
            {
                return ts.ReadValue();
            }
        }
        public TResponse ReadResponse<TResponse>(NamedPipeClientStream stream, int ReceiveBufferSize = 8192)
        {
            if (TransStream.IsTransStream(typeof(TResponse)))
            {
                TransStream ts = TransStream.CopyFrom(stream, ReceiveBufferSize);
                return GenericTypes.Cast<TResponse>(ts, true);
            }
            using (TransStream ts = new TransStream(stream, ReceiveBufferSize, TransStream.ToTransformType(typeof(TResponse)), false))
            {
                return ts.ReadValue<TResponse>();
            }
        }


        #endregion

        #region extension

        /// <summary>
        /// Set the given value to body stream using <see cref="BinarySerializer"/>, This method is a part of <see cref="IMessageStream"/> implementation..
        /// </summary>
        /// <param name="value"></param>
        public static NetStream SerializeBody(object value)
        {

            if (value != null)
            {
                //TypeName = value.GetType().FullName;

                NetStream ns = new NetStream();
                var ser = new BinarySerializer();
                ser.Serialize(ns, value);
                ns.Position = 0;
                return ns;
            }
            else
            {
                //TypeName = typeof(object).FullName;
                return null;
            }
        }

        /// <summary>
        /// Convert <see cref="MessageHeader"/> to <see cref="IDictionary"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IDictionary ConvertTo(MessageHeader message)
        {
            IDictionary dict = new Dictionary<string, object>();
            dict.Add("Identifier", message.Identifier);
            //if (message._Body != null)
            //    dict.Add("Body", message.Body);
            dict.Add("TypeName", message.TypeName);
            dict.Add("Formatter", (byte)message.Formatter);
            dict.Add("Label", message.Label);
            dict.Add("CustomId", message.CustomId);
            dict.Add("SessionId", message.SessionId);
            dict.Add("Command", message.Command);
            dict.Add("Source", message.Source);
            dict.Add("DuplexType", (byte)message.DuplexType);
            dict.Add("Expiration", message.Expiration);
            dict.Add("Creation", message.Creation);
            if (message.Args != null)
                dict.Add("Args", message.Args);
            dict.Add("TransformType", (byte)message.TransformType);
            //mqh-dict.Add("EncodingName", message.EncodingName);
            return dict;
        }

        public DynamicEntity ToEntity()
        {
            dynamic entity = new DynamicEntity();
            entity.Identifier = this.Identifier;
            entity.TypeName = this.TypeName;
            entity.Formatter = this.Formatter;
            entity.Label = this.Label;
            entity.CustomId = this.CustomId;
            entity.SessionId = this.SessionId;
            entity.Command = this.Command;
            entity.Source = this.Source;
            DuplexType = this.DuplexType;
            entity.Expiration = this.Expiration;
            entity.Creation = this.Creation;
            entity.Args = this.Args;
            entity.TransformType = (byte)this.TransformType;
            //mqh-entity.EncodingName = this.EncodingName;

            return entity;

        }

        public string ToJson(bool pretty = false)
        {
            return EntityWrite(new JsonSerializer(JsonSerializerMode.Write, null), pretty);
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
      
        //internal static MessageHeader ServerReadRequest(NetProtocol protocol,Stream streamServer, int ReceiveBufferSize = 8192)
        //{
        //    var message = Factory(protocol);
        //    message.EntityRead(streamServer, null);

        //    return message;
        //}

        internal static void ServerWriteResponse(Stream streamServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            streamServer.Write(bResponse.ToArray(), 0, bResponse.iLength);

            streamServer.Flush();

        }

        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageHeader"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static MessageHeader ConvertFrom(IDictionary<string, object> dict, NetProtocol protocol)
        {
            MessageHeader message = new MessageHeader();

            message.Identifier = dict.Get<string>("Identifier");
            message.TypeName = dict.Get<string>("TypeName");
            message.Formatter = (Formatters)dict.Get<byte>("Formatter");
            message.Label = dict.Get<string>("Label");
            message.CustomId = dict.Get<string>("CustomId");
            message.SessionId = dict.Get<string>("SessionId");
            message.Command = dict.Get<string>("Command");
            message.Source = dict.Get<string>("Source");

            message.DuplexType = (DuplexTypes)dict.Get<byte>("DuplexType", 0);
            message.Expiration = dict.Get<int>("Expiration", 0);
            message.Creation = dict.Get<DateTime>("Creation", DateTime.Now);
            message.Args = dict.Get<NameValueArgs>("Args");
            message.TransformType = (TransformType)dict.Get<byte>("TransformType");
            //mqh-message.EncodingName = Types.NZorEmpty(dict.Get<string>("EncodingName"), DefaultEncoding);

            return message;
        }

        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageHeader"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageHeader ConvertFrom(IDictionary<string, object> dict)
        {
            MessageHeader message = new MessageHeader();

            message.Identifier = dict.Get<string>("Identifier");
            message.TypeName = dict.Get<string>("TypeName");
            message.Formatter = (Formatters)dict.Get<byte>("Formatter");
            message.Label = dict.Get<string>("Label");
            message.CustomId = dict.Get<string>("CustomId");
            message.SessionId = dict.Get<string>("SessionId");
            message.Command = dict.Get<string>("Command");
            message.Source = dict.Get<string>("Source");
            message.DuplexType = (DuplexTypes)dict.Get<byte>("DuplexType", 0);
            message.Expiration = dict.Get<int>("Expiration", 0);
            message.Creation = dict.Get<DateTime>("Creation", DateTime.Now);
            message.Args = dict.Get<NameValueArgs>("Args");
            message.TransformType = (TransformType)dict.Get<byte>("TransformType");
            //mqh-message.EncodingName = Types.NZorEmpty(dict.Get<string>("EncodingName"), DefaultEncoding);

            return message;
        }

        #endregion

        #region static
        /// <summary>
        /// Create instant of MessageHeader
        /// </summary>
        /// <param name="command"></param>
        /// <param name="id"></param>
        /// <param name="label"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static MessageHeader Create(string command, string id, string label, int expiration = 0, string sessionId = null)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");

            if (expiration < 0)
                expiration = 0;
            MessageHeader message = new MessageHeader()
            {
                Command = command,
                CustomId = id,
                Label = label,
                Expiration = expiration,
                SessionId = sessionId,
                DuplexType = DuplexTypes.Respond,
                TransformType = TransformType.Object
            };
            return message;
        }

        #endregion

    }
}
