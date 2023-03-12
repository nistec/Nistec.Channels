using Nistec.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Channels
{
    public interface ITransformResponse : IDisposable
    {
        byte[] GetBytes();
        void SetState(int state, string message);
    }
    public interface ITransformMessage : IDisposable
    {

        /// <summary>
        /// Get or Set indicate wether the message is a duplex type.
        /// </summary>
        bool IsDuplex { get; }//{ get; set; }

        ///// <summary>
        ///// Get or Set DuplexType.
        ///// </summary>
        //DuplexTypes DuplexType { get; set; }

        ///// <summary>
        /////  Get or Set The message expiration.
        ///// </summary>
        //int Expiration { get; set; }
        /// <summary>
        /// Get or Set The result type name.
        /// </summary>
        TransformType TransformType { get; set; }
    }
}
