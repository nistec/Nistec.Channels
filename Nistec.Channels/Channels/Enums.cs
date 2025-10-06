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
#pragma warning disable CS1591
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

    //See AckStatus
    public enum ChannelState
    {
        None = 0,
        //Succesfull
        Ok = 200,
        Scheduled = 201,
        Received = 202,

        BadRequest = 400,
        Unauthorized = 401,
        NotEnoughCredit = 402,//Payment Required
        Forbidden = 403,//User or Account is blocked
        ItemNotFound = 404,
        MethodNotAllowed = 405, //Method Not Allowed
        TokenVerificationExpired = 406, //Not Acceptable
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        SenderIsNotCconfirmed = 412,//Precondition Failed
        SizeToolarge = 413, //Payload Too Large
        Unsupported = 415,//Unsupported Media Type
        IpAddressNotallowed = 416,//Range Not Satisfiable
        Failed = 417,//Expectation Failed
        Qoutaexceeds = 419,
        UnprocessableContent = 422,
        TargetIsBlocked = 423,
        UpgradeRequired = 426,//Evaluationexpired
        PreconditionRequired = 428,//UserNonConfirmed
        TooManyRequests = 429,

        //Server error
        InternalServerError = 500,
        NotImplemented = 501,
        ConnectionError = 502,
        ServiceUnavailable = 503,
        TimeoutError = 504,
        NetworkError = 505,
        ArgumentError = 506,
        OperationError = 508,
        SerializeError = 510,
        SecurityError = 511,

        //Custom server error
        //InvalidContent = 550,
        //BillingEror = 551,
        //NotificationError = 552,
        //BlockedItem = 553,
        //RejectedItem = 554,
        //DuplicateKey = 555,
        //PersonalizeError = 556,
        //ParsingError = 557,
        //QueueError = 558,
        //LoadingError = 559,
        //SqlError = 560,
        //IOException = 561,

        //fatal error
        FatalException = 590,
        FatalCarrierException = 591,
        FatalSchedulerException = 592,

        UnexpectedError = 599,
        Exception = -1
    }
    /*
    public enum ChannelState
    {
        None = 0,
        Ok = 200,
        Scheduled = 201,
        Received = 202,

        //Client error
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,//NotEnoughCredit
        Forbidden = 403,//User or Account is blocked
        ItemNotFound = 404,
        NotAllowed = 405,
        TokenVerificationExpired=406,//Not Acceptable
        RequestTimeout = 408,
        PreconditionFailed =412,//Sender is not confirmed
        PayloadTooLarge=413,//Size too large
        Unsupported = 415,
        //NotEnoughCredit = 416,//Ip address not allowed
        Failed = 417,//Expectation Failed
        UnprocessableContent =422,//Invalid Content
        //Qouta exceeds = 419,
        //Target is blocked=423,
        //Invalid Price=425,
        UpgradeRequired =426,//Evaluation expired
        PreconditionRequired=428,//User Non Confirmed
        //Invalid Targets=452

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
    */
    public enum ChannelStateSection
    {
        None,
        Ok,
        ClientError,
        ServerError,
        FatalError
    }
}
