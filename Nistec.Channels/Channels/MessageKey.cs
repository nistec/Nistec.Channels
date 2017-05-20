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
using System.Xml;
using System.Data;
using System.Runtime.Serialization;
using Nistec.Generic;

namespace Nistec.Channels
{

    /// <summary>
    /// Represent the item key info for pipe message.
    /// </summary>
    [Serializable]
    public class MessageKey
    {
        public static string GetKey(string name, params string[] keys)
        {
            return MessageKey.Get(name, keys).ToString();
        }

        public static MessageKey Get(string name, string[] keys)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("MessageKey.name");
            }
            if (keys == null || keys.Length == 0)
            {
                throw new ArgumentNullException("MessageKey.keys");
            }
            return new MessageKey() { ItemName = name, ItemKeys = keys };
        }

        public static MessageKey Get(string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                throw new ArgumentNullException("MessageKey.keys");
            }
            return new MessageKey() { ItemName = "", ItemKeys = keys };
        }

        public static MessageKey Parse(string strKeyInfo)
        {
            if (string.IsNullOrEmpty(strKeyInfo))
            {
                throw new ArgumentNullException("MessageKey.strKeyInfo");
            }
            string[] args = strKeyInfo.SplitTrim(':');
            if (args == null || args.Length < 2)
            {
                throw new ArgumentException("MessageKey.strKeyInfo is incorrect");
            }
            return new MessageKey() { ItemName = args[0], ItemKeys = args[1].SplitTrim('_') };
        }

        #region properties

        public string ItemName
        {
            get;
            set;
        }

        public string[] ItemKeys
        {
            get;
            set;
        }

        public string CacheKey
        {
            get { return ItemKeys == null ? null : string.Join("_", ItemKeys); }
        }

        public bool IsEmpty
        {
            get { return ItemKeys == null || ItemKeys.Length == 0; }
        }

        #endregion

        /// <summary>
        /// Get MessageKey as string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", ItemName, CacheKey);
        }
        /// <summary>
        /// Split key to ItemKeys.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string[] SplitKey(string key)
        {
            return key.SplitTrim('_');
        }
    }
}
