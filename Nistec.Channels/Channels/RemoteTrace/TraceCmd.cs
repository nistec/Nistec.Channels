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


namespace Nistec.Channels.RemoteTrace
{
    /// <summary>
    /// Known Entity Source Type
    /// </summary>
    public class KnownEntitySourceType
    {
        public const string Table = "Table";
        public const string Viewe = "Viewe";
        public const string Proceduree = "Proceduree";
    }
    /// <summary>
    /// Represent trace entity known types.
    /// </summary>
    public class KnownTraceEntityTypes
    {
        /// <summary>Represent GenericEntity type.</summary>
        public const string GenericEntity = "GenericEntity";
        /// <summary>Represent EntityContext type.</summary>
        public const string EntityContext = "EntityContext";
        /// <summary>Represent IDictionary type also known as GenericRecord.</summary>
        public const string IDictionary = "IDictionary";
        /// <summary>Represent BodyStream type also known as NetStream.</summary>
        public const string BodyStream = "BodyStream";
        /// <summary>Represent any entity type, mean unknown type.</summary>
        public const string AnyType = "AnyType";
    }


    /// <summary>
    /// Known Trace State
    /// </summary>
    public class KnownTraceState
    {
        /// <summary>Ok.</summary>
        public const int Ok = 0;
        /// <summary>ItemAdded.</summary>
        public const int ItemAdded = 1;
        /// <summary>ItemChanged.</summary>
        public const int ItemChanged = 2;
        /// <summary>ItemRemoved.</summary>
        public const int ItemRemoved = 3;
        /// <summary>TraceNotReady.</summary>
        public const int TraceNotReady = 501;
        /// <summary>TraceIsFull.</summary>
        public const int TraceIsFull = 502;
        /// <summary>InvalidItem.</summary>
        public const int InvalidItem = 503;
        /// <summary>InvalidSession.</summary>
        public const int InvalidSession = 504;
        /// <summary>AddItemFailed.</summary>
        public const int AddItemFailed = 505;
        /// <summary>MergeItemFailed.</summary>
        public const int MergeItemFailed = 506;
        /// <summary>CopyItemFailed.</summary>
        public const int CopyItemFailed = 507;
        /// <summary>RemoveItemFailed.</summary>
        public const int RemoveItemFailed = 508;
        /// <summary>ArgumentsError.</summary>
        public const int ArgumentsError = 509;
        /// <summary>ItemAllreadyExists.</summary>
        public const int ItemAllreadyExists = 510;
        /// <summary>SerializationError.</summary>
        public const int SerializationError = 511;
        /// <summary>UnexpectedError.</summary>
        public const int UnexpectedError = 599;
    }

    /// <summary>
    /// Represent all trace api command.
    /// </summary>
    public class TraceCmd
    {
        /// <summary>TraceLog.</summary>
        public const string TraceLog = "trace_TRACELOG";
        /// <summary>TraceAct.</summary>
        public const string TraceAct = "trace_TRACEACT";
        /// <summary>trace_SiteCounter.</summary>
        public const string SiteCounter = "trace_SiteCounter";
        /// <summary>trace_ServiceCounter.</summary>
        public const string ServiceCounter = "trace_ServiceCounter";

    }

    /// <summary>
    /// Represent the trace managment command.
    /// </summary>
    public class TraceManagerCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "mang_Reply";
        /// <summary>Get trace properties.</summary>
        public const string TraceProperties = "mang_TraceProperties";
        /// <summary>Cmd.</summary>
        public const string Timeout = "mang_Timeout";
        /// <summary>Get performance report.</summary>
        public const string GetPerformanceReport = "mang_GetPerformanceReport";
        /// <summary>Get performance report for specified agent.</summary>
        public const string GetAgentPerformanceReport = "mang_GetAgentPerformanceReport";
        /// <summary>Save trace to xml file.</summary>
        public const string TraceToXml = "mang_TraceToXml";
        /// <summary>Load trace from xml file.</summary>
        public const string TraceFromXml = "mang_TraceFromXml";
        /// <summary>Get trace log.</summary>
        public const string TraceLog = "mang_TraceLog";
        /// <summary>Reset trace.</summary>
        public const string Reset = "mang_Reset";
    }

    /// <summary>
    /// Represent known args for api commands.
    /// </summary>
    public class KnowsArgs
    {
        //public const string SessionId = "SessionId";
        /// <summary>Source.</summary>
        public const string Source = "Source";
        /// <summary>Destination.</summary>
        public const string Destination = "Destination";

        /// <summary>ConnectionKey.</summary>
        public const string ConnectionKey = "ConnectionKey";
        /// <summary>TableName.</summary>
        public const string TableName = "TableName";
        /// <summary>MappingName.</summary>
        public const string MappingName = "MappingName";
        /// <summary>SourceName.</summary>
        public const string SourceName = "SourceName";
        /// <summary>SourceType.</summary>
        public const string SourceType = "SourceType";
        /// <summary>EntityName.</summary>
        public const string EntityName = "EntityName";
        /// <summary>EntityType.</summary>
        public const string EntityType = "EntityType";
        /// <summary>Filter.</summary>
        public const string Filter = "Filter";
        /// <summary>Column.</summary>
        public const string Column = "Column";
        /// <summary>EntityKeys.</summary>
        public const string EntityKeys = "EntityKeys";
        /// <summary>UserId.</summary>
        public const string UserId = "UserId";
        /// <summary>TargetKey.</summary>
        public const string TargetKey = "TargetKey";
        /// <summary>AddToTrace.</summary>
        public const string AddToTrace = "AddToTrace";
        /// <summary>IsAsync.</summary>
        public const string IsAsync = "IsAsync";
        /// <summary>StrArgs.</summary>
        public const string StrArgs = "StrArgs";
        /// <summary>ShouldSerialized.</summary>
        public const string ShouldSerialized = "ShouldSerialized";

        /// <summary>CloneType.</summary>
        public const string CloneType = "CloneType";
        /// <summary>SyncType.</summary>
        public const string SyncType = "SyncType";
        /// <summary>SyncTime.</summary>
        public const string SyncTime = "SyncTime";

    }

}
