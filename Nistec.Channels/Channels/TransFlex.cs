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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#pragma warning disable CS1591
namespace Nistec.Channels
{
    //public enum FlexType : byte { None = 0, Object = 100, Stream = 101, Json = 102, Base64 = 103, Text = 104, Ack = 105, State = 106, Csv = 107, Xml = 108 }//{Message=0,Stream=1,Json=2 }

    public enum FlexType : byte { Object = 100, Stream = 101, Json = 102, Base64 = 103, Text = 104, Csv = 107, Xml = 108 }

    [Serializable]
    public class TransFlex : ISerialEntity, ISerialJson, IDisposable, IDataStream
    {
        const string schema =
@"
 {
            Identifier:"",
            Label:"",
            Topic:"",
            Source:"",
            Destination:"",
            CustomId:"",
            SessionId:"",
            Expiration:"",
            Args:{"":""}
            Message:"",
            FlexType:""
        }
}";

        #region ctor

        /// <summary>
        /// Initialize a new instance of TransformMessage
        /// </summary>
        public TransFlex()
        {
            Identifier = UUID.Identifier();
            Creation = DateTime.Now;
            _Args = new NameValueArgs();
        }

        public TransFlex(FlexType flexType) : this()
        {
            FlexType = FlexType;
        }
        public TransFlex(FlexType flexType, object content) : this()
        {
            SetContent(flexType, content);
        }

        protected TransFlex(Guid itemId) : this(itemId.ToString())
        {
        }
        protected TransFlex(string identifier)
        {
            Identifier = ValidIdentifier(identifier);
            Creation = DateTime.Now;
            _Args = new NameValueArgs();
        }

        public TransFlex(TransFlex copy) : this()
        {
            Copy(copy);
        }
 
        internal void Copy(TransFlex copy)
        {
            //ItemId = copy.ItemId;
            Identifier = copy.Identifier;
            Label = copy.Label;
            CustomId = copy.CustomId;
            SessionId = copy.SessionId;
            Topic = copy.Topic;
            Source = copy.Source;
            Destination = copy.Destination;
            //IsDuplex = copy.IsDuplex;
            //DuplexType = copy.DuplexType;
            Expiration = copy.Expiration;
            Creation = copy.Creation;
            Args = copy.Args;

            InputStream = copy.InputStream;
            State = copy.State;
            TypeName = copy.TypeName;
            Message = copy.Message;
            FlexType = copy.FlexType;
        }

        public static TransFlex Get(string topic, string source, string destination)
        {
            return new TransFlex()
            {
                Topic = topic,
                Source = source,
                Destination = destination
            };
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
                Message = null;
                InputStream = null;
                Topic = null;
                Args = null;
                Identifier = null;
                CustomId = null;
                SessionId = null;
                Label = null;
            }
            disposed = true;
        }
        #endregion

        #region IDataStream

        public byte[] DataStream()
        {
            return InputStream;
        }
        public byte[] GetBytes()
        {
            return ToStream().ToArray();
        }
        public virtual object ReadBody()
        {
            if (InputStream == null)
                return null;
            //BodyStream.Position = 0;
            var ser = new BinarySerializer();
            return ser.Deserialize(new NetStream(InputStream));
        }

        //public string TypeName { get; set; }
        //string ToJson();
        public TransType TransType { get; set; }

        //public string Message { get; set; }

        public bool IsEmpty
        {
            get { return InputStream == null || InputStream.Length == 0; }
        }
        //T ReadBody<T>();
        //object GetContent();

        #endregion

        #region properties

        /// <summary>
        /// Get or Set The message Id.
        /// </summary>
        public string Identifier { get; protected set; }
        /// <summary>
        /// Get or Set The message detail.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Get or Set The message topic.
        /// </summary>
        public string Topic { get; set; }
        /// <summary>
        /// Get or Set who send the message.
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Get or Set the message Destination.
        /// </summary>
        public string Destination { get; set; }
        /// <summary>
        /// Get or Set The last time that message was modified.
        /// </summary>
        public DateTime Creation { get; protected set; }
        /// <summary>
        /// Get or Set The message CustomId.
        /// </summary>
        public string CustomId { get; set; }
        /// <summary>
        /// Get or Set The message SessionId.
        /// </summary>
        public string SessionId { get; set; }
        /// <summary>
        ///  Get or Set The message expiration int minutes.
        /// </summary>
        public int Expiration { get; set; }
        public byte[] InputStream { get; protected set; }
        public string TypeName { get; protected set; }
        public string Message { get; protected set; }
        public int State { get; set; }
        public FlexType FlexType { get; protected set; }

        //public void SetContent(object content)
        //{
        //    SetBody(content);
        //}
        //public object GetContent()
        //{
        //    return GetBody();
        //}
        //object _Value;

        //[NoSerialize]
        //public object Content { get => GetBody(); set => SetBody(value); }
        #endregion

        #region Set/Get Body

        //public byte[] GetBytes()
        //{
        //    if (InputStream == null || InputStream.Length == 0)
        //        return InputStream;
        //    //if (TypeName == typeof(byte[]).FullName)
        //    //    return BinarySerializer.Deserialize(InputStream);
        //    //if (TypeName == typeof(NetStream).FullName)
        //    //    return new NetStream(InputStream);
        //    //if (SerializeTools.IsEntityClassOrStructSerialize(SerializeTools.GetType(TypeName)))
        //    //    return BinarySerializer.Deserialize(InputStream);
        //    else
        //        return InputStream;

        //    //if (_Body == null)
        //    //    return null;
        //    //byte[] bytes = new byte[_Body.Length];
        //    //Array.Copy(_Body, bytes, _Body.Length);
        //    //return bytes;
        //}

        //public object Content
        //{
        //    get
        //    {
        //        if (FlexType == FlexType.Stream)
        //            return InputStream;
        //        return Message;
        //    }
        //}

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

        void SetBodyInternal(string typeName, string inputStream, string message)
        {
            if (FlexType == FlexType.Stream)
            {
                if (inputStream != null && inputStream.Length > 0)
                    InputStream = NetStream.FromBase64String(inputStream).ToArray();
                TypeName = typeName ?? typeof(object).FullName;
            }
            else
            {
                Message = message;
                TypeName = typeName ?? typeof(string).FullName;
            }
        }

        void SetBodyInternal(string typeName, byte[] inputStream, string message)
        {

            if (FlexType == FlexType.Stream)
            {
                InputStream = inputStream;
                TypeName = typeName ?? typeof(object).FullName;
            }
            else
            {
                Message = message;
                TypeName = typeName ?? typeof(string).FullName;
            }
        }

        /// <summary>
        /// Get content converted to object.
        /// </summary>
        /// <returns></returns>
        public object GetContent()
        {

            switch (FlexType)
            {
                case FlexType.Stream:
                    if (InputStream == null || InputStream.Length == 0)
                        return InputStream;
                    if (TypeName == typeof(byte[]).FullName)
                        return BinarySerializer.Deserialize(InputStream);
                    if (TypeName == typeof(NetStream).FullName)
                        return new NetStream(InputStream);
                    if (SerializeTools.IsEntityClassOrStructSerialize(SerializeTools.GetType(TypeName)))
                        return BinarySerializer.Deserialize(InputStream);
                    else
                        return InputStream;
                case FlexType.Csv:
                case FlexType.Text:
                    return Message;
                case FlexType.Json:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return JsonSerializer.Deserialize(Message, type);
                        else
                            return Message;
                    }
                case FlexType.Xml:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return Xml.XSerializer.Deserialize(Message, type);
                        else
                            return Message;
                    }
                case FlexType.Base64:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (SerializeTools.IsEntityClassOrStruct(type))
                            return BinarySerializer.DeserializeFromBase64(Message);
                        else
                            return Message;
                    }
                case FlexType.Object:
                    {
                        var type = SerializeTools.GetType(TypeName);
                        if (InputStream == null)
                            return null;
                        //BodyStream.Position = 0;
                        return BinarySerializer.Deserialize(InputStream);
                        //return ReadBody();
                    }
                default:
                    return this;
            }
        }
        /// <summary>
        /// Set the given value to message string, for base64 set FlexType to FlexType.Stream.
        /// </summary>
        /// <param name="flexType"></param>
        /// <param name="value"></param>
        public void SetContent(FlexType flexType, object value)
        {
            FlexType = flexType;

            if (value == null)
            {
                TypeName = typeof(object).FullName;
                InputStream = null;
                Message = null;
            }
            else
            {
                switch (flexType)
                {
                    case FlexType.Stream:
                        if (value.GetType() == typeof(byte[]))
                            InputStream = (byte[])value;
                        else if (value.GetType() == typeof(NetStream))
                            InputStream = ((NetStream)value).ToArray();
                        else if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            InputStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        Message = null;
                        break;
                    case FlexType.Csv:
                    case FlexType.Text:
                        TypeName = typeof(string).FullName;
                        Message = value.ToString();
                        InputStream = null;
                        break;
                    case FlexType.Json:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            Message = JsonSerializer.Serialize(value);
                        else
                            Message = value.ToString();
                        TypeName = value.GetType().FullName;
                        InputStream = null;
                        break;
                    case FlexType.Xml:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            Message = Xml.XSerializer.Serialize(value);
                        else
                            Message = value.ToString();
                        TypeName = value.GetType().FullName;
                        InputStream = null;
                        break;
                    case FlexType.Base64:
                        if (SerializeTools.IsEntityClassOrStructSerialize(value.GetType()))
                            Message = BinarySerializer.SerializeToBase64(value);
                        else
                            Message = value.ToString();
                        TypeName = value.GetType().FullName;
                        InputStream = null;
                        break;
                    case FlexType.Object:
                        InputStream = BinarySerializer.SerializeToBytes(value);
                        TypeName = value.GetType().FullName;
                        break;
                }
            }
        }
        #endregion

        #region validation

        public bool IsValidHeader()
        {
            return !(string.IsNullOrEmpty(Topic) || string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Destination));
        }
        public bool IsValidContent()
        {
            return !(InputStream == null && string.IsNullOrEmpty(Message));
        }

        public bool IsBodyString
        {
            get { return (TypeName == typeof(string).FullName); }
        }
        //public FlexType ValidateFlexType()
        //{
        //    if (InputStream == null)
        //        return Channels.FlexType.Message;
        //    else
        //        return Channels.FlexType.Stream;
        //}
        public virtual void ValidateBody()
        {
            if (!IsValidContent())
            {
                throw new ChannelException(ChannelState.UnprocessableContent, "TransFlex Error: Invalid Content!");
            }
        }

        public virtual void ValidateHeader()
        {
            if (!IsValidHeader())
            {
                throw new ChannelException(ChannelState.UnprocessableContent, "TransFlex Error: Invalid one or more header properties (Topic,Source,Destination)!");
            }
        }

        public virtual void ValidateMessage()
        {
            ValidateHeader();

            ValidateBody();

            /*
            if (Request == null)
            {
                throw new ArgumentException("Message is null or empty");
            }
            if (Request.Body == null || Request.Body.Value == null)
            {
                throw new ArgumentException("Message body is null or empty");
            }
            if (Request.Body.IsBodyString && string.IsNullOrEmpty(Request.Body.Message))
            {
                throw new ArgumentException("Message body string is empty");
            }
            if (!Request.Body.IsBodyString && Request.Body.InputStream == null || Request.Body.InputStream.Length == 0)
            {
                throw new ArgumentException("Message input stream is empty");
            }
            if (Request.Header == null)
            {
                throw new ArgumentException("Message header is null or empty");
            }
            if (string.IsNullOrEmpty(Request.Header.Source))
            {
                throw new ArgumentException("Message header source is null or empty");
            }
            if (string.IsNullOrEmpty(Request.Header.Destination))
            {
                throw new ArgumentException("Message header destination is null or empty");
            }
            if (string.IsNullOrEmpty(Request.Header.Topic))
            {
                throw new ArgumentException("Message header topic is null or empty");
            }
            */
        }
        #endregion

        #region methods

        public string ToJson(bool pretty = false)
        {
            return JsonSerializer.Serialize(this);
            //return GenericKeyValue.Create("Header", Header.ToString(), "State", State, "TypeName", TypeName, "Topic", Topic, "Body", ReadBody()).ToJson(pretty);
        }

        public static string ValidIdentifier(string identifier)
        {
            if (identifier == null)
                return UUID.Identifier();
            if (identifier.Length < 5 || identifier == Guid.Empty.ToString())
                return UUID.Identifier();
            return identifier;
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
            streamer.WriteString(Label);
            streamer.WriteString(CustomId);
            streamer.WriteString(SessionId);
            streamer.WriteString(Topic);
            streamer.WriteString(Source);
            streamer.WriteString(Destination);

            streamer.WriteValue(Expiration);
            streamer.WriteValue(Creation);
            streamer.WriteValue(Args);

            streamer.WriteValue((int)State);
            streamer.WriteString(TypeName);
            streamer.WriteValue(InputStream);
            streamer.WriteString(Message);
            streamer.WriteString(FlexType.ToString());

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

            Identifier = streamer.ReadString()?? Identifier;
            Label = streamer.ReadString();
            CustomId = streamer.ReadString();
            SessionId = streamer.ReadString();
            Topic = streamer.ReadString();
            Source = streamer.ReadString();
            Destination = streamer.ReadString();
            Expiration = streamer.ReadValue<int>();
            Creation = DateTime.Now;// streamer.ReadValue<DateTime>();
            Args = (NameValueArgs)streamer.ReadValue();

            State = streamer.ReadValue<int>();
            var typeName = streamer.ReadString();
            var body=(byte[])streamer.ReadValue();
            var message = streamer.ReadString();
            SetBodyInternal(typeName, body, message);

            //TypeName = streamer.ReadString();
            //InputStream = (byte[])streamer.ReadValue();
            //Message = streamer.ReadString();
            //FlexType = (FlexType)streamer.ReadEnumParser();

        }

        #endregion

        #region ISerialJson

        public virtual string EntityWrite(IJsonSerializer serializer, bool pretty = false)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Write, null);
            serializer.WriteToken("Identifier", Identifier);
            serializer.WriteToken("Label", Label, null);
            serializer.WriteToken("CustomId", CustomId, null);
            serializer.WriteToken("SessionId", SessionId, null);
            serializer.WriteToken("Topic", Topic);
            serializer.WriteToken("Source", Source);
            serializer.WriteToken("Destination", Destination);
            serializer.WriteToken("Expiration", Expiration);
            serializer.WriteToken("Creation", Creation);
            serializer.WriteToken("Args", Args);

            serializer.WriteToken("State", State);
            serializer.WriteToken("TypeName", TypeName);
            serializer.WriteToken("InputStream", InputStream == null ? null : System.Convert.ToBase64String(InputStream));
            serializer.WriteToken("Message", Message);
            serializer.WriteToken("FlexType", FlexType.ToString());

            return serializer.WriteOutput(pretty);

        }
        //protected Dictionary<string, object> JsonReader;
        public virtual object EntityRead(Dictionary<string, object> JsonReader, IJsonSerializer serializer)
        {

            if (JsonReader != null)
            {
                Identifier = JsonReader.Get<string>("Identifier")?? Identifier;
                Label = JsonReader.Get<string>("Label");
                CustomId = JsonReader.Get<string>("CustomId");
                SessionId = JsonReader.Get<string>("SessionId");
                Topic = JsonReader.Get<string>("Topic");
                Source = JsonReader.Get<string>("Source");
                Destination = JsonReader.Get<string>("Destination");
                Expiration = JsonReader.Get<int>("Expiration");
                Creation = DateTime.Now;// JsonReader.Get<DateTime>("Creation");
                Args = NameValueArgs.Convert((IDictionary<string, object>)JsonReader.Get("Args"));// dic.Get<NameValueArgs>("Args");

                State = JsonReader.Get<int>("State");
                var typeName = JsonReader.Get<string>("TypeName");
                var body = JsonReader.Get<string>("InputStream");
                var message = JsonReader.Get<string>("Message");
                SetBodyInternal(typeName, body, message);

            }
            return this;
        }
        public virtual object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = false });

            //var queryParams = new Dictionary<string, string>(HtmlPage.Document.QueryString, StringComparer.InvariantCultureIgnoreCase);

            var JsonReader = serializer.Read<Dictionary<string, object>>(json);

            if (JsonReader != null)
            {
                Identifier = JsonReader.Get<string>("Identifier") ?? Identifier;
                Label = JsonReader.Get<string>("Label");
                CustomId = JsonReader.Get<string>("CustomId");
                SessionId = JsonReader.Get<string>("SessionId");
                Topic = JsonReader.Get<string>("Topic");
                Source = JsonReader.Get<string>("Source");
                Destination = JsonReader.Get<string>("Destination");
                Expiration = JsonReader.Get<int>("Expiration");
                Creation = DateTime.Now;// JsonReader.Get<DateTime>("Creation");
                Args = NameValueArgs.Convert((IDictionary<string, object>)JsonReader.Get("Args"));// dic.Get<NameValueArgs>("Args");

                State = JsonReader.Get<int>("State");
                var typeName = JsonReader.Get<string>("TypeName");
                var body = JsonReader.Get<string>("InputStream");
                var message = JsonReader.Get<string>("Message");
                SetBodyInternal(typeName, body, message);
            }
            //JsonReader = null;
            return this;
        }

        public virtual object EntityRead(NameValueCollection queryString, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = false });

            if (queryString != null)
            {

                Identifier = queryString.Get<string>("Identifier")?? Identifier;
                Label = queryString.Get<string>("Label");
                CustomId = queryString.Get<string>("CustomId");
                SessionId = queryString.Get<string>("SessionId");
                Topic = queryString.Get<string>("Topic");
                Source = queryString.Get<string>("Source");
                Destination = queryString.Get<string>("Destination");
                Expiration = queryString.Get<int>("Expiration");
                Creation = DateTime.Now;// queryString.Get<DateTime>("Creation", DateTime.Now);
                var args = queryString.Get("Args");
                if (args != null)
                {
                    string[] nameValue = args.SplitTrim(':', ',', ';');
                    Args = NameValueArgs.Create(nameValue);
                }
                State = queryString.Get<int>("State");
                var typeName = queryString.Get<string>("TypeName");
                var body = queryString.Get<string>("InputStream");
                var message = queryString.Get<string>("Message");
                SetBodyInternal(typeName, body, message);
            }

            return this;
        }

        #endregion

        #region Encode/Decode Body
        /*
        void SetBody(string typeName, string inputStream,string message)
        {

            if (inputStream != null && inputStream.Length > 0)
            {
                InputStream = NetStream.FromBase64String(inputStream).ToArray();
                TypeName = typeName ?? typeof(object).FullName;
                FlexType = FlexType.Stream;
            }
            else
            {
                Message = message;
                TypeName = typeof(string).FullName;
                FlexType = FlexType.Message;
            }
        }
        void SetBody(string typeName, byte[] inputStream, string message)
        {

            if (inputStream != null && inputStream.Length > 0)
            {
                InputStream = inputStream;
                TypeName = typeName ?? typeof(object).FullName;
                FlexType = FlexType.Stream;
            }
            else
            {
                Message = message;
                TypeName = typeof(string).FullName;
                FlexType = FlexType.Message;
            }
        }

        /// <summary>
        /// Set the given value to body stream using <see cref="BinarySerializer"/>, This method is a part of <see cref="IMessageStream"/> implementation..
        /// </summary>
        /// <param name="value"></param>
        internal protected void SetBody(object value)
        {
            if (value == null)
            {
                TypeName = typeof(object).FullName;
                InputStream = null;
                Message = null;
                FlexType = FlexType.Message;
            }
            else if (value is byte[])
            {
                TypeName = value.GetType().FullName;
                InputStream = (byte[])value;
                FlexType = FlexType.Stream;
            }
            else if (value is NetStream)
            {
                TypeName = value.GetType().FullName;
                InputStream = ((NetStream)value).ToArray();
                FlexType = FlexType.Stream;
            }
            else if (value is string)
            {
                TypeName = value.GetType().FullName;
                InputStream = null;// BinarySerializer.SerializeToBytes(value);
                Message = value.ToString();
                FlexType = FlexType.Message;
            }
            else if (FlexType == FlexType.Json)
            {
                TypeName = value.GetType().FullName;
                Message = JsonSerializer.Serialize(value);
            }
            else
            {
                TypeName = value.GetType().FullName;
                //InputStream = BinarySerializer.SerializeToBytes(value);
                using (NetStream ns = new NetStream())
                {
                    var ser = new BinarySerializer();
                    ser.Serialize(ns, value);
                    ns.Position = 0;
                    InputStream = ns.ToArray();
                }
                FlexType = FlexType.Stream;
            }
        }

   
        
        /// <summary>
        /// Set the given byte array to body stream.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="typeName"></param>
        public void SetBody(byte[] value, string typeName)
        {
            TypeName = (!string.IsNullOrEmpty(typeName)) ? typeName : typeof(object).FullName;
            InputStream = value;
            Message = null;
            FlexType = FlexType.Stream;
        }
        /// <summary>
        /// Set the given stream to body stream.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="typeName"></param>
        /// <param name="copy"></param>
        public void SetBody(NetStream ns, string typeName, bool copy = true)
        {
            SetBody(ns.Copy(), typeName);
        }

        internal protected object GetBody()
        {
            if (TypeName == typeof(string).FullName)
                return Message;
            if (InputStream == null)
                return null;
            if (_Value != null)
                return _Value;
            _Value= BinarySerializer.Deserialize(InputStream);

            return _Value;
        }

        internal protected object GetBodyAsString()
        {
            if (TypeName == typeof(string).FullName)
                return Message;

            if (InputStream == null)
                return "";

            return BodyToBase64();
        }
        */        

        public NetStream BodyStream()
        {
            if (InputStream == null)
                return null;
            return new NetStream(InputStream);
        }

        public string BodyToBase64()
        {
            if (InputStream == null)
                return null;
            return Convert.ToBase64String(InputStream);
        }

        public virtual string BodyToString()
        {
            return string.Format("TypeName: {0}, TransType: {1}, Size: {2}", TypeName, State.ToString(), (Message == null) ? 0 : Message.Length);
        }

        public NetStream BodyToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }

        public string BodyToJson(bool pretty = false)
        {
            return GenericKeyValue.Create("State", State, "TypeName", TypeName, "FlexType", FlexType, "InputStream", BodyToBase64(), "Message", Message).ToJson(pretty);
        }

        //========================================================
        /*
        protected object SetBodyInternal(object value)
        {
            SetBody(value);
            return GetBody();
        }


        public virtual object ReadBody()
        {
            if (BodyStream() == null)
                return null;
            //BodyStream.Position = 0;
            var ser = new BinarySerializer();
            return ser.Deserialize(BodyStream());
        }

        public T ReadBody<T>()
        {
            return GenericTypes.Cast<T>(ReadBody(), true);
        }
        */
        #endregion

    }
}
