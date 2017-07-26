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


namespace Nistec.Channels.RemoteCache
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
    /// Represent cache entity known types.
    /// </summary>
    public class KnownCacheEntityTypes
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
    /// SyncType
    /// </summary>
    public class KnownSyncType
    {
        /// <summary>
        /// No sync time
        /// </summary>
        public const string None = "None";
        /// <summary>
        /// Dal SyncType By Day 
        /// </summary>
        public const string Daily = "Daily";
        /// <summary>
        /// Dal SyncType By Interval
        /// </summary>
        public const string Interval = "Interval";
        /// <summary>
        /// Dal SyncType By Event
        /// </summary>
        public const string Event = "Event";
        /// <summary>
        /// Remove SyncType
        /// </summary>
        public const string Remove = "Remove";
    }

    /// <summary>
    /// Session states.
    /// </summary>
    public class KnownSessionState
    {
        /// <summary>
        /// Active
        /// </summary>
        public const string Active = "Active";
        /// <summary>
        /// Idle
        /// </summary>
        public const string Idle = "Idle";
        /// <summary>
        /// Timedout
        /// </summary>
        public const string Timedout = "Timedout";
    }
    /// <summary>
    /// Known Cache State
    /// </summary>
    public class KnownCacheState
    {
        /// <summary>Ok.</summary>
        public const int Ok = 0;
        /// <summary>ItemAdded.</summary>
        public const int ItemAdded = 1;
        /// <summary>ItemChanged.</summary>
        public const int ItemChanged = 2;
        /// <summary>ItemRemoved.</summary>
        public const int ItemRemoved = 3;
        /// <summary>CacheNotReady.</summary>
        public const int CacheNotReady = 501;
        /// <summary>CacheIsFull.</summary>
        public const int CacheIsFull = 502;
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
    /// Represent all cache api command.
    /// </summary>
    public class CacheCmd
    {
        /// <summary>
        /// Reply for test.
        /// </summary>
        public const string Reply = "cache_Reply";
        /// <summary>Remove item from cache.</summary>
        public const string RemoveItem = "cache_RemoveItem";
        /// <summary>Remove item from cache async.</summary>
        public const string RemoveItemAsync = "cache_RemoveItemAsync";
        /// <summary>Get item properties from cache.</summary>
        public const string ViewItem = "cache_ViewItem";
        /// <summary>Get item value and properties from cache.</summary>
        public const string GetItem = "cache_GetItem";
        /// <summary>Get value from cache.</summary>
        public const string GetValue = "cache_GetValue";
        /// <summary>Fetch value from cache.</summary>
        public const string FetchValue = "cache_FetchValue";
        /// <summary>Fetch item properties and value from cache.</summary>
        public const string FetchItem = "cache_FetchItem";
        /// <summary>Add new item to cache.</summary>
        public const string AddItem = "cache_AddItem";
        /// <summary>Keep alive item in cache.</summary>
        public const string KeepAliveItem = "cache_KeepAliveItem";

        /// <summary>Duplicate item to a new destination in cache.</summary>
        public const string CopyItem = "cache_CopyItem";
        /// <summary>Duplicate item to a new destination in cache and remove the old item.</summary>
        public const string CutItem = "cache_CutItem";
        ///// <summary>Merge item with a new collection items, this feature is valid only for items implements <see cref="System.Collections.ICollection"/> or <see cref="System.ComponentModel.IListSource"/> and return <see cref="CacheState"/>.</summary>
        //public const string MergeItem = "cache_MergeItem";
        ///// <summary>Merge item by remove all items from the current item that match the collection items in argument, this feature is valid only for items implements <see cref="System.Collections.ICollection"/> or <see cref="System.ComponentModel.IListSource"/> and return <see cref="CacheState"/>.</summary>
        //public const string MergeItemRemove = "cache_MergeItemRemove";
        /// <summary>Remove all items from cache that belong to specified session..</summary>
        public const string RemoveCacheSessionItems = "cache_RemoveCacheSessionItems";

    }

    /// <summary>
    /// Represent the cache managment command.
    /// </summary>
    public class CacheManagerCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "mang_Reply";
        /// <summary>Get cache properties.</summary>
        public const string CacheProperties = "mang_CacheProperties";
        /// <summary>Cmd.</summary>
        public const string Timeout = "mang_Timeout";
        /// <summary>Cmd.</summary>
        public const string SessionTimeout = "mang_SessionTimeout";
        /// <summary>Get list of all key items in cache.</summary>
        public const string GetAllKeys = "mang_GetAllKeys";
        /// <summary>Get list of all key items in cache.</summary>
        public const string GetAllKeysIcons = "mang_GetAllKeysIcons";
        /// <summary>Get list of all items copy in cache .</summary>
        public const string CloneItems = "mang_CloneItems";
        ///// <summary>Get statistic report.</summary>
        //public const string GetStatistic = "mang_GetStatistic";
        /// <summary>Get performance report.</summary>
        public const string GetPerformanceReport = "mang_GetPerformanceReport";
        /// <summary>Get performance report for specified agent.</summary>
        public const string GetAgentPerformanceReport = "mang_GetAgentPerformanceReport";
        /// <summary>Get list of all key items in data cache.</summary>
        public const string GetAllDataKeys = "mang_GetAllDataKeys";
        ///// <summary>Get data statistic report.</summary>
        //public const string GetDataStatistic = "mang_GetDataStatistic";
        /// <summary>Get list of all entites name items in sync cache.</summary>
        public const string GetAllSyncCacheKeys = "mang_GetAllSyncCacheKeys";
        /// <summary>Save cache to xml file.</summary>
        public const string CacheToXml = "mang_CacheToXml";
        /// <summary>Load cache from xml file.</summary>
        public const string CacheFromXml = "mang_CacheFromXml";
        /// <summary>Get cache log.</summary>
        public const string CacheLog = "mang_CacheLog";
        /// <summary>Reset cache.</summary>
        public const string Reset = "mang_Reset";
        /// <summary>Get all sessions keys.</summary>
        public const string GetAllSessionsKeys = "mang_GetAllSessionsKeys";
        /// <summary>Get all sessions keys using <see cref="Nistec.Caching.Session.SessionState"/> state.</summary>
        public const string GetAllSessionsStateKeys = "mang_GetAllSessionsStateKeys";
        /// <summary>Get all items keys from specified session.</summary>
        public const string GetSessionItemsKeys = "mang_GetSessionItemsKeys";
    }

    /// <summary>
    /// Represent known args for api commands.
    /// </summary>
    public class KnowsArgs
    {
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
        /// <summary>AddToCache.</summary>
        public const string AddToCache = "AddToCache";
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
        //public const string Timeout = "Timeout";

    }

    /// <summary>
    /// Represent data cache commands.
    /// </summary>
    public class DataCacheCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "data_Reply";
        /// <summary>Set value to specified field in row.</summary>
        public const string SetValue = "data_SetValue";
        /// <summary>Get value from specified field in row.</summary>
        public const string GetDataValue = "data_GetDataValue";
        /// <summary>Get row from specified table.</summary>
        public const string GetRow = "data_GetRow";
        /// <summary>Get data table from specified data cache.</summary>
        public const string GetDataTable = "data_GetDataTable";
        /// <summary>Remove table from data cache.</summary>
        public const string RemoveTable = "data_RemoveTable";
        /// <summary>Get item properties.</summary>
        public const string GetItemProperties = "data_GetItemProperties";

        /// <summary>Add a new data item to data cache.</summary>
        public const string AddDataItem = "data_AddDataItem";
        /// <summary>Add a new data item to data cache async.</summary>
        public const string AddDataItemSync = "data_AddDataItemSync";
        /// <summary>Add Item to SyncTables.</summary>
        public const string AddSyncDataItem = "data_AddSyncDataItem";

    }

    internal class SessionCmdDefaults
    {

        /// <summary>MSessionPipeName.</summary>
        public const string MSessionPipeName = "nistec_session";
        /// <summary>MSessionManagerPipeName.</summary>
        public const string MSessionManagerPipeName = "nistec_session_manager";
    }

    /// <summary>
    /// Represent session commands.
    /// </summary>
    public class SessionCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "sess_Reply";
        /// <summary>Add new session to sessions cache.</summary>
        public const string AddSession = "sess_AddSession";
        /// <summary>Remove session from sessions cache.</summary>
        public const string RemoveSession = "sess_RemoveSession";
        /// <summary>Clear all item from specified session.</summary>
        public const string ClearSessionItems = "sess_ClearSessionItems";
        /// <summary>Clear all sessions from session cache.</summary>
        public const string ClearAllSessions = "sess_ClearAllSessions";
        /// <summary>Get existing session, if session not exists return null.</summary>
        public const string GetExistingSession = "sess_GetExistingSession";
        /// <summary>Get existing session, if session not exists create new one.</summary>
        public const string GetOrCreateSession = "sess_GetOrCreateSession";
        /// <summary>Refresh session.</summary>
        public const string SessionRefresh = "sess_SessionRefresh";
        /// <summary>Refresh sfcific session in session cache or create a new session bag if not exists.</summary>
        public const string RefreshOrCreate = "sess_RefreshOrCreate";

        /// <summary>Remove item from specified session.</summary>
        public const string RemoveSessionItem = "sess_RemoveSessionItem";
        /// <summary>Add item to existing session, if session not exists do nothing.</summary>
        public const string AddItemExisting = "sess_AddItemExisting";
        /// <summary>Add item to session, if session not exists create new one and add the the item to session created.</summary>
        public const string AddSessionItem = "sess_AddSessionItem";
        /// <summary>Get item from specified session.</summary>
        public const string GetSessionItem = "GetSessionItem";
        /// <summary>Fetch item from specified session.</summary>
        public const string FetchSessionItem = "sess_FetchSessionItem";
        /// <summary>Copy item from specified session to a new place.</summary>
        public const string CopyTo = "sess_CopyTo";
        /// <summary>Fetch item from specified session to a new place..</summary>
        public const string FetchTo = "sess_FetchTo";
        /// <summary>Get indicate whether the session is exists.</summary>
        public const string Exists = "sess_Exists";
        /// <summary>Get all sessions keys.</summary>
        public const string GetAllSessionsKeys = "sess_GetAllSessionsKeys";
        /// <summary>Get all sessions keys using <see cref="SessionState"/> state.</summary>
        public const string GetAllSessionsStateKeys = "sess_GetAllSessionsStateKeys";
        /// <summary>Get all items keys from specified session.</summary>
        public const string GetSessionItemsKeys = "sess_GetSessionItemsKeys";
    }

    /// <summary>
    /// Represent session managment command.
    /// </summary>
    public class SessionManagerCmd
    {
        /// <summary>Log.</summary>
        public const string Log = "Log";
        /// <summary>GetAllSessionsKeys.</summary>
        public const string GetAllSessionsKeys = "GetAllSessionsKeys";
        /// <summary>GetAllSessionsStateKeys.</summary>
        public const string GetAllSessionsStateKeys = "GetAllSessionsStateKeys";
        /// <summary>ReGetAllSessionsply.</summary>
        public const string GetAllSessions = "GetAllSessions";
        /// <summary>GetActiveSessions.</summary>
        public const string GetActiveSessions = "GetActiveSessions";
        /// <summary>GetSessionsItemsKeys.</summary>
        public const string GetSessionsItemsKeys = "GetSessionsItemsKeys";
    }

    /// <summary>
    /// Represent sync cache commands.
    /// </summary>
    public class SyncCacheCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "sync_Reply";
        /// <summary>Get item from sync cache.</summary>
        public const string GetSyncItem = "sync_GetSyncItem";
        ///// <summary>Get item as <see cref="GenericEntity"/> from sync cache.</summary>
        //public const string GetSyncValue = "sync_GetSyncValue";
        /// <summary>Get item as dictionary from sync cache.</summary>
        public const string GetRecord = "sync_GetRecord";
        /// <summary>Get item as Entity from sync cache.</summary>
        public const string GetEntity = "sync_GetEntity";
        /// <summary>Get item as Entity copy using stream convert from sync cache.</summary>
        public const string GetAs = "sync_GetAs";
        /// <summary>Get indicate werher the item exists in sync cache.</summary>
        public const string Contains = "sync_Contains";
        /// <summary>Remove item from sync cache.</summary>
        public const string RemoveSyncItem = "sync_RemoveSyncItem";

        /// <summary>Refresh all items in sync cache.</summary>
        public const string Refresh = "sync_Refresh";
        /// <summary>Reset all items in sync cache.</summary>
        public const string Reset = "sync_Reset";
        /// <summary>Refresh specified item in sync cache.</summary>
        public const string RefreshItem = "sync_RefreshItem";
        /// <summary>Get all items copy for specified entity from sync cache.</summary>
        public const string GetEntityItems = "sync_GetEntityItems";
        /// <summary>Get all keys for specified entity from sync cache.</summary>
        public const string GetEntityKeys = "sync_GetEntityKeys";
        /// <summary>Get report of all items for specified entity from sync cache.</summary>
        public const string GetItemsReport = "sync_GetItemsReport";
        /// <summary>Get all entites names from sync cache.</summary>
        public const string GetAllEntityNames = "sync_GetAllEntityNames";
        /// <summary>Get all items count for specified entity from sync cache.</summary>
        public const string GetEntityItemsCount = "sync_GetEntityItemsCount";

    }
}
