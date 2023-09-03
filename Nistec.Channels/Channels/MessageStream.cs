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

namespace Nistec.Channels
{

    /// <summary>
    /// Represent a message stream for network communication like namedPipe or Tcp.
    /// This message can serialize/desrialize fast and easly using the <see cref="BinaryStreamer"/>
    /// </summary>
    [Serializable]//IMessageStream,
    public abstract class MessageStream : ISerialEntity, ISerialJson, IMessageStream, IBodyStream, ITransformResponse, INotify, IDisposable, ITransformMessage
    {

        //public ITransformHeader Transform { get; protected set; }

        //byte[] _Body;
        //public byte[] Body
        //{
        //    get { return _Body; }
        //    set
        //    {
        //        _Body = value;
        //        if (Body == null)
        //            _BodyStream = null;
        //        _BodyStream = new NetStream(value);
        //    }
        //}

        //NetStream _BodyStream;
        //public NetStream BodyStream
        //{
        //    get { return _BodyStream; }
        //}

        //public NetStream BodyStream
        //{
        //    get { return (Body == null) ? null: new NetStream(Body);}
        //}

        public NetStream BodyStream()
        {
            if (_Body == null)
                return null;
            return new NetStream(_Body);
        }
        public void BodyStream(byte[] bytes)
        {
            _Body = bytes;
        }
        public void BodyStream(NetStream stream)
        {
            _Body = stream.ToArray(); ;
        }

        /// <summary>
        /// Get or Set The message body stream.
        /// </summary>
        protected byte[] _Body;

        //protected byte[] BodyBinary();
        //public abstract NetStream BodyStream();

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
        ///// <summary>
        /// Get or Set The message body stream.
        /// </summary>
        //NetStream _BodyStream;
        //public NetStream BodyStream { get; set; }
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

        #region ___ITransformMessage
        /*
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
         */
        ///// <summary>
        /////  Get or Set The message expiration int minutes.
        ///// </summary>
        //public int Expiration { get; set; }
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
        /// Initialize a new instance of MessageStream
        /// </summary>
        public MessageStream()
        {
            Identifier = UUID.Identifier();
            Creation = DateTime.Now;
            //mqh-EncodingName = "utf-8";
            _Args = new NameValueArgs();
            Formatter = Formatters.BinarySerializer;
        }
        protected MessageStream(Guid itemId) : this(itemId.ToString())
        {
            //Identifier = itemId.ToString();
            //Modified = DateTime.Now;
            //EncodingName = "utf-8";
        }
        protected MessageStream(string identifier)
        {
            Identifier = ValidIdentifier(identifier);
            Creation = DateTime.Now;
            //mqh-EncodingName = "utf-8";
            _Args = new NameValueArgs();

        }

        protected MessageStream(HttpRequestInfo request) : this()
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
        /// Initialize a new instance of MessageStream from stream using for <see cref="ISerialEntity"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        protected MessageStream(Stream stream, IBinaryStreamer streamer) : this()
        {
            EntityRead(stream, streamer);
        }
        /// <summary>
        /// Initialize a new instance of MessageStream from <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="info"></param>
        protected MessageStream(SerializeInfo info) : this()
        {
            Identifier = info.GetValue<string>("Identifier");
            _Body = (byte[])info.GetValue("Body");
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

        protected MessageStream(IDictionary<string, object> dict) : this()
        {
            Identifier = dict.Get<string>("Identifier");
            _Body = dict.Get<byte[]>("Body", null);//, ConvertDescriptor.Implicit),
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

        public MessageStream(MessageStream copy) : this()
        {
            Copy(copy);
        }

        void Copy(MessageStream copy)
        {
            Identifier = copy.Identifier;
            _Body = copy._Body;
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
                if (_Body != null)
                {
                    //BodyStream.Dispose();
                    _Body = null;
                }
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

        int GetSize()
        {
            if (_Body == null)
                return 0;
            return _Body.Length;
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
                return _Body == null || _Body.Length == 0;
            }
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

        #region Convert

        /// <summary>
        /// Convert body to string.
        /// </summary>
        /// <returns></returns>
        public string BodyToString()
        {
            if (_Body == null)
                return null;
            var body = DecodeBody();
            if (body == null)
                return null;
            return body.ToString();
            //return System.Text.Encoding.GetEncoding(Types.NZorEmpty(EncodingName, DefaultEncoding)).GetString(BodyStream.ToArray());
        }

        /// <summary>
        /// Convert body to json string.
        /// </summary>
        /// <returns></returns>
        public string BodyToJson<T>()
        {
            if (_Body == null)
                return null;
            T body = DecodeBody<T>();
            return JsonSerializer.Serialize(body);
        }

        /// <summary>
        /// Convert body to base 64 string.
        /// </summary>
        /// <returns></returns>
        public string BodyToBase64()
        {
            if (_Body == null)
                return null;
            return BinarySerializer.ToBase64(_Body);
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
        public virtual void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteString(Identifier);
            streamer.WriteValue(_Body);
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
            _Body = (byte[])streamer.ReadValue();
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
            info.Add("Body", _Body);
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
            _Body = (byte[])info.GetValue("Body");
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
            serializer.WriteToken("Body", _Body == null ? null : BinarySerializer.ToBase64(_Body));
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
                var body = JsonReader.Get<string>("Body");
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

                if (body != null && body.Length > 0)
                    _Body = BinarySerializer.FromBase64(body);// NetStream.FromBase64String(body);

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
                var body = JsonReader.Get<string>("Body");
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

                if (body != null && body.Length > 0)
                    _Body = BinarySerializer.FromBase64(body); //NetStream.FromBase64String(body);
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
                var body = queryString.Get<string>("Body");
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

                if (body != null && body.Length > 0)
                    _Body = BinarySerializer.FromBase64(body); //NetStream.FromBase64String(body);
            }

            return this;
        }

        #endregion

        #region IMessageStream
        /// <summary>
        /// Get body stream ready to read from position 0, is a part of <see cref="IBodyStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public NetStream GetStream()
        {
            if (_Body == null)
                return null;
            return BodyStream();
        }

        /// <summary>
        /// Get copy of body stream, is a part of <see cref="IBodyStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public NetStream GetCopy()
        {
            if (_Body == null)
                return null;
            return BodyStream().Copy();
        }

        public byte[] GetBytes()
        {
            if (_Body == null)
                return null;
            byte[] bytes=new byte[_Body.Length];
            Array.Copy(_Body,bytes, _Body.Length);
            return bytes;
        }

        public string GetBodyString()
        {
            if (_Body == null)
                return null;
            return UTF8Encoding.UTF8.GetString(_Body);
        }

        public void SetState(int state, string message)
        {
            //State = (MessageState)state;
            //Message = message;

            this.SetBody(message);
        }

        /// <summary>
        /// Set the given value to body stream using <see cref="BinarySerializer"/>, This method is a part of <see cref="IMessageStream"/> implementation..
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetBody(object value)
        {

            if (value == null)
            {
                TypeName = typeof(object).FullName;
                _Body = null;
            }
            else if (value is byte[])
            {
                TypeName = value.GetType().FullName;
                _Body =(byte[]) value;
            }
            else if (value is NetStream)
            {
                TypeName = value.GetType().FullName;
                _Body = ((NetStream)value).ToArray();
            }
            else
            {
                TypeName = value.GetType().FullName;

                using (NetStream ns = new NetStream())
                {
                    var ser = new BinarySerializer();
                    ser.Serialize(ns, value);
                    ns.Position = 0;
                    _Body = ns.ToArray();
                }
            }
        }

        public virtual void SetBody(NetStream stream, Type type)
        {
            TypeName = (type != null) ? type.FullName : typeof(object).FullName;
            if (stream != null)
            {
                _Body = stream.ToArray();
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
                _Body = value;
            }
        }
        /// <summary>
        /// Deserialize body stream to object, This method is a part of <see cref="IMessageStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public virtual object DecodeBody()
        {
            if (_Body == null)
                return null;
            using (var sream = BodyStream())
            {
                var ser = new BinarySerializer();
                return ser.Deserialize(sream);
            }
        }
        /// <summary>
        ///  Deserialize body stream to generic object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T DecodeBody<T>()
        {
            return GenericTypes.Cast<T>(DecodeBody(), true);
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
            writer.Serialize(stream, entity);
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
                _Body = value;
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
                //if (copy)
                //    ns.CopyTo(BodyStream);
                //else
                    _Body = ns.ToArray();
            }
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
            task.TryDispose();
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
            task.TryDispose();
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
            task.TryDispose();
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
            task.TryDispose();
            return TransStream.WriteState((int)nullState, nullState.ToString());// TransType.State);  //TransStream.GetAckStream(nullState, actionName);//null;
        }
        #endregion

        #region ReadTransStream


        //public object ReadTransStream(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        //{
        //    return new TransStream(stream, readTimeout, ReceiveBufferSize);
        //}

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
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
        /// Convert <see cref="MessageStream"/> to <see cref="IDictionary"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IDictionary ConvertTo(MessageStream message)
        {
            IDictionary dict = new Dictionary<string, object>();
            dict.Add("Identifier", message.Identifier);
            //if (message._Body != null)
            //    dict.Add("Body", message.Body);
            dict.Add("Body", message._Body);
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

        public IDictionary<string, object> ToDictionary()
        {
            var dic = DictionaryUtil.ToDictionary(this, "");
            if (_Body != null)
            {
                dic["Body"] = this.DecodeBody();
            }
            return dic;
        }

        public DynamicEntity ToEntity()
        {
            dynamic entity = new DynamicEntity();
            entity.Identifier = this.Identifier;
            if (_Body != null)
            {
                var body = this.DecodeBody();
                if (body != null)
                    entity.Body = DictionaryUtil.ToDictionaryOrObject(body, "");
            }
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
        /// <summary>
        /// Convert stream to <see cref="TcpMessage"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static MessageStream ParseStream(Stream stream, NetProtocol protocol)
        {
            var message = Factory(protocol);
            message.EntityRead(stream, null);
            return message;
        }
        /// <summary>
        /// Convert stream to <see cref="TcpMessage"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MessageStream ParseStream(Stream stream)
        {
            var message = new GenericMessage();
            message.EntityRead(stream, null);
            return message;
        }
        //internal static MessageStream ServerReadRequest(NetProtocol protocol,Stream streamServer, int ReceiveBufferSize = 8192)
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
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary<string, object> dict, NetProtocol protocol)
        {
            MessageStream message = Factory(protocol);

            message.Identifier = dict.Get<string>("Identifier");
            message._Body = dict.Get<byte[]>("Body", null);//, ConvertDescriptor.Implicit),
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
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary<string, object> dict)
        {
            MessageStream message = new GenericMessage();

            message.Identifier = dict.Get<string>("Identifier");
            message._Body = dict.Get<byte[]>("Body", null);//, ConvertDescriptor.Implicit),
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

        //internal static IMessageStream Create(NetProtocol protocol,string command, string id, int expiration=0)
        //{
        //    return Create(protocol,command, id, null, null, null, expiration);
        //}
        //internal static IMessageStream Create(NetProtocol protocol, string command, string id, object value, int expiration = 0)
        //{
        //    return Create(protocol, command, id, null, value, null, expiration);
        //}
        //internal static IMessageStream Create(NetProtocol protocol, string command, string id, string label, object value, string[] args, int expiration = 0, TransformType transformType = TransformType.Object, bool isDuplex = true)
        //{
        //    if (string.IsNullOrEmpty(command))
        //        throw new ArgumentNullException("CreateMessage.command");

        //    if (expiration < 0)
        //        expiration = 0;
        //    IMessageStream message = null;
        //    switch (protocol)
        //    {
        //        case NetProtocol.Tcp:
        //            message = new TcpMessage(command, id, value, expiration);
        //            break;
        //        case NetProtocol.Pipe:
        //            message = new PipeMessage(command, id, value, expiration);
        //            break;
        //        case NetProtocol.Http:
        //            message = new HttpMessage(command, id, value, expiration);
        //            break;
        //        default:
        //            throw new ArgumentException("Protocol is not supported " + protocol.ToString());
        //    }
        //    message.IsDuplex = isDuplex;
        //    message.TransformType = transformType;
        //    if (label != null)
        //        message.Label = label;
        //    if (args != null)
        //        message.Args = MessageStream.CreateArgs(args);

        //    return message;
        //}

        internal static IMessageStream Create(NetProtocol protocol, string command, string id, int expiration = 0)
        {
            return Create(protocol, command, id, null, null, expiration);
        }
        //internal static IMessageStream Create(NetProtocol protocol, string command, string id, object value, int expiration = 0)
        //{
        //    return Create(protocol, command, id, null, value, null, expiration);
        //}


        internal static IMessageStream Create(NetProtocol protocol, string command, string id, string label, object value, int expiration = 0, string sessionId = null)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");

            if (expiration < 0)
                expiration = 0;
            IMessageStream message = null;
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    message = new TcpMessage(command, id, value, expiration, sessionId);
                    break;
                case NetProtocol.Pipe:
                    message = new PipeMessage(command, id, value, expiration, sessionId);
                    break;
                case NetProtocol.Http:
                    message = new HttpMessage(command, id, value, expiration, sessionId);
                    break;
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
            //message.IsDuplex = true;// isDuplex;
            message.DuplexType = DuplexTypes.Respond;
            message.TransformType = TransformType.Object;// transformType;
            if (label != null)
                message.Label = label;
            //if (args != null)
            //    message.Args = MessageStream.CreateArgs(args);

            return message;
        }
        /// <summary>
        /// Create instant of MessageStream
        /// </summary>
        /// <param name="command"></param>
        /// <param name="id"></param>
        /// <param name="label"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static MessageStream Create(string command, string id, string label, object value, int expiration = 0, string sessionId = null)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");

            if (expiration < 0)
                expiration = 0;
            MessageStream message = new GenericMessage(command, id, label, value, expiration, sessionId);
            message.DuplexType = DuplexTypes.Respond;
            message.TransformType = TransformType.Object;// transformType;
            return message;
        }


        /// <summary>
        /// Create a new message stream.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public static MessageStream Create(NetProtocol protocol, Stream stream, IBinaryStreamer streamer)
        {
            MessageStream message = Factory(protocol);
            message.EntityRead(stream, streamer);
            return message;
        }

        public static MessageStream Factory(NetProtocol protocol)
        {
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    return new TcpMessage();
                case NetProtocol.Pipe:
                    return new PipeMessage();
                case NetProtocol.Http:
                    return new HttpMessage();
                default:
                    throw new ArgumentException("Not supported NA Protocol");
            }
        }

        public static TransformType GetTransformType(Type type)
        {
            if (type == typeof(TransStream))
                return TransformType.Stream;
            //if (SerializeTools.IsStream(type))
            //    return TransformType.Stream;
            return TransformType.Object;
        }
        #endregion

        #region Read/Write pipe

        public string ReadResponseAsJson(NamedPipeClientStream stream, int ReceiveBufferSize, TransformType transformType, bool isTransStream)
        {// = 8192

            if (isTransStream)
            {
                using (TransStream ts = TransStream.CopyFrom(stream, ReceiveBufferSize))
                {
                    return ts.ReadToJson();
                }
            }

            using (TransStream ack = new TransStream(stream, ReceiveBufferSize, transformType)) //, transformType, isTransStream))
            {
                return ack.ReadToJson();
            }
        }

        public static MessageStream ReadRequest(NamedPipeServerStream pipeServer, int ReceiveBufferSize = 8192)
        {
            PipeMessage message = new PipeMessage();
            message.EntityRead(pipeServer, null);
            return message;
        }

        internal static void WriteResponse(NamedPipeServerStream pipeServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            pipeServer.Write(bResponse.ToArray(), 0, bResponse.iLength);

            pipeServer.Flush();

        }

        #endregion

        #region Read/Write tcp

        //internal static NetStream FaultStream(string faultDescription)
        //{
        //    var message = new CacheMessage("Fault", "Fault", faultDescription, 0);
        //    return message.Serialize();
        //}

        public static MessageStream ReadRequest(NetworkStream streamServer, int ReceiveBufferSize = 8192)
        {
            //var message = new TcpMessage();
            //using (var ntStream = new NetStream())
            //{
            //    ntStream.CopyFrom(streamServer, 0, ReceiveBufferSize);

            //    message.EntityRead(ntStream, null);
            //}
            //return message;

            var message = new TcpMessage();
            message.EntityRead(streamServer, null);
            return message;
        }

        internal static void WriteResponse(NetworkStream streamServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            int cbResponse = bResponse.iLength;

            streamServer.Write(bResponse.ToArray(), 0, cbResponse);

            streamServer.Flush();

        }


        #endregion

        #region Read/Write http

        public static MessageStream ReadRequest(HttpRequestInfo request)
        {
            if (request.BodyStream != null)
            {
                return MessageStream.ParseStream(request.BodyStream, NetProtocol.Http);
            }
            else
            {

                var message = new HttpMessage();

                if (request.QueryString != null)//request.BodyType == HttpBodyType.QueryString)
                    message.EntityRead(request.QueryString, null);
                else if (request.Body != null)
                    message.EntityRead(request.Body, null);
                //else if (request.Url.LocalPath != null && request.Url.LocalPath.Length > 1)
                //    message.EntityRead(request.Url.LocalPath.TrimStart('/').TrimEnd('/'), null);

                return message;
            }
        }

        internal static void WriteResponse(HttpListenerContext context, NetStream bResponse)
        {
            var response = context.Response;
            if (bResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.StatusDescription = "No response";
                return;
            }

            int cbResponse = bResponse.iLength;
            byte[] buffer = bResponse.ToArray();



            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();

        }


        #endregion

    }

#if (false)
    /// <summary>
    /// Represent a message stream for network communication like namedPipe or Tcp.
    /// This message can serialize/desrialize fast and easly using the <see cref="BinaryStreamer"/>
    /// </summary>
    [Serializable]//IMessageStream,
    public abstract class MessageStream : ISerialEntity,  ISerialJson, IMessageStream, IBodyStream, ITransformResponse, INotify, IDisposable, ITransformMessage
    {

        //public ITransformHeader Transform { get; protected set; }

        //public NetStream BodyStream()
        //{
        //    if (Body == null)
        //        return null;
        //    return new NetStream(Body);
        //}
        //public void InputStream(byte[] bytes)
        //{
        //    Body = bytes;
        //}

        ///// <summary>
        ///// Get or Set The message body stream.
        ///// </summary>
        //protected byte[] Body { get; set; }

        //protected byte[] BodyBinary();
        //public abstract NetStream BodyStream();

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
        ///// <summary>
        /// Get or Set The message body stream.
        /// </summary>
        //NetStream _BodyStream;
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

    #region ___ITransformMessage
        /*
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
         */
        ///// <summary>
        /////  Get or Set The message expiration int minutes.
        ///// </summary>
        //public int Expiration { get; set; }
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
        /// Initialize a new instance of MessageStream
        /// </summary>
        public MessageStream()
        {
            Identifier = UUID.Identifier();
            Creation = DateTime.Now;
            //mqh-EncodingName = "utf-8";
            _Args = new NameValueArgs();
            Formatter = Formatters.BinarySerializer;
        }
        protected MessageStream(Guid itemId):this(itemId.ToString())
        {
            //Identifier = itemId.ToString();
            //Modified = DateTime.Now;
            //EncodingName = "utf-8";
        }
        protected MessageStream(string identifier)
        {
            Identifier = ValidIdentifier(identifier);
            Creation = DateTime.Now;
            //mqh-EncodingName = "utf-8";
            _Args = new NameValueArgs();

        }

        protected MessageStream (HttpRequestInfo request):this()
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
        /// Initialize a new instance of MessageStream from stream using for <see cref="ISerialEntity"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        protected MessageStream(Stream stream, IBinaryStreamer streamer) : this()
        {
            EntityRead(stream, streamer);
        }
        /// <summary>
        /// Initialize a new instance of MessageStream from <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="info"></param>
        protected MessageStream(SerializeInfo info) : this()
        {
            Identifier = info.GetValue<string>("Identifier");
            BodyStream = (NetStream)info.GetValue("BodyStream");
            TypeName = info.GetValue<string>("TypeName");
            Formatter = (Formatters)info.GetValue<int>("Formatter");
            Label = info.GetValue<string>("Label");
            CustomId = info.GetValue<string>("CustomId");
            SessionId = info.GetValue<string>("SessionId");
            Command = info.GetValue<string>("Command");
            Source = info.GetValue<string>("Source ");
            DuplexType =(DuplexTypes) info.GetValue<byte>("DuplexType");
            Expiration = info.GetValue<int>("Expiration");
            Creation = info.GetValue<DateTime>("Creation");
            Args = (NameValueArgs)info.GetValue("Args");
            TransformType = (TransformType)info.GetValue<byte>("TransformType");
            //mqh-EncodingName = Types.NZorEmpty(info.GetValue<string>("EncodingName"), DefaultEncoding);
        }

        protected MessageStream(IDictionary<string, object> dict) : this()
        {
            Identifier = dict.Get<string>("Identifier");
            BodyStream = dict.Get<NetStream>("Body", null);//, ConvertDescriptor.Implicit),
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

        public MessageStream(MessageStream copy) : this()
        {
            Copy(copy);
        }

        void Copy(MessageStream copy)
        {
            Identifier = copy.Identifier;
            BodyStream = copy.BodyStream;
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

        int GetSize()
        {
            if (BodyStream == null)
                return 0;
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
                else {
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

    #region Convert

        /// <summary>
        /// Convert body to string.
        /// </summary>
        /// <returns></returns>
        public string BodyToString()
        {
            if (BodyStream == null)
                return null;
            var body = DecodeBody();
            if (body == null)
                return null;
            return body.ToString();
            //return System.Text.Encoding.GetEncoding(Types.NZorEmpty(EncodingName, DefaultEncoding)).GetString(BodyStream.ToArray());
        }

        /// <summary>
        /// Convert body to json string.
        /// </summary>
        /// <returns></returns>
        public string BodyToJson<T>()
        {
            if (BodyStream == null)
                return null;
            T body = DecodeBody<T>();
            return JsonSerializer.Serialize(body);
        }

        /// <summary>
        /// Convert body to base 64 string.
        /// </summary>
        /// <returns></returns>
        public string BodyToBase64()
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
        public virtual void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteString(Identifier);
            streamer.WriteValue(BodyStream);
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
            BodyStream = (NetStream)streamer.ReadValue();
            TypeName = streamer.ReadString();
            Formatter = (Formatters)streamer.ReadValue<int>();
            Label = streamer.ReadString();
            CustomId = streamer.ReadString();
            SessionId = streamer.ReadString();
            Command = streamer.ReadString();
            Source = streamer.ReadString();
            DuplexType =(DuplexTypes) streamer.ReadValue<byte>();
            Expiration = streamer.ReadValue<int>();
            Creation = streamer.ReadValue<DateTime>();
            Args = (NameValueArgs)streamer.ReadValue();
            TransformType =(TransformType) streamer.ReadValue<byte>();
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
            info.Add("BodyStream", BodyStream);
            info.Add("TypeName", TypeName);
            info.Add("Formatter", (int)Formatter);
            info.Add("Label", Label);
            info.Add("CustomId", CustomId);
            info.Add("SessionId", SessionId);
            info.Add("Command", Command);
            info.Add("Source", Source);
            //info.Add("IsDuplex", IsDuplex);
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
            BodyStream = (NetStream)info.GetValue("BodyStream");
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

            object body = null;
            if (BodyStream != null)
            {
                body = BinarySerializer.ConvertFromStream(BodyStream);
            }


            serializer.WriteToken("Identifier", Identifier);
            serializer.WriteToken("BodyStream", BodyStream == null ? null : BodyStream.ToBase64String());
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

            //if (BodyStream != null)
            //    serializer.WriteToken("Message", body);
            //else
            //    serializer.WriteToken("Message", Message);

            //serializer.WriteToken("Query", Query);
            //serializer.WriteToken("State", State);

            serializer.WriteToken("Body", body);

            return serializer.WriteOutput(pretty);

        }
        //protected Dictionary<string, object> JsonReader;
        public virtual object EntityRead(Dictionary<string, object> JsonReader, IJsonSerializer serializer)
        {

            if (JsonReader != null)
            {
                Identifier = JsonReader.Get<string>("Identifier");
                var body = JsonReader.Get<string>("BodyStream");
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

                if (body != null && body.Length > 0)
                    BodyStream = NetStream.FromBase64String(body);

            }
            return this;
        }
        public virtual object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            //var queryParams = new Dictionary<string, string>(HtmlPage.Document.QueryString, StringComparer.InvariantCultureIgnoreCase);
          
           var     JsonReader = serializer.Read<Dictionary<string, object>>(json);

            if (JsonReader != null)
            {
                Identifier = JsonReader.Get<string>("Identifier");
                var body = JsonReader.Get<string>("BodyStream");
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
                //IsDuplex = dic.Get<bool>("IsDuplex");
                Args = NameValueArgs.Convert((IDictionary<string, object>)JsonReader.Get("Args"));// dic.Get<NameValueArgs>("Args");
                TransformType = (TransformType)JsonReader.GetEnum<TransformType>("TransformType", TransformType.Object);
                //mqh-EncodingName = Types.NZorEmpty(JsonReader.Get<string>("EncodingName"), DefaultEncoding);

                if (body != null && body.Length > 0)
                    BodyStream = NetStream.FromBase64String(body);
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
                var body = queryString.Get<string>("BodyStream");
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

                if (body != null && body.Length > 0)
                    BodyStream = NetStream.FromBase64String(body);
            }

            return this;
        }

    #endregion
        
    #region IMessageStream
        /// <summary>
        /// Get body stream ready to read from position 0, is a part of <see cref="IBodyStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public NetStream GetStream()
        {
            if (BodyStream == null)
                return null;
            return BodyStream.Ready();
        }

        /// <summary>
        /// Get copy of body stream, is a part of <see cref="IBodyStream"/> implementation.
        /// </summary>
        /// <returns></returns>
        public NetStream GetCopy()
        {
            if (BodyStream == null)
                return null;
            return BodyStream.Copy();
        }

        public byte[] GetBytes()
        {
            if (BodyStream == null)
                return null;
            return BodyStream.ToArray();
        }

        public string GetBodyString()
        {
            if (BodyStream == null)
                return null;
            return UTF8Encoding.UTF8.GetString(BodyStream.ToArray()) ;
        }

        public void SetState(int state, string message)
        {
            //State = (MessageState)state;
            //Message = message;

            this.SetBody(message);
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
            //BodyStream.Position = 0;
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
            return GenericTypes.Cast<T>(DecodeBody(), true);
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
            task.TryDispose();
            return TransStream.WriteState(-1, messageOnError);//, TransType.Error);
        }

        /// <summary>
        /// Execute async task request and return the response as<see cref="NetStream"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="messageOnError"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public TransStream AsyncTransObject(Func<object> action, string messageOnError, TransformType transform= TransformType.Object)
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
            task.TryDispose();
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
            task.TryDispose();
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
                        return new TransStream(task.Result,0, task.Result.Length, TransType.Stream);// TransWriter.Write(task.Result, TransType.Object);
                }
            }
            task.TryDispose();
            return TransStream.WriteState((int)nullState, nullState.ToString());// TransType.State);  //TransStream.GetAckStream(nullState, actionName);//null;
        }
    #endregion

    #region ReadTransStream
        
         
        //public object ReadTransStream(NetworkStream stream, int readTimeout, int ReceiveBufferSize)
        //{
        //    return new TransStream(stream, readTimeout, ReceiveBufferSize);
        //}

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        public object ReadResponse(NetworkStream stream, int readTimeout, int ReceiveBufferSize,  bool isTransStream)//TransformType transformType,
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
        /// Convert <see cref="MessageStream"/> to <see cref="IDictionary"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IDictionary ConvertTo(MessageStream message)
        {
            IDictionary dict = new Dictionary<string, object>();
            dict.Add("Identifier", message.Identifier);
            if (message.BodyStream != null)
                dict.Add("Body", message.BodyStream);
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

            //if (message.IsDuplex)
            //    dict.Add("IsDuplex", message.IsDuplex);

            //if (message.ReturnTypeName != null)
            //    dict.Add("ReturnTypeName", message.ReturnTypeName);
            return dict;
        }

        public IDictionary<string,object> ToDictionary()
        {
            var dic = DictionaryUtil.ToDictionary(this, "");
            if (BodyStream != null)
            {
                dic["Body"] = this.DecodeBody();
            }
            return dic;
        }

        public DynamicEntity ToEntity()
        {
            dynamic entity = new DynamicEntity();
            entity.Identifier = this.Identifier;
            if (BodyStream != null)
            {
                var body = this.DecodeBody();
                if (body != null)
                    entity.Body = DictionaryUtil.ToDictionaryOrObject(body, "");
            }
            entity.TypeName = this.TypeName;
            entity.Formatter = this.Formatter;
            entity.Label = this.Label;
            entity.CustomId = this.CustomId;
            entity.SessionId = this.SessionId;
            entity.Command = this.Command;
            entity.Source = this.Source;
            //entity.IsDuplex = this.IsDuplex;
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
        /// <summary>
        /// Convert stream to <see cref="TcpMessage"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static MessageStream ParseStream(Stream stream, NetProtocol protocol)
        {
            var message = Factory(protocol);
            message.EntityRead(stream, null);
            return message;
        }
        /// <summary>
        /// Convert stream to <see cref="TcpMessage"/> message.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MessageStream ParseStream(Stream stream)
        {
            var message = new GenericMessage();
            message.EntityRead(stream, null);
            return message;
        }
        //internal static MessageStream ServerReadRequest(NetProtocol protocol,Stream streamServer, int ReceiveBufferSize = 8192)
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
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary<string,object> dict, NetProtocol protocol)
        {
            MessageStream message = Factory(protocol);

            message.Identifier = dict.Get<string>("Identifier");
            message.BodyStream = dict.Get<NetStream>("Body", null);//, ConvertDescriptor.Implicit),
            message.TypeName = dict.Get<string>("TypeName");
            message.Formatter = (Formatters)dict.Get<byte>("Formatter");
            message.Label = dict.Get<string>("Label");
            message.CustomId = dict.Get<string>("CustomId");
            message.SessionId = dict.Get<string>("SessionId");
            message.Command = dict.Get<string>("Command");
            message.Source = dict.Get<string>("Source");

            message.DuplexType = (DuplexTypes)dict.Get<byte>("DuplexType", 0);
            message.Expiration = dict.Get<int>("Expiration", 0);
            //message.IsDuplex = dict.Get<bool>("IsDuplex", true);

            message.Creation = dict.Get<DateTime>("Creation", DateTime.Now);
            message.Args = dict.Get<NameValueArgs>("Args");
            message.TransformType = (TransformType)dict.Get<byte>("TransformType");
            //mqh-message.EncodingName = Types.NZorEmpty(dict.Get<string>("EncodingName"), DefaultEncoding);

            return message;
        }

        /// <summary>
        /// Convert <see cref="IDictionary"/> to <see cref="MessageStream"/>.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static MessageStream ConvertFrom(IDictionary<string, object> dict)
        {
            MessageStream message = new GenericMessage();

            message.Identifier = dict.Get<string>("Identifier");
            message.BodyStream = dict.Get<NetStream>("Body", null);//, ConvertDescriptor.Implicit),
            message.TypeName = dict.Get<string>("TypeName");
            message.Formatter = (Formatters)dict.Get<byte>("Formatter");
            message.Label = dict.Get<string>("Label");
            message.CustomId = dict.Get<string>("CustomId");
            message.SessionId = dict.Get<string>("SessionId");
            message.Command = dict.Get<string>("Command");
            message.Source = dict.Get<string>("Source");

            message.DuplexType = (DuplexTypes)dict.Get<byte>("DuplexType", 0);
            message.Expiration = dict.Get<int>("Expiration", 0);
            //message.IsDuplex = dict.Get<bool>("IsDuplex", true);

            message.Creation = dict.Get<DateTime>("Creation", DateTime.Now);
            message.Args = dict.Get<NameValueArgs>("Args");
            message.TransformType = (TransformType)dict.Get<byte>("TransformType");
            //mqh-message.EncodingName = Types.NZorEmpty(dict.Get<string>("EncodingName"), DefaultEncoding);

            return message;
        }

    #endregion

    #region static

        //internal static IMessageStream Create(NetProtocol protocol,string command, string id, int expiration=0)
        //{
        //    return Create(protocol,command, id, null, null, null, expiration);
        //}
        //internal static IMessageStream Create(NetProtocol protocol, string command, string id, object value, int expiration = 0)
        //{
        //    return Create(protocol, command, id, null, value, null, expiration);
        //}
        //internal static IMessageStream Create(NetProtocol protocol, string command, string id, string label, object value, string[] args, int expiration = 0, TransformType transformType = TransformType.Object, bool isDuplex = true)
        //{
        //    if (string.IsNullOrEmpty(command))
        //        throw new ArgumentNullException("CreateMessage.command");

        //    if (expiration < 0)
        //        expiration = 0;
        //    IMessageStream message = null;
        //    switch (protocol)
        //    {
        //        case NetProtocol.Tcp:
        //            message = new TcpMessage(command, id, value, expiration);
        //            break;
        //        case NetProtocol.Pipe:
        //            message = new PipeMessage(command, id, value, expiration);
        //            break;
        //        case NetProtocol.Http:
        //            message = new HttpMessage(command, id, value, expiration);
        //            break;
        //        default:
        //            throw new ArgumentException("Protocol is not supported " + protocol.ToString());
        //    }
        //    message.IsDuplex = isDuplex;
        //    message.TransformType = transformType;
        //    if (label != null)
        //        message.Label = label;
        //    if (args != null)
        //        message.Args = MessageStream.CreateArgs(args);

        //    return message;
        //}

        internal static IMessageStream Create(NetProtocol protocol, string command, string id, int expiration = 0)
        {
            return Create(protocol, command, id, null, null, expiration);
        }
        //internal static IMessageStream Create(NetProtocol protocol, string command, string id, object value, int expiration = 0)
        //{
        //    return Create(protocol, command, id, null, value, null, expiration);
        //}


        internal static IMessageStream Create(NetProtocol protocol, string command, string id, string label, object value, int expiration = 0, string sessionId = null)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");

            if (expiration < 0)
                expiration = 0;
            IMessageStream message = null;
            switch (protocol)
            {
                case NetProtocol.Tcp:
                    message = new TcpMessage(command, id, value, expiration, sessionId);
                    break;
                case NetProtocol.Pipe:
                    message = new PipeMessage(command, id, value, expiration, sessionId);
                    break;
                case NetProtocol.Http:
                    message = new HttpMessage(command, id, value, expiration, sessionId);
                    break;
                default:
                    throw new ArgumentException("Protocol is not supported " + protocol.ToString());
            }
            //message.IsDuplex = true;// isDuplex;
            message.DuplexType = DuplexTypes.Respond;
            message.TransformType = TransformType.Object ;// transformType;
            if (label != null)
                message.Label = label;
            //if (args != null)
            //    message.Args = MessageStream.CreateArgs(args);

            return message;
        }
        /// <summary>
        /// Create instant of MessageStream
        /// </summary>
        /// <param name="command"></param>
        /// <param name="id"></param>
        /// <param name="label"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static MessageStream Create(string command, string id, string label, object value, int expiration = 0, string sessionId = null)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("CreateMessage.command");

            if (expiration < 0)
                expiration = 0;
            MessageStream message = new GenericMessage(command, id, label, value, expiration, sessionId);
            message.DuplexType = DuplexTypes.Respond;
            message.TransformType = TransformType.Object;// transformType;
            return message;
        }


        /// <summary>
        /// Create a new message stream.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public static MessageStream Create(NetProtocol protocol, Stream stream, IBinaryStreamer streamer)
        {
            MessageStream message = Factory(protocol);
            message.EntityRead(stream, streamer);
            return message;
        }

        public static MessageStream Factory(NetProtocol protocol)
        {
            switch(protocol)
            {
                case NetProtocol.Tcp:
                    return new TcpMessage();
                case NetProtocol.Pipe:
                    return new PipeMessage();
                case NetProtocol.Http:
                    return new HttpMessage();
                default:
                    throw new ArgumentException("Not supported NA Protocol");
            }
        }

        public static TransformType GetTransformType(Type type)
        {
            if (type==typeof(TransStream))
                return TransformType.Stream;
            //if (SerializeTools.IsStream(type))
            //    return TransformType.Stream;
            return TransformType.Object;
        }
    #endregion
               
    #region Read/Write pipe

        public string ReadResponseAsJson(NamedPipeClientStream stream, int ReceiveBufferSize, TransformType transformType, bool isTransStream)
        {// = 8192

            if (isTransStream)
            {
                using (TransStream ts = TransStream.CopyFrom(stream, ReceiveBufferSize))
                {
                    return ts.ReadToJson();
                }
            }

            using (TransStream ack = new TransStream(stream, ReceiveBufferSize, transformType)) //, transformType, isTransStream))
            {
                return ack.ReadToJson();
            }
        }

        public static MessageStream ReadRequest(NamedPipeServerStream pipeServer, int ReceiveBufferSize = 8192)
        {
            PipeMessage message = new PipeMessage();
            message.EntityRead(pipeServer, null);
            return message;
        }

        internal static void WriteResponse(NamedPipeServerStream pipeServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            pipeServer.Write(bResponse.ToArray(), 0, bResponse.iLength);

            pipeServer.Flush();

        }

    #endregion

    #region Read/Write tcp

        //internal static NetStream FaultStream(string faultDescription)
        //{
        //    var message = new CacheMessage("Fault", "Fault", faultDescription, 0);
        //    return message.Serialize();
        //}

        public static MessageStream ReadRequest(NetworkStream streamServer, int ReceiveBufferSize = 8192)
        {
            //var message = new TcpMessage();
            //using (var ntStream = new NetStream())
            //{
            //    ntStream.CopyFrom(streamServer, 0, ReceiveBufferSize);

            //    message.EntityRead(ntStream, null);
            //}
            //return message;

            var message = new TcpMessage();
            message.EntityRead(streamServer, null);
            return message;
        }

        internal static void WriteResponse(NetworkStream streamServer, NetStream bResponse)
        {
            if (bResponse == null)
            {
                return;
            }

            int cbResponse = bResponse.iLength;

            streamServer.Write(bResponse.ToArray(), 0, cbResponse);

            streamServer.Flush();

        }


    #endregion

    #region Read/Write http

        public static MessageStream ReadRequest(HttpRequestInfo request)
        {
            if (request.BodyStream != null)
            {
                return MessageStream.ParseStream(request.BodyStream, NetProtocol.Http);
            }
            else
            {

                var message = new HttpMessage();

                if (request.QueryString!=null)//request.BodyType == HttpBodyType.QueryString)
                    message.EntityRead(request.QueryString, null);
                else if (request.Body != null)
                    message.EntityRead(request.Body, null);
                //else if (request.Url.LocalPath != null && request.Url.LocalPath.Length > 1)
                //    message.EntityRead(request.Url.LocalPath.TrimStart('/').TrimEnd('/'), null);

                return message;
            }
        }

        internal static void WriteResponse(HttpListenerContext context, NetStream bResponse)
        {
            var response = context.Response;
            if (bResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.StatusDescription = "No response";
                return;
            }

            int cbResponse = bResponse.iLength;
            byte[] buffer = bResponse.ToArray();



            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();

        }


    #endregion

    }
#endif
}
