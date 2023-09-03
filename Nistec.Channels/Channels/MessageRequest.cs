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
using Nistec.Serialization;
using Nistec.IO;
using System.IO;
using Nistec.Generic;
using Nistec.Runtime;
using System.Collections.Specialized;
using System.Net.Sockets;

namespace Nistec.Channels
{

    /// <summary>
    /// Represent a request message for named pipe/tcp communication.
    /// </summary>
    [Serializable]
    public class MessageRequest : ISerialEntity, ISerialJson,ITransformMessage, IDisposable
    {
        #region properties

        //public Formatters Formatter { get { return Formatters.BinarySerializer; } }
        public string Source { get; set; }
        public string Destination { get; set; }
        public GenericNameValue Request { get; set; }
        public string Command { get; set; }
        public DateTime Modified { get; set; }
        #endregion

        #region ctor

        public MessageRequest() 
        { 
            Modified = DateTime.Now;
            Request = new GenericNameValue();
            TransformType = TransformType.None;
        }

        public MessageRequest(string sender, string dest,string command,string[] keyValueMessage)
        {
            Modified = DateTime.Now;
            Source = sender;
            Destination = dest;
            Command = command;
            Request = new GenericNameValue(keyValueMessage);
            TransformType = TransformType.None;
        }

        public MessageRequest(NetStream stream)
            : this()
        {
            EntityRead(stream, null);
        }

        public MessageRequest(string sender, string dest, string command, string queryString)
        {
            Modified = DateTime.Now;
            Source = sender;
            Destination = dest;
            Command = command;
            Request = GenericNameValue.ParseQueryString(queryString);
            TransformType = TransformType.None;
        }
        public MessageRequest(Stream stream, IBinaryStreamer streamer)
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
                Command=null;
                Destination=null;
                Source = null;
                Request = null;
            }
            disposed = true;
        }
        #endregion

        #region ___ITransformMessage

        ///// <summary>
        ///// Get indicate wether the message is a duplex type.
        ///// </summary>
        //bool _IsDuplex;
        //public bool IsDuplex
        //{
        //    get { return _IsDuplex; }
        //    set
        //    {
        //        _IsDuplex = value;
        //        //if (!value)
        //        //    _DuplexType = DuplexTypes.None;
        //        //else if (_DuplexType == DuplexTypes.None)
        //        //    _DuplexType = DuplexTypes.WaitOne;
        //    }
        //}

    
        //public virtual TransformType TransformType { get; set; }

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

        #region  IEntityFormatter

        public void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteString(Command);
            streamer.WriteString(Source);
            streamer.WriteString(Destination);
            streamer.WriteValue(Modified);
            streamer.WriteValue(Request);
            streamer.WriteValue((byte)DuplexType);
            streamer.WriteValue((byte)TransformType);
            streamer.Flush();
        }

        

        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            Command = streamer.ReadString();
            Source = streamer.ReadString();
            Destination = streamer.ReadString();
            Modified = streamer.ReadValue<DateTime>();
            Request = streamer.ReadValue<GenericNameValue>();
            DuplexType = (DuplexTypes)streamer.ReadValue<byte>();
            TransformType = (TransformType)streamer.ReadValue<byte>();
        }

        #endregion

        #region ISerialJson

        public virtual string EntityWrite(IJsonSerializer serializer, bool pretty = false)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Write, null);

            serializer.WriteToken("Command", Command);
            serializer.WriteToken("Source", Source);
            serializer.WriteToken("Destination", Destination);
            serializer.WriteToken("Modified", Modified);
            serializer.WriteToken("Request", Request);
            serializer.WriteToken("DuplexType", (byte)DuplexType);
            serializer.WriteToken("TransformType", (byte)TransformType);
            return serializer.WriteOutput(pretty);

        }
        //protected Dictionary<string, object> JsonReader;
        public virtual object EntityRead(Dictionary<string, object> JsonReader, IJsonSerializer serializer)
        {

            if (JsonReader != null)
            {
                Command = JsonReader.Get<string>("Command");
                Source = JsonReader.Get<string>("Source");
                Destination = JsonReader.Get<string>("Destination");
                Modified = JsonReader.Get<DateTime>("Modified");
                Request = GenericNameValue.Convert((IDictionary<string, string>)JsonReader.Get("Request"));
                DuplexType = JsonReader.GetEnum<DuplexTypes>("DuplexType", DuplexTypes.None);
                TransformType = JsonReader.GetEnum<TransformType>("TransformType", TransformType.None);
            }
            return this;
        }
        public virtual object EntityRead(string json, IJsonSerializer serializer)
        {
            if (serializer == null)
                serializer = new JsonSerializer(JsonSerializerMode.Read, new JsonSettings() { IgnoreCaseOnDeserialize = true });

            var JsonReader = serializer.Read<Dictionary<string, object>>(json);

            if (JsonReader != null)
            {
                return EntityRead(JsonReader, serializer);
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

                Command = queryString.Get<string>("Command");
                Source = queryString.Get<string>("Source");
                Destination = queryString.Get<string>("Destination");
                Modified = queryString.Get<DateTime>("Modified");
                var args = queryString.Get("Request");
                if (args != null)
                {
                    string[] nameValue = args.SplitTrim(':', ',', ';');
                    Request = GenericNameValue.Create(nameValue);
                }
                DuplexType = queryString.GetEnum<DuplexTypes>("DuplexType", DuplexTypes.None);
                TransformType = queryString.GetEnum<TransformType>("TransformType", TransformType.None);
            }

            return this;
        }

        #endregion

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
                return GenericTypes.Cast<TResponse>(ts, true);

            }
            using (TransStream ts = new TransStream(stream, readTimeout, ReceiveBufferSize, TransformType.Stream)) //, TransReader.ToTransformType(typeof(TResponse)), false))
            {
                return ts.ReadValue<TResponse>();
            }
        }
    }
}
