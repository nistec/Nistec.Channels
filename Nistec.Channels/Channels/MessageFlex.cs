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
using Nistec.Channels.Tcp;
using Nistec.Generic;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;

namespace Nistec.Channels
{
    /// <summary>
    /// String message stream
    /// </summary>
    [Serializable]
    public class MessageFlex : MessageStream //, ITransformMessage, ITransformResponse, ISerialEntity
    {
        #region Vers
        const int Vers = -2147483300; //max=-2147483647
        //1.0=-2147483300

        int GetVersion(int version)
        {
            switch(version)
            {
                case -2147483300:
                default: return 1;
            }
        }
       
        #endregion

        #region ctor

        public MessageFlex() : base()
        {
            IsDuplex = true;
            TransformType = TransformType.Json;
            TypeName = typeof(MessageFlex).FullName;
            _IArgs = new NameValueArgs<int>();
        }
        public MessageFlex(Guid itemId) : base(itemId)
        {
            IsDuplex = true;
            TransformType = TransformType.Json;
            TypeName = typeof(MessageFlex).FullName;
            _IArgs = new NameValueArgs<int>();
        }

        public MessageFlex(MessageFlex copy) : base(copy)
        {
            TransformType = TransformType.Json;
            TypeName = typeof(MessageFlex).FullName;
            Message = copy.Message;
            Query = copy.Query;
            _IArgs = copy.IArgs;
            if (_IArgs == null)
                _IArgs = new NameValueArgs<int>();
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
            TypeName = typeof(MessageFlex).FullName;
            _IArgs = new NameValueArgs<int>();
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
            TypeName = typeof(MessageFlex).FullName;
            _IArgs = new NameValueArgs<int>();
        }

        /// <summary>
        /// Initialize a new instance of MessageFlex from stream using for <see cref="ISerialEntity"/>.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        protected MessageFlex(Stream stream, IBinaryStreamer streamer) : this()
        {
            EntityRead(stream, streamer);
        }
        //protected MessageFlex(NetworkStream stream)
        //{
        //    EntityRead(stream, null);
        //}
        //protected MessageFlex(NamedPipeServerStream stream)
        //{
        //    EntityRead(stream, null);
        //}

           
        protected MessageFlex(string json, IJsonSerializer streamer) : this()
        {
            EntityRead(json, streamer);
        }

        /// <summary>
        /// Initialize a new instance of message stream.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiration"></param>
        public MessageFlex(string command, string key, object value, int expiration) : this()
        {
            Command = command;
            //Identifier = id;
            CustomId = key;
            Expiration = expiration;
            Message= JsonSerializer.Serialize(value);
        }

        public void Load(HttpRequestInfo request, bool isDuplex = true, StringFormatType formatType = StringFormatType.Json)
        {
            Message = request.Body;
            IsDuplex = isDuplex;
            TransformType = (TransformType)(int)formatType;
        }

        #endregion

        #region Flex properties
        public string Message { get; set; }
        public string Query { get; set; }
        public int State { get; internal set; }
        #endregion

        #region IArgs
        NameValueArgs<int> _IArgs;
        /// <summary>
        /// Get or Set The header identifiers for current message.
        /// </summary>
        public NameValueArgs<int> IArgs
        {
            get { return _IArgs; }
            set
            {
                if (value == null)
                    _IArgs.Clear();
                else
                {
                    _IArgs = value;
                }
            }
        }

        public void Set(string key, int value)
        {
            IArgs.Set(key, value);
        }
        public int GetIArg(string key)
        {
            return IArgs.Get(key);
        }
        /*
        /// <summary>
        /// Create arguments helper.
        /// </summary>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        public static NameValueArgs<int> CreateIArgs(params object[] keyValues)
        {
            if (keyValues == null)
                return null;
            NameValueArgs<int> args = new NameValueArgs<int>(keyValues);
            return args;
        }
        public NameValueArgs<int> IArgsAdd(params object[] keyValues)
        {
            if (keyValues == null)
                return null;
            int count = keyValues.Length;
            if (count % 2 != 0)
            {
                throw new ArgumentException("values parameter not correct, Not match key value arguments");
            }

            if (IArgs == null)
                IArgs = new NameValueArgs<int>();

            for (int i = 0; i < count; i++)
            {
                string key = keyValues[i].ToString();
                int value = Types.ToInt(keyValues[++i]);

                if (IArgs.ContainsKey(key))
                    IArgs[key] = value;
                else
                    IArgs.Add(key, value);
            }
            return IArgs;
        }
        /// <summary>
        /// Get or create a collection of arguments.
        /// </summary>
        /// <returns></returns>
        public NameValueArgs<int> IArgsGet()
        {
            if (IArgs == null)
                return new NameValueArgs<int>();
            return IArgs;
        }

        public int IArgsGet(string name)
        {
            if (IArgs == null)
                return 0;
            return IArgs.Get(name);
        }
        public void IArgsSet(string name, int value)
        {
            IArgsGet().Add(name, value);
        }
        //public void Notify(params object[] args)
        //{
        //    AddArgs(args);
        //}
        */
        #endregion

        #region ITransformMessage


        TransformType _TransformType;
        /// <summary>
        /// Get or Set The result type name.
        /// </summary>
        public override TransformType TransformType
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

        #region  ISerialEntity


        /// <summary>
        /// Write the current object include the body and properties to stream using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public override void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);
            streamer.WriteValue(Vers);
            streamer.WriteString(Message);
            streamer.WriteString(Query);
            streamer.WriteValue((int)State);
            streamer.WriteValue(IArgs);
            base.EntityWrite(stream, streamer);

            /*
            streamer.WriteValue(Vers);
            streamer.WriteValue(ItemId);
            streamer.WriteString(Identifier);
            streamer.WriteValue(BodyStream);
            streamer.WriteString(TypeName);
            streamer.WriteValue((int)Formatter);
            streamer.WriteString(Label);
            streamer.WriteString(CustomId);
            streamer.WriteString(Command);
            streamer.WriteString(Sender);
            //streamer.WriteValue(IsDuplex);
            streamer.WriteValue((int)DuplexType);

            streamer.WriteValue(Expiration);
            streamer.WriteValue(Modified);
            streamer.WriteValue(Args);
            streamer.WriteValue((byte)TransformType);
            streamer.WriteString(EncodingName);

            streamer.WriteString(Message);
            streamer.WriteString(Query);
            streamer.WriteValue((int)State);
            streamer.WriteValue(IArgs);

            streamer.Flush();
            */
        }


        /// <summary>
        /// Read stream to the current object include the body and properties using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public override void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            int version = streamer.ReadValue<int>();
            Message = streamer.ReadString();
            Query = streamer.ReadString();
            State = streamer.ReadValue<int>();
            IArgs = (NameValueArgs<int>)streamer.ReadValue();
            base.EntityRead(stream, streamer);

            /*
            int version= streamer.ReadValue<int>();
            ItemId = streamer.ReadValue<Guid>();
            Identifier = streamer.ReadString();
            BodyStream = (NetStream)streamer.ReadValue();
            TypeName = streamer.ReadString();
            Formatter = (Formatters)streamer.ReadValue<int>();
            Label = streamer.ReadString();
            CustomId = streamer.ReadString();
            Command = streamer.ReadString();
            Sender = streamer.ReadString();
            //IsDuplex = streamer.ReadValue<bool>();
            DuplexType = (DuplexTypes)streamer.ReadValue<int>();
            Expiration = streamer.ReadValue<int>();
            Modified = streamer.ReadValue<DateTime>();
            Args = (NameValueArgs)streamer.ReadValue();
            TransformType = (TransformType)streamer.ReadValue<byte>();
            EncodingName = Types.NZorEmpty(streamer.ReadString(), DefaultEncoding);

            Message = streamer.ReadString();
            Query = streamer.ReadString();
            State = streamer.ReadValue<int>();
            IArgs = (NameValueArgs<int>)streamer.ReadValue();
            */
        }

        /// <summary>
        /// Write the current object include the body and properties to <see cref="ISerializerContext"/> using <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        public override void WriteContext(ISerializerContext context, SerializeInfo info=null)
        {
            if(info==null)
            info = new SerializeInfo();
            info.Add("Vers", Vers);
            info.Add("Message", Message);
            info.Add("Query", Query);
            info.Add("State", (int)State);
            info.Add("IArgs", IArgs);
            base.WriteContext(context, info);

            /*
            SerializeInfo info = new SerializeInfo();
            info.Add("Vers", Vers);
            info.Add("ItemId", ItemId);
            info.Add("Identifier", Identifier);
            info.Add("BodyStream", BodyStream);
            info.Add("TypeName", TypeName);
            info.Add("Formatter", (int)Formatter);
            info.Add("Label", Label);
            info.Add("CustomId", CustomId);
            info.Add("Command", Command);
            info.Add("Sender", Sender);
            //info.Add("IsDuplex", IsDuplex);
            info.Add("DuplexType", (int)DuplexType);
            info.Add("Expiration", Expiration);
            info.Add("Modified", Modified);
            info.Add("Args", Args);
            info.Add("TransformType", (byte)TransformType);
            info.Add("EncodingName", EncodingName);

            info.Add("Message", Message);
            info.Add("Query", Query);
            info.Add("State", (int)State);
            info.Add("IArgs", IArgs);
            context.WriteSerializeInfo(info);
            */
        }


        /// <summary>
        /// Read <see cref="ISerializerContext"/> context to the current object include the body and properties using <see cref="SerializeInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        public override void ReadContext(ISerializerContext context, SerializeInfo info = null)
        {
            if(info==null)
            info = context.ReadSerializeInfo();
            int version = info.GetValue<int>("Vers");
            Message = info.GetValue<string>("Message");
            Query = info.GetValue<string>("Query");
            State = info.GetValue<int>("State");
            IArgs = (NameValueArgs<int>)info.GetValue("IArgs");
            base.ReadContext(context, info);

            /*
            SerializeInfo info = context.ReadSerializeInfo();

            int version = info.GetValue<int>("Vers");
            ItemId = info.GetValue<Guid>("ItemId");
            Identifier = info.GetValue<string>("Identifier");
            BodyStream = (NetStream)info.GetValue("BodyStream");
            TypeName = info.GetValue<string>("TypeName");
            Formatter = (Formatters)info.GetValue<int>("Formatter");
            Label = info.GetValue<string>("Label");
            CustomId = info.GetValue<string>("CustomId");
            Command = info.GetValue<string>("Command");
            Sender = info.GetValue<string>("Sender");
            //IsDuplex = info.GetValue<bool>("IsDuplex");
            DuplexType = (DuplexTypes)info.GetValue<int>("DuplexType");
            Expiration = info.GetValue<int>("Expiration");
            Modified = info.GetValue<DateTime>("Modified");
            Args = (NameValueArgs)info.GetValue("Args");
            TransformType = (TransformType)info.GetValue<byte>("TransformType");
            EncodingName = Types.NZorEmpty(info.GetValue<string>("EncodingName"), DefaultEncoding);

            Message = info.GetValue<string>("Message");
            Query = info.GetValue<string>("Query");
            State = info.GetValue<int>("State");
            IArgs = (NameValueArgs<int>)info.GetValue("IArgs");
            */
        }


        #endregion

        #region ISerialJson

        public override string EntityWrite(IJsonSerializer serializer, bool pretty = false)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Write, null);

            serializer.WriteToken("Vers", Vers);
            serializer.WriteToken("Message", Message);
            serializer.WriteToken("Query", Query);
            serializer.WriteToken("State", State);
            serializer.WriteToken("IArgs", IArgs);
            return base.EntityWrite(serializer, pretty);

            /*
            object body = null;
            if (BodyStream != null)
            {
                body = BinarySerializer.ConvertFromStream(BodyStream);
            }
            serializer.WriteToken("Vers", Vers);
            serializer.WriteToken("ItemId", ItemId);
            serializer.WriteToken("Identifier", Identifier);
            serializer.WriteToken("BodyStream", BodyStream == null ? null : BodyStream.ToBase64String());
            serializer.WriteToken("TypeName", TypeName);
            serializer.WriteToken("Formatter", Formatter);
            serializer.WriteToken("Label", Label, null);
            serializer.WriteToken("CustomId", CustomId, null);
            serializer.WriteToken("Command", Command);
            serializer.WriteToken("Sender", Sender);

            serializer.WriteToken("DuplexType", (int)DuplexType);
            serializer.WriteToken("Expiration", Expiration);
            serializer.WriteToken("Modified", Modified);

            //serializer.WriteToken("IsDuplex", IsDuplex);
            serializer.WriteToken("Args", Args);
            serializer.WriteToken("TransformType", TransformType);
            serializer.WriteToken("EncodingName", EncodingName);

            if (BodyStream != null)
                serializer.WriteToken("Message", body);
            else
                serializer.WriteToken("Message", Message);

            serializer.WriteToken("Query", Query);
            serializer.WriteToken("State", State);
            serializer.WriteToken("IArgs", IArgs);

            //serializer.WriteToken("Body", body);

            return serializer.WriteOutput(pretty);
            */
        }

        public override object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            var JsonReader = serializer.Read<Dictionary<string, object>>(json);

            if (JsonReader != null)
            {
                int version = JsonReader.Get<int>("Vers");
                Message = JsonReader.Get<string>("Message");
                Query = JsonReader.Get<string>("Query");
                State = JsonReader.Get<int>("State");
                IArgs = NameValueArgs<int>.Convert((IDictionary<string, int>)JsonReader.Get("IArgs"));// dic.Get<NameValueArgs>("Args");
            };
            return base.EntityRead(JsonReader, serializer);

            /*
            //var queryParams = new Dictionary<string, string>(HtmlPage.Document.QueryString, StringComparer.InvariantCultureIgnoreCase);
            .
            var dic = serializer.Read<Dictionary<string, object>>(json);

            if (dic != null)
            {
                int version = dic.Get<int>("Vers");
                ItemId = dic.Get<Guid>("ItemId");
                Identifier = dic.Get<string>("Identifier");
                var body = dic.Get<string>("BodyStream");
                TypeName = dic.Get<string>("TypeName");
                Formatter = dic.GetEnum<Formatters>("Formatter", Formatters.BinarySerializer);
                Label = dic.Get<string>("Label");
                CustomId = dic.Get<string>("CustomId");
                Command = dic.Get<string>("Command");
                Sender = dic.Get<string>("Sender");

                DuplexType = (DuplexTypes)dic.Get<int>("DuplexType");
                Expiration = dic.Get<int>("Expiration");
                Modified = dic.Get<DateTime>("Modified");
                //IsDuplex = dic.Get<bool>("IsDuplex");
                Args = NameValueArgs.Convert((IDictionary<string, object>)dic.Get("Args"));// dic.Get<NameValueArgs>("Args");
                TransformType = (TransformType)dic.GetEnum<TransformType>("TransformType", TransformType.Object);
                EncodingName = Types.NZorEmpty(dic.Get<string>("EncodingName"), DefaultEncoding);

                Message = dic.Get<string>("Message");
                Query = dic.Get<string>("Query");
                State = dic.Get<int>("State");
                IArgs = NameValueArgs<int>.Convert((IDictionary<string, int>)dic.Get("IArgs"));// dic.Get<NameValueArgs>("Args");

                if (body != null && body.Length > 0)
                    BodyStream = NetStream.FromBase64String(body);
            }

            return this;
            */
        }

        public override object EntityRead(NameValueCollection queryString, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            if (queryString != null)
            {
                int version = queryString.Get<int>("Vers");
                Message = queryString.Get<string>("Message");
                Query = queryString.Get<string>("Query");
                State = queryString.Get<int>("State");
                var iargs = queryString.Get("IArgs");
                if (iargs != null)
                {
                    string[] nameValue = iargs.SplitTrim(':', ',', ';');
                    IArgs = NameValueArgs<int>.Create(nameValue);
                }
            }
            return base.EntityRead(queryString, serializer);

            /*
            if (queryString != null)
            {

                int version = queryString.Get<int>("Vers");
                ItemId = queryString.Get<Guid>("ItemId");
                Identifier = queryString.Get<string>("Identifier");
                var body = queryString.Get<string>("BodyStream");
                TypeName = queryString.Get<string>("TypeName");
                Formatter = queryString.GetEnum<Formatters>("Formatter", Formatters.Json);
                Label = queryString.Get<string>("Label");
                CustomId = queryString.Get<string>("CustomId");
                Command = queryString.Get<string>("Command");
                Sender = queryString.Get<string>("Sender");

                DuplexType = (DuplexTypes)queryString.Get<int>("DuplexType");
                Expiration = queryString.Get<int>("Expiration");
                Modified = queryString.Get<DateTime>("Modified", DateTime.Now);
                //IsDuplex = queryString.Get<bool>("IsDuplex");
                var args = queryString.Get("Args");
                if (args != null)
                {
                    string[] nameValue = args.SplitTrim(':', ',', ';');
                    Args = NameValueArgs.Get(nameValue);
                }
                //Args = NameValueArgs.Convert((IDictionary<string, object>)queryString.Get("Args"));//queryString.Get<NameValueArgs>("Args");
                TransformType = (TransformType)queryString.GetEnum<TransformType>("TransformType", TransformType.Object);
                EncodingName = Types.NZorEmpty(queryString.Get<string>("EncodingName"), DefaultEncoding);

                Message = queryString.Get<string>("Message");
                Query = queryString.Get<string>("Query");
                State = queryString.Get<int>("State");
                var iargs = queryString.Get("IArgs");
                if (iargs != null)
                {
                    string[] nameValue = iargs.SplitTrim(':', ',', ';');
                    IArgs = NameValueArgs<int>.Get(nameValue);
                }

                if (body != null && body.Length > 0)
                    BodyStream = NetStream.FromBase64String(body);
            }
            return this;
            */
        }

        #endregion

#if(false)

        #region properties
        /// <summary>
        /// Get or Set The message Id.
        /// </summary>
        public string Id { get; set; }
        public string Message { get; set; }
        public string Command { get; set; }
        public string Sender { get; set; }
        public string Label { get; set; }
        public string EncodingName { get; set; }
        public string Args { get; set; }
        public int State { get; internal set; }
        /// <summary>
        /// Get or Set The last time that message was modified.
        /// </summary>
        public DateTime Modified { get; set; }
        //public int Ttl { get; internal set; }
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
            streamer.WriteString(Id);
            streamer.WriteString(Message);
            streamer.WriteString(Label);
            streamer.WriteString(Command);
            streamer.WriteString(Sender);
            streamer.WriteString(Args);
            streamer.WriteValue((byte)DuplexType);
            streamer.WriteValue(Expiration);
            streamer.WriteString(EncodingName);
            streamer.WriteValue((int)State);
            //streamer.WriteValue((int)Ttl);
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
            Id = streamer.ReadString();
            Message = streamer.ReadString();
            Label = streamer.ReadString();
            Command = streamer.ReadString();
            Sender = streamer.ReadString();
            Args = streamer.ReadString();
            DuplexType = (DuplexTypes)streamer.ReadValue<byte>();
            Expiration = streamer.ReadValue<int>();
            EncodingName = Types.NZorEmpty(streamer.ReadString(), "utf-8");
            State = streamer.ReadValue<int>();
            //Ttl = streamer.ReadValue<int>();
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
            serializer.WriteToken("Id", Id);
            serializer.WriteToken("Message", Message);
            serializer.WriteToken("Label", Label, null);
            serializer.WriteToken("Command", Command);
            serializer.WriteToken("Sender", Sender);
            serializer.WriteToken("Args", Args);
            serializer.WriteToken("DuplexType", (byte)DuplexType);
            serializer.WriteToken("Expiration", Expiration);
            serializer.WriteToken("EncodingName", EncodingName);
            serializer.WriteToken("State", State);
            //serializer.WriteToken("Ttl", Ttl);
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
                Id = dic.Get<string>("Id");
                Message = dic.Get<string>("Message");
                Label = dic.Get<string>("Label");
                Command = dic.Get<string>("Command");
                Sender = dic.Get<string>("Sender");
                Args = dic.Get<string>("Args");
                DuplexType = (DuplexTypes)dic.Get<byte>("DuplexType");
                Expiration = dic.Get<int>("Expiration");
                EncodingName = Types.NZorEmpty(dic.Get<string>("EncodingName"), "utf-8");
                State = dic.Get<int>("State");
                //Ttl = dic.Get<int>("Ttl");
            }

            return this;
        }

        #endregion
#endif

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

        #region ParseQueryString

        public void ToQuery(params object[] keyValueParams)
        {
           Query= ToQueryString(keyValueParams);
        }

        public NameValueArgs ParseQuery()
        {
            return ParseQueryString(Query);
        }

        public static string ToQueryString(params object[] keyValueParams)
        {
            StringBuilder sb = new StringBuilder();

            int count = keyValueParams.Length;
            if (count % 2 != 0)
            {
                throw new ArgumentException("values parameter not correct, Not match key value arguments");
            }
            for (int i = 0; i < count; i++)
            {
                string key = keyValueParams[i].ToString();
                object value = keyValueParams[++i];
                sb.AppendFormat("{0}={1}&", key, value);
            }
            return sb.ToString().TrimEnd('&'); ;
        }

        public static string ToQueryString(NameValueArgs args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, string> entry in args)
            {
                sb.AppendFormat("{0}={1}&", entry.Key, entry.Value);
            }
            return sb.ToString().TrimEnd('&');
        }

        public static NameValueArgs ParseRawUrlQuery(string url)
        {

            if (url == null)
                url = string.Empty;

            string qs = string.Empty;

            if (url.Contains("?"))
            {
                qs = url.Substring(url.IndexOf("?") + 1);
                url = url.Substring(0, url.IndexOf("?"));
            }

            return ParseQueryString(qs);
        }

        private static string CLeanQueryString(string qs)
        {
            return qs.Replace("&amp;", "&");
        }

        public static NameValueArgs ParseQueryString(string qs, bool cleanAmp = true)
        {
            NameValueArgs dictionary = new NameValueArgs();

            if (qs == null)
                qs = string.Empty;

            string str = cleanAmp ? CLeanQueryString(qs) : qs;

            if (string.IsNullOrEmpty(str))
            {
                return dictionary;
            }
            if (!str.Contains('='))
            {
                return dictionary;
            }


            //string[] t = qs.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            //if (t.Length % 2 != 0)
            //{
            //    throw new ArgumentException("queryString is incorrect, Not match key value arguments");
            //}

            //dictionary =(NameValueArgs)
            //   t.Select(item => item.Split('=')).ToDictionary(s => s[0], s => s[1]);
            //return dictionary;


            foreach (string arg in str.Split(new char[] { '&' }))
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    string[] strArray = arg.Split(new char[] { '=' });
                    if (strArray.Length == 2)
                    {
                        string key = cleanAmp ? strArray[0] : Regx.RegexReplace("amp;", strArray[0], "");
                        dictionary[key] = strArray[1];
                    }
                    else
                    {
                        dictionary[arg] = null;
                    }
                }
            }

            return dictionary;
        }

        #endregion

        #region Read/Write pipe

        //public string ReadResponseAsJson(NamedPipeClientStream stream, int ReceiveBufferSize, TransformType transformType, bool isTransStream)
        //{// = 8192

        //    if (isTransStream)
        //    {
        //        using (TransStream ts = TransStream.CopyFrom(stream, ReceiveBufferSize))
        //        {
        //            return ts.ReadJson();
        //        }
        //    }

        //    using (TransStream ack = new TransStream(stream, ReceiveBufferSize, transformType)) //, transformType, isTransStream))
        //    {
        //        return ack.ReadJson();
        //    }
        //}

        public static MessageFlex ReadRequest(NamedPipeServerStream pipeServer, int ReceiveBufferSize = 8192)
        {
            MessageFlex message = new MessageFlex();
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

        public static MessageFlex ReadRequest(NetworkStream streamServer, int ReceiveBufferSize = 8192)
        {
            MessageFlex message = new MessageFlex();
            message.EntityRead(streamServer, null);
            return message;
        }

        //internal static void WriteResponse(NetworkStream streamServer, NetStream bResponse)
        //{
        //    if (bResponse == null)
        //    {
        //        return;
        //    }

        //    int cbResponse = bResponse.iLength;

        //    streamServer.Write(bResponse.ToArray(), 0, cbResponse);

        //    streamServer.Flush();

        //}


        #endregion

        #region Read/Write http

        public static MessageFlex ReadRequest(HttpRequestInfo request)
        {
            MessageFlex message = new MessageFlex(request);
            return message;

            //if (request.BodyStream != null)
            //{
            //    return MessageStream.ParseStream(request.BodyStream, NetProtocol.Http);
            //}
            //else
            //{

            //    var message = new HttpMessage();

            //    if (request.QueryString != null)//request.BodyType == HttpBodyType.QueryString)
            //        message.EntityRead(request.QueryString, null);
            //    else if (request.Body != null)
            //        message.EntityRead(request.Body, null);
            //    //else if (request.Url.LocalPath != null && request.Url.LocalPath.Length > 1)
            //    //    message.EntityRead(request.Url.LocalPath.TrimStart('/').TrimEnd('/'), null);

            //    return message;
            //}
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
}
