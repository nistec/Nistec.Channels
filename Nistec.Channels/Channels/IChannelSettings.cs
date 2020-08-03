using Nistec.Logging;
using Nistec.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Channels
{
    /// <summary>
    /// IChannelSettings
    /// </summary>
    public interface IChannelSettings
    {
        /// <summary>
        /// Get NetProtocol
        /// </summary>
        NetProtocol Protocol { get; }
        /// <summary>
        ///  Get or Set HostName.
        /// </summary>
        string HostName { get; }
        /// <summary>
        ///  Get Host Address.
        /// </summary>
        string RawHostAddress { get; }
        ///// <summary>
        /////  Get or Set Port.
        ///// </summary>
        //int Port { get; set; }
        /// <summary>
        ///  Get or Set Indicates that the channel can be used for asynchronous reading and writing..
        /// </summary>
        bool IsAsync { get; }
        /// <summary>
        /// Get or Set MaxServerConnections (Only for server side) (Default=1).
        /// </summary>
        int MaxServerConnections { get; }
        /// <summary>
        /// Get or Set ConnectTimeout (Default=5000).
        /// </summary>
        int ConnectTimeout { get; }
        ///// <summary>
        ///// Get or Set ReadTimeout (Default=5000).
        ///// </summary>
        //int ReadTimeout { get; set; }
        /// <summary>
        /// Get or Set ReceiveBufferSize (Default=8192).
        /// </summary>
        int ReceiveBufferSize { get; }
        /// <summary>
        /// Get or Set SendBufferSize (Default=8192).
        /// </summary>
        int SendBufferSize { get; }
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        ILogger Log { get; set; }
    }
}
