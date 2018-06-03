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
using System.Collections.Specialized;
using System.Xml;

namespace Nistec.Channels.Config
{
    /// <summary>
    /// Represent Tcp server configuration element collection.
    /// </summary>
    public class TcpServerConfigItems : ConfigurationElementCollection
    {

        /// <summary>
        /// Get or Set <see cref="TcpServerConfigItem"/> item by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TcpServerConfigItem this[int index]
        {
            get
            {
                return base.BaseGet(index) as TcpServerConfigItem;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
        /// <summary>
        /// Get or Set <see cref="TcpServerConfigItem"/> item by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new TcpServerConfigItem this[string key]
        {
            get { return (TcpServerConfigItem)BaseGet(key); }
            set
            {
                if (BaseGet(key) != null)
                {
                    BaseRemoveAt(BaseIndexOf(BaseGet(key)));
                }
                BaseAdd(value);
            }
        }
        /// <summary>
        /// Create New Element.
        /// </summary>
        /// <returns></returns>
        protected override System.Configuration.ConfigurationElement CreateNewElement()
        {
            return new TcpServerConfigItem();
        }
        /// <summary>
        /// Get Element Key
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(System.Configuration.ConfigurationElement element)
        {
            return ((TcpServerConfigItem)element).HostName;
        }
    }
}
