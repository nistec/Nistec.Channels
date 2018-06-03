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
    /// Represent pipe server config item.
    /// </summary>
    public class HttpServerConfigItem : HttpConfigItem
    {


        /// <summary>
        /// Get max server connection.
        /// </summary>
        [ConfigurationProperty("MaxServerConnections", DefaultValue = "1", IsRequired = false)]
        public int MaxServerConnections
        {
            get
            {
                return Types.ToInt(this["MaxServerConnections"], 1);
            }
        }
      

    }
}
