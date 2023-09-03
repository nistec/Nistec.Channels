using Nistec.Generic;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nistec.Channels
{
    public class TransformHeader :  ITransformMessage
    {

        /// <summary>
        /// Get Creation Time
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

        public bool IsExpired
        {
            get { return Expiration == 0 ? true : Creation.AddMinutes(Expiration) > DateTime.Now; }
        }

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
                    _DuplexType = DuplexTypes.Respond;
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

        /// <summary>
        ///  Get or Set The message expiration int minutes.
        /// </summary>
        public int Expiration { get; set; }

        public virtual TransformType TransformType { get; set; }

        #endregion
 
    }

    /*
    public class TransformHeader : ISerialEntity, ITransformMessage
    {
        internal TransformHeader(string identifier)
        {
            Creation = DateTime.Now;
            Identifier = Types.NZorEmpty(identifier, UUID.Identifier());
            IsDuplex = true;
            Version = 4022;
            //EncodingName = DefaultEncoding;
        }
        public TransformHeader() : this((string)null)
        {
        }
        public TransformHeader(Stream stream)
        {
            EntityRead(stream, null);
        }
        internal TransformHeader(TransformHeader h) : this(h.Identifier)
        {
            Creation = h.Creation;
            TransformType = h.TransformType;
            CustomId = h.CustomId;
            SessionId = h.SessionId;
            Expiration = h.Expiration;
        }

        #region property

       
        public int Version { get; internal set; }
        /// <summary>
        /// Get ItemId
        /// </summary>
        public string Identifier { get; private set; }
        /// <summary>
        /// Get Creation Time
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

        //public bool IsExpired
        //{
        //    get { return Expiration == 0 ? true : Creation.AddMinutes(Expiration) > DateTime.Now; }
        //}
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
        ///  Get or Set The message expiration int minutes.
        /// </summary>
        public int Expiration { get; set; }

        public virtual TransformType TransformType { get; set; }

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

            streamer.WriteString(Identifier);//.WriteValue(ItemId);
            streamer.WriteValue(Creation);
            streamer.WriteValue((byte)TransformType);
            streamer.WriteValue((byte)DuplexType);
            streamer.WriteString(CustomId);
            streamer.WriteString(SessionId);
            streamer.WriteValue(Expiration);
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

            Identifier = streamer.ReadString();//.ReadValue<Guid>();
            Creation = streamer.ReadValue<DateTime>();
            TransformType = (TransformType)streamer.ReadValue<byte>();
            DuplexType = (DuplexTypes)streamer.ReadValue<byte>();
            CustomId = streamer.ReadString();
            SessionId = streamer.ReadString();
            Expiration = streamer.ReadValue<int>();

        }

        #endregion

        public string Print()
        {

            return string.Format("TransformType:{0},DuplexType:{1},Expiration:{2},SessionId:{3},Creation:{4},Identifier:{5}",
            TransformType.ToString(),
            DuplexType.ToString(),
            Expiration,
            SessionId,
            Creation,
            Identifier);
        }

        public NetStream ToStream()
        {
            NetStream stream = new NetStream();
            EntityWrite(stream, null);
            return stream;
        }
        public byte[] ToBinary()
        {
            return ToStream().ToArray();
        }

        public string ToBase64()
        {
            return BinarySerializer.ToBase64(ToBinary());
        }
        public static TransformHeader FromBase64(string base64String)
        {
            return FromBinary(BinarySerializer.FromBase64(base64String));
        }

        internal static TransformHeader FromBinary(byte[] value)
        {
            return new TransformHeader(new NetStream(value));
        }
    }
    */
}
