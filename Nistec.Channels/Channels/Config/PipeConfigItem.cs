//licHeader
//===============================================================================================================
// System  : Nistec.Channels - Nistec.Channels Class Library
// Author  : Nissim Trujman  (nissim@nistec.net)
// Updated : 01/07/2015
// Note    : Copyright 2007-2015, Nissim Trujman, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that is part of cache core.
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

namespace Nistec.Channels.Config
{
    /// <summary>
    /// Represent pipe  <see cref="ConfigurationElement"/> item.
    /// </summary>
    public class PipeConfigItem : ConfigurationElement
    {
        /// <summary>
        /// Get pipe name.
        /// </summary>
        [ConfigurationProperty("HostName", IsRequired = true)]
        public string HostName
        {
            get
            {
                return this["HostName"] as string;
            }
        }
        /// <summary>
        /// Get server name.
        /// </summary>
        [ConfigurationProperty("ServerName", DefaultValue = "." , IsRequired = false)]
        public string ServerName
        {
            get
            {
                return this["ServerName"] as string;
            }
        }
        /// <summary>
        /// Get pipe name.
        /// </summary>
        [ConfigurationProperty("PipeName", IsRequired = true)]
        public string PipeName
        {
            get
            {
                return this["PipeName"] as string;
            }
        }
        /// <summary>
        /// Get pipe direction.
        /// </summary>
        [ConfigurationProperty("PipeDirection", DefaultValue = "InOut", IsRequired = false)]
        public string PipeDirection
        {
            get
            {
                return this["PipeDirection"] as string;
            }
        }
        /// <summary>
        /// Get pipe options.
        /// </summary>
        [ConfigurationProperty("PipeOptions", DefaultValue = "None", IsRequired = false)]
        public string PipeOptions
        {
            get
            {
                return this["PipeOptions"] as string;
            }
        }
        /// <summary>
        /// Get Verify Pipe Name.
        /// </summary>
        [ConfigurationProperty("VerifyPipe", IsRequired = false)]
        public string VerifyPipe
        {
            get
            {
                return this["VerifyPipe"] as string;
            }
        }
        /// <summary>
        /// Get connection timeout.
        /// </summary>
        [ConfigurationProperty("ConnectTimeout", DefaultValue = "5000", IsRequired = false)]
        public int ConnectTimeout
        {
            get
            {
                return Types.ToInt(this["ConnectTimeout"], 5000);
            }
        }
        /// <summary>
        /// Get process timeout.
        /// </summary>
        [ConfigurationProperty("ProcessTimeout", DefaultValue = "5000", IsRequired = false)]
        public int ProcessTimeout
        {
            get
            {
                return Types.ToInt(this["ProcessTimeout"], 5000);
            }
        }
       
        /// <summary>
        /// Get In buffer size in bytes.
        /// </summary>
        [ConfigurationProperty("ReceiveBufferSize", DefaultValue = "8192", IsRequired = false)]
        public int ReceiveBufferSize
        {
            get
            {
                return Types.ToInt(this["ReceiveBufferSize"], 8192);
            }
        }
        /// <summary>
        /// Get Out buffer size in bytes.
        /// </summary>
        [ConfigurationProperty("SendBufferSize", DefaultValue = "8192", IsRequired = false)]
        public int SendBufferSize
        {
            get
            {
                return Types.ToInt(this["SendBufferSize"], 8192);
            }
        }

        /// <summary>
        /// Get indicate if host server is async.
        /// </summary>
        [ConfigurationProperty("IsAsync", DefaultValue = true, IsRequired = false)]
        public bool IsAsync
        {
            get
            {
                return Types.ToBool(this["IsAsync"], true);
            }
        }
    }
}
