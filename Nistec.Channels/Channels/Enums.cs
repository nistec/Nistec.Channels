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

namespace Nistec.Channels
{
    /// <summary>
    /// TransType
    /// </summary>
    public enum TransType : byte { None = 0, Object = 100, Stream = 101, Json = 102, Base64 = 103, Text = 104, Ack = 105, State = 106, Csv = 107, Xml = 108 }
    /// <summary>
    /// StringFormatType
    /// </summary>
    public enum StringFormatType : byte { None = 0, Json = 102, Base64 = 103, Text = 104, Csv = 107, Xml = 108 }

    /// <summary>
    /// Channel Service State
    /// </summary>
    public enum ChannelServiceState { None, Started, Stoped, Paused }


    /// <summary>
    /// Net Protocol
    /// </summary>
    [Flags]
    public enum NetProtocol
    {
        NA = 0,
        Pipe = 1,
        Tcp = 2,
        Http = 4
    }

    /// <summary>
    /// Net Format
    /// </summary>
   // [Flags]
    public enum BundleFormatter
    {
        NA = 0,
        Binary = 1,
        Json = 2
    }

    /// <summary>
    /// Message Direction
    /// </summary>
    public enum MessageDirection
    {
        Request,
        Response
    }

    public enum ChannelState
    {
        None = 0,
        Ok = 200,
        Scheduled = 201,
        Received = 202,

        //Client error
        BadRequest = 400,
        Unauthorized = 401,
        Failed = 403,
        ItemNotFound = 404,
        NotAllowed = 405,
        RequestTimeout = 408,
        Unsupported = 415,
        NotEnoughCredit = 416,
        BadTargets = 417,

        //Server error
        InternalServerError = 500,
        NotImplemented = 501,
        ConnectionError = 502,
        ServiceError = 503,
        TimeoutError = 504,
        NetworkError = 505,
        ArgumentsError = 506,
        OperationError = 508,
        SerializeError = 510,
        SecurityError = 511,

        //fatal error
        FatalException = 590,
        FatalCarrierException = 591,
        FatalSchedulerException = 592,
        UnexpectedError = 599,
        Exception = -1
    }

    public enum ChannelStateSection
    {
        None,
        Ok,
        ClientError,
        ServerError,
        FatalError
    }
}
