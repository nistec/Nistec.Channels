using Nistec.Channels.Http;
using Nistec.Channels.Tcp;
using Nistec.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nistec.Channels
{

    public interface IMessageFlex: ITransformMessage
    {
        #region properties
        /// <summary>
        /// Get or Set The message Id.
        /// </summary>
        string Id { get; set; }
        string Message { get; set; }
        string Command { get; set; }
        string Sender { get; set; }
        string Label { get; set; }
        //public string Args { get; set; }
        string EncodingName { get; set; }
        int State { get;}

        #endregion

        #region ITransformMessage

        ///// <summary>
        ///// Get indicate wether the message is a duplex type.
        ///// </summary>
        //bool IsDuplex { get; set; }
        
        ///// <summary>
        ///// Get or Set DuplexType.
        ///// </summary>
        //DuplexTypes DuplexType { get; set; }
        ///// <summary>
        ///// Get or Set The return type name.
        ///// </summary>
        //TransformType TransformType { get; set; }
        /// <summary>
        ///  Get or Set The message expiration.
        /// </summary>
        int Expiration { get; set; }


        #endregion

        #region ReadTransStream

        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <param name="isTransStream"></param>
        /// <returns></returns>
        object ReadResponse(NetworkStream stream, int readTimeout, int ReceiveBufferSize, bool isTransStream);


        /// <summary>
        /// Convert an object of the specified type and whose value is equivalent to the specified object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        T Cast<T>(object o, bool enableException = false);


        /// <summary>
        /// Read response from server.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="stream"></param>
        /// <param name="readTimeout"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <returns></returns>
        TResponse ReadResponse<TResponse>(NetworkStream stream, int readTimeout, int ReceiveBufferSize);


        //public object ReadTransStream(NamedPipeClientStream stream, int ReceiveBufferSize = 8192)
        //{
        //    return new TransStream(stream, ReceiveBufferSize);
        //}

        object ReadResponse(NamedPipeClientStream stream, int ReceiveBufferSize, TransformType transformType, bool isTransStream);
        
        TResponse ReadResponse<TResponse>(NamedPipeClientStream stream, int ReceiveBufferSize = 8192);
        
        #endregion
    }

    

    /// <summary>
    /// MessageTransStream
    /// </summary>
    public abstract class MessageFlextStream //where T: ISerialEntity
    {
        #region properties
        ///// <summary>
        ///// Get or Set The message Id.
        ///// </summary>
        //public string Id { get; set; }
        //public string Message { get; set; }
        //public string Command { get; set; }
        //public string Sender { get; set; }
        //public string Label { get; set; }
        ////public string Args { get; set; }
        //public string EncodingName { get; set; }
        //public int State { get; internal set; }
        #endregion

        #region ITransformMessage
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
        /// <summary>
        ///  Get or Set The message expiration.
        /// </summary>
        public int Expiration { get; set; }
        */
       
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
        /// <param name="isTransStream"></param>
        /// <returns></returns>
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

    }
}
