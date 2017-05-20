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
using Nistec.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Channels
{

    /// <summary>
    /// The exception that is thrown when a non-fatal application error occurs.
    /// </summary>
    public class MessageException : ApplicationException
    {
        /// <summary>
        /// Get state.
        /// </summary>
        public MessageState State { get; private set; }
        /// <summary>
        /// Initializes a new instance of the MessageException class.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        public MessageException(MessageState state, string message)
            : base(message)
        {
            State = state;
        }
        /// <summary>
        /// Initializes a new instance of the MessageException class.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MessageException(MessageState state, string message, Exception innerException)
            : base(message, innerException)
        {
            State = state;
        }
        /// <summary>
        /// Initializes a new instance of the MessageException class.
        /// </summary>
        /// <param name="ack"></param>
        public MessageException(AckStream ack)
            : base(ack.Message)
        {
            State = ack.State;
        }
        

    }
}
