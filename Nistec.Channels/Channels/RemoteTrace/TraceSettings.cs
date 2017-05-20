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
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;
using Nistec.Generic;
using Nistec.Runtime;
using Nistec.Serialization;

namespace Nistec.Channels.RemoteTrace
{
    /// <summary>
    /// Represent the cache api settings.
    /// </summary>
    public class TraceSettings
    {
        #region common
        ///// <summary>EnableLog.</summary>
        //public readonly static bool EnableLog = false;

        /// <summary>InBufferSize.</summary>
        public const int DefaultInBufferSize = 8192;
        /// <summary>OutBufferSize.</summary>
        public const int DefaultOutBufferSize = 8192;

        /// <summary>InBufferSize.</summary>
        public static int InBufferSize
        {
            get
            {
                return NetConfig.Get<int>("InBufferSize", DefaultInBufferSize);
            }
        }
        /// <summary>OutBufferSize.</summary>
        public static int OutBufferSize
        {
            get
            {
                return NetConfig.Get<int>("OutBufferSize", DefaultOutBufferSize);
            }
        }
 
        /// <summary>
        /// Get Default Formatter
        /// </summary>
        public static Formatters DefaultFormatter { get { return Formatters.BinarySerializer; } }
        

        /// <summary>
        /// DefaultSessionTimeout
        /// </summary>
        public const int DefaultSessionTimeout = 30;
        /// <summary>
        /// Get Max Session Timeout in minutes
        /// </summary>
        public const int DefaultMaxSessionTimeout = 1440;
        /// <summary>
        /// Get Default Expiration in minutes
        /// </summary>
        public const int DefaultExpiration = 30;

        const bool DefaultIsAsync = false;

        const bool DefaultEnableException = false;

        /// <summary>IsRemoteAsync.</summary>
        public static bool IsRemoteAsync
        {
            get
            {
                return NetConfig.Get<bool>("IsRemoteAsync", DefaultIsAsync);
            }
        }

        /// <summary>EnableRemoteException.</summary>
        public static bool EnableRemoteException
        {
            get
            {
                return NetConfig.Get<bool>("EnableRemoteException", DefaultEnableException);
            }

        }
        #endregion

        #region pipe/tcp

        /// <summary>
        /// Default Trace HostName
        /// </summary>
        const string DefaultRemoteTraceHostName = "nistec_trace";
        /// <summary>
        /// Default Trace manager HostName
        /// </summary>
        const string DefaultRemoteTraceManagerHostName = "nistec_trace_manager";
        

        /// <summary>RemoteTraceHostName.</summary>
        public static string RemoteTraceHostName 
        { 
            get 
            {
                return NetConfig.NZ("RemoteTracePipeName", DefaultRemoteTraceHostName);
            } 
        }

        /// <summary>RemoteTraceManagerHostName.</summary>
        public static string RemoteTraceManagerPipeName 
        { 
            get 
            {
                return NetConfig.NZ("RemoteTraceManagerHostName", DefaultRemoteTraceManagerHostName);
            }
        }
        #endregion

        #region tcp

        /// <summary>
        /// Default Trace HostAddress
        /// </summary>
        const string DefaultRemoteTraceHostAddress = "localhost";
        
        /// <summary>
        /// Default TraceManager HostAddress
        /// </summary>
        const string DefaultRemoteTraceManagerHostAddress = "localhost";

        /// <summary>RemoteTraceHostAddress.</summary>
        public static string RemoteTraceHostAddress
        {
            get
            {
                return NetConfig.NZ("RemoteTraceHostAddress", DefaultRemoteTraceHostAddress);
            }
        }

       
        /// <summary>RemoteTraceManagerHostAddress.</summary>
        public static string RemoteTraceManagerHostAddress
        {
            get
            {
                return NetConfig.NZ("RemoteTraceManagerHostAddress", DefaultRemoteTraceManagerHostAddress);
            }
        }

        /// <summary>
        /// Default Trace Port
        /// </summary>
        public const int DefaultRemoteTracePort = 13007;
        /// <summary>
        /// Default TraceManager Port
        /// </summary>
        public const int DefaultRemoteTraceManagerPort = 13008;

        /// <summary>RemoteTracePort.</summary>
        public static int RemoteTracePort
        {
            get
            {
                return NetConfig.Get<int>("RemoteTracePort", DefaultRemoteTracePort);
            }
        }

  
        /// <summary>RemoteTraceManagerPort.</summary>
        public static int RemoteTraceManagerPort
        {
            get
            {
                return NetConfig.Get<int>("RemoteTraceManagerPort", DefaultRemoteTraceManagerPort);
            }
        }
        #endregion

    }
   
}
