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
using System.Collections.Specialized;

namespace Nistec.Channels
{

    /// <summary>
    /// Represent the item key for messaging.
    /// </summary>
    [Serializable]
    public class ComplexQuery: ComplexKey
    {
        #region static

        public static ComplexQuery Get(string prefix, params string[] nameValueArgs)
        {
            return new ComplexQuery(prefix, nameValueArgs);
        }

        public static string GetInfo(string prefix, params string[] nameValueArgs)
        {
            return new ComplexQuery(prefix, nameValueArgs).ToString();
        }
 
        public static Dictionary<string, string> ParseQueryString(string queryString)
        {
            if (queryString == null)
            {
                throw new ArgumentNullException("queryString");

            }
            string[] t = queryString.Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> dictionary =
               t.Select(item => item.Split('=')).ToDictionary(s => s[0], s => s[1]);
            return dictionary;
        }

        public static string ToQueryString(string[] nameValueArgs)
        {
            if (nameValueArgs == null)
            {
                throw new ArgumentNullException("nameValueArgs");

            }
            int count = nameValueArgs.Length;
            if (count % 2 != 0)
            {
                throw new ArgumentException("nameValueArgs is incorrect, Not match key value arguments");
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append(nameValueArgs[i] + "=" + nameValueArgs[++i] + "&");
            }
            return (sb.Length == 0) ? "" : sb.ToString().TrimEnd('&');
        }

        #endregion

        #region ctor

        public ComplexQuery() { }
   
        public ComplexQuery(string prefix, string[] nameValueArgs)
        {
            Prefix = prefix;
            if (nameValueArgs != null)
            {
                Suffix = ToQueryString(nameValueArgs);
            }
        }

        #endregion
 
        #region parse

        public bool TryParse(out Dictionary<string, string> nameValue)
        {
            if(Suffix==null)
            {
                nameValue = null;
                return false;
            }
            string[] t = Suffix.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> dictionary =
               t.Select(item => item.Split('=')).ToDictionary(s => s[0], s => s[1]);
            nameValue = dictionary;
            return dictionary != null && dictionary.Count > 0;
        }

        #endregion
    }


    /// <summary>
    /// Represent the item key args for messaging.
    /// </summary>
    [Serializable]
    public class ComplexArgs: ComplexKey
    {

        #region static

        public static ComplexKey Get(string prefix, params string[] args)
        {
            return new ComplexArgs(prefix, args);
        }
        public static string[] ParseArgs(string args, int length, params char[] trimChars)
        {
            if (string.IsNullOrEmpty(args))
            {
                throw new ArgumentNullException("ComplexArgs.args");
            }
            string[] items = args.SplitTrim(trimChars);
            if (items.Length != length)
            {
                throw new ArgumentNullException("ComplexArgs args is out of range length: " + length.ToString());
            }
            return items;
        }
        public static string JoinKeyInfo(params string[] args)
        {
            if (args == null || args.Length == 0 || args.Length >2)
            {
                throw new ArgumentException("ComplexKey.args is incorrect");
            }
            return string.Join(Splitter, args);
        }

        #endregion

        #region ctor

        public ComplexArgs() { }

        public ComplexArgs(string prefix, string[] args)
        {
            Prefix = prefix;
            if (args != null)
            {
                Suffix = KeySet.JoinTrim(args); 
            }
        }

        #endregion

        #region parse

        public bool TryParseSuffix(out string[] values)
        {
            if (Suffix == null)
            { 
                values = null;
                return false;
            }
            string[] array = KeySet.SplitTrim(Suffix);

            values = array;
            return array != null && array.Length > 0;
        }

       
        #endregion

    }

    /// <summary>
    /// Represent the item key info for messaging.
    /// </summary>
    [Serializable]
    public class ComplexKey
    {
        public const string Splitter = "::";
        public static readonly char[] TrimChars = new char[] { '[', ']' };

        public static string[] SplitKeyInfo(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            string[] array = s.Split(new string[] { Splitter }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = array[i].Trim(TrimChars);
            }
            return array;
        }
      
        #region static

        public static ComplexKey Get(string prefix, string suffix)
        {
            return new ComplexKey() { Prefix = prefix, Suffix = suffix };
        }

        public static ComplexKey Parse(string keyInfo)
        {
            if (string.IsNullOrEmpty(keyInfo))
            {
                throw new ArgumentNullException("ComplexKey.keyInfo");
            }
            string[] args = SplitKeyInfo(keyInfo);
            if (args == null || args.Length < 2)
            {
                throw new ArgumentException("ComplexKey.keyInfo is incorrect");
            }
            return new ComplexKey() { Prefix = args[0].Trim(), Suffix = args[1].Trim() };
        }

        #endregion

        #region properties

        public string Prefix
        {
            get;
            set;
        }
        public string Suffix 
        {
            get;
            set;
        }

        public bool IsEmpty
        {
            get { return (Prefix == null || Prefix.Length == 0) && (Suffix == null || Suffix.Length == 0); }
        }

        #endregion

        #region override
        /// <summary>
        /// Get ComplexKey as string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}{1}{2}", Prefix, Splitter, Suffix);
        }
        #endregion
    }

}
