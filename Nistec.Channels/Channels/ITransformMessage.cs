using Nistec.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#pragma warning disable CS1591
namespace Nistec.Channels
{
    public interface INotify
    {
        void Notify(params string[] args);
    }
    public interface ITransformResponse //: IDisposable
    {
        byte[] GetBytes();
        void SetState(int state, string message);
    }
    
    public interface ITransformMessage //: IDisposable
    {

        /// <summary>
        /// Get or Set DuplexType.
        /// </summary>
        DuplexTypes DuplexType { get; set; }

        /// <summary>
        /// Get or Set The result type name.
        /// </summary>
        TransformType TransformType { get; set; }
    }
}
