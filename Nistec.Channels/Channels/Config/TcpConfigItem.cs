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
    /// Represent tcp  <see cref="ConfigurationElement"/> item.
    /// </summary>
    public class TcpConfigItem : ConfigurationElement
    {
        /// <summary>
        /// Get host name.
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
        /// Get host address.
        /// </summary>
        [ConfigurationProperty("Address", IsRequired = true)]
        public string Address
        {
            get
            {
                return this["Address"] as string;
            }
        }
        /// <summary>
        /// Get port.
        /// </summary>
        [ConfigurationProperty("Port", IsRequired = false)]
        public int Port
        {
            get
            {
                return Types.ToInt(this["Port"]);
            }
        }

        /// <summary>
        /// Get indicate if host server is async.
        /// </summary>
        [ConfigurationProperty("IsAsync", DefaultValue = false, IsRequired = false)]
        public bool IsAsync
        {
            get
            {
                return Types.ToBool(this["IsAsync"], true);
            }
        }
       
        /// <summary>
        /// Get connection timeout.
        /// </summary>
        [ConfigurationProperty("ConnectTimeout", DefaultValue = 5000, IsRequired = false)]
        public int ConnectTimeout
        {
            get
            {
                return Types.ToInt(this["ConnectTimeout"], 5000);
            }
        }
        /// <summary>
        /// Get Process timeout.
        /// </summary>
        [ConfigurationProperty("ReceiveTimeout", DefaultValue = 5000, IsRequired = false)]
        public int ReceiveTimeout
        {
            get
            {
                return Types.ToInt(this["ReceiveTimeout"], 5000);
            }
        }
        /// <summary>
        /// Get Process timeout.
        /// </summary>
        [ConfigurationProperty("ReadTimeout", DefaultValue = 1000, IsRequired = false)]
        public int ReadTimeout
        {
            get
            {
                return Types.ToInt(this["ReadTimeout"], 0);
            }
        }
        /// <summary>
        /// Get Max Socket Error.
        /// </summary>
        [ConfigurationProperty("MaxSocketError", DefaultValue = 50, IsRequired = false)]
        public int MaxSocketError
        {
            get
            {
                return Types.ToInt(this["MaxSocketError"], 50);
            }
        }
        /// <summary>
        /// Get In buffer size in bytes.
        /// </summary>
        [ConfigurationProperty("ReceiveBufferSize", DefaultValue = 4096, IsRequired = false)]
        public int ReceiveBufferSize
        {
            get
            {
                return Types.ToInt(this["ReceiveBufferSize"], 4096);
            }
        }
        /// <summary>
        /// Get Out buffer size in bytes.
        /// </summary>
        [ConfigurationProperty("SendBufferSize", DefaultValue = 4096, IsRequired = false)]
        public int SendBufferSize
        {
            get
            {
                return Types.ToInt(this["SendBufferSize"], 4096);
            }
        }
    }
}
