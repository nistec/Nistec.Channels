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

    #region Known args

    [Serializable]
    public enum RemoteCacheState
    {
        /// <summary>Ok.</summary>
        Ok = 0,
        /// <summary>ItemAdded.</summary>
        ItemAdded = 1,
        /// <summary>ItemChanged.</summary>
        ItemChanged = 2,
        /// <summary>ItemRemoved.</summary>
        ItemRemoved = 3,
        /// <summary>ItemNotFount.</summary>
        NotFound = 100,
        /// <summary>CacheNotReady.</summary>
        CacheNotReady = 501,
        /// <summary>CacheIsFull.</summary>
        CacheIsFull = 502,
        /// <summary>InvalidItem.</summary>
        InvalidItem = 503,
        /// <summary>InvalidSession.</summary>
        InvalidSession = 504,
        /// <summary>AddItemFailed.</summary>
        AddItemFailed = 505,
        /// <summary>MergeItemFailed.</summary>
        MergeItemFailed = 506,
        /// <summary>CopyItemFailed.</summary>
        CopyItemFailed = 507,
        /// <summary>RemoveItemFailed.</summary>
        RemoveItemFailed = 508,
        /// <summary>ArgumentsError.</summary>
        ArgumentsError = 509,
        /// <summary>ItemAllreadyExists.</summary>
        ItemAllreadyExists = 510,
        /// <summary>SerializationError.</summary>
        SerializationError = 511,
        /// <summary>CommandNotSupported.</summary>
        CommandNotSupported = 512,
        /// <summary>UnexpectedError.</summary>
        UnexpectedError = 599
    }

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
        /// <summary>Primary key.</summary>
        public const string Pk = "Pk";
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

    #endregion

    #region commands
    /// <summary>
    /// Represent all cache api command.
    /// </summary>
    public class CacheCmd
    {
        /// <summary>
        /// Reply for test.
        /// </summary>
        public const string Reply = "cach_reply";
        /// <summary>Remove item from cache.</summary>
        public const string Remove = "cach_remove";
        /// <summary>Remove item from cache async.</summary>
        public const string RemoveAsync = "cach_removeasync";
        /// <summary>Get item properties from cache.</summary>
        public const string ViewEntry = "cach_viewentry";
        /// <summary>Get item value and properties from cache.</summary>
        public const string GetEntry = "cach_getentry";
        /// <summary>Get value from cache.</summary>
        public const string GetRecord = "cach_getrecord";
        /// <summary>Get value from cache.</summary>
        public const string Get = "cach_get";
        /// <summary>Fetch value from cache.</summary>
        public const string Fetch = "cach_fetch";
        /// <summary>Fetch item properties and value from cache.</summary>
        public const string FetchEntry = "cach_fetchentry";
        /// <summary>Add new item to cache.</summary>
        public const string Add = "cach_add";
        /// <summary>Add new item to cache.</summary>
        public const string Set = "cach_set";
        /// <summary>Keep alive item in cache.</summary>
        public const string KeepAliveItem = "cach_keepaliveitem";
        /// <summary>Load data item to or from cache.</summary>
        public const string LoadData = "cach_loaddata";

        /// <summary>Duplicate item to a new destination in cache.</summary>
        public const string CopyTo = "cach_copyto";
        /// <summary>Duplicate item to a new destination in cache and remove the old item.</summary>
        public const string CutTo = "cach_cutto";
        ///// <summary>Merge item with a new collection items, this feature is valid only for items implements <see cref="System.Collections.ICollection"/> or <see cref="System.ComponentModel.IListSource"/> and return <see cref="CacheState"/>.</summary>
        //public const string MergeItem = "cach_MergeItem";
        ///// <summary>Merge item by remove all items from the current item that match the collection items in argument, this feature is valid only for items implements <see cref="System.Collections.ICollection"/> or <see cref="System.ComponentModel.IListSource"/> and return <see cref="CacheState"/>.</summary>
        //public const string MergeItemRemove = "cach_MergeItemRemove";
        /// <summary>Remove all items from cache that belong to specified session..</summary>
        public const string RemoveItemsBySession = "cach_removeitemsbysession";
    }

    /// <summary>
    /// Represent data cache commands.
    /// </summary>
    public class DataCacheCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "data_reply";
        /// <summary>Set value to specified field in row.</summary>
        public const string Set = "data_setvalue";
        /// <summary>Add value to specified field in row.</summary>
        public const string Add = "data_addvalue";
        /// <summary>Get value from specified field in row.</summary>
        public const string Get = "data_value";
        /// <summary>Get row from specified table.</summary>
        public const string GetRecord = "data_getrecord";
        /// <summary>Get row stream from specified table.</summary>
        public const string GetStream = "data_getstream";

        ///// <summary>Get data cache statistic.</summary>
        //public const string GetDataStatistic = "data_GetDataStatistic";

        /// <summary>Add a new data item to data cache.</summary>
        public const string AddTable = "data_addtable";
        /// <summary>Add a new data item to data cache with sync.</summary>
        public const string AddTableWithSync = "data_addtablewithsync";

        /// <summary>Set a new data item with override to data cache.</summary>
        public const string SetTable = "data_settable";
        /// <summary>Get data table from specified data cache.</summary>
        public const string GetTable = "data_gettable";
        /// <summary>Remove table from data cache.</summary>
        public const string RemoveTable = "data_removetable";

        ///// <summary>Add a new data item to data cache async.</summary>
        //public const string AddDataItemSync = "data_AddDataItemSync";
        /// <summary>Add Item to SyncTables.</summary>
        public const string AddSyncItem = "data_addsyncitem";
        /// <summary>Get item properties.</summary>
        public const string GetItemProperties = "data_getitemproperties";
        /// <summary>Refresh specified table in db cache.</summary>
        public const string Refresh = "data_refresh";
        /// <summary>Restart all items in db cache.</summary>
        public const string Reset = "data_reset";
        /// <summary>Get indicate werher the table exists in db cache.</summary>
        public const string Contains = "data_contains";

        /// <summary>Get all items copy for specified entity from sync cache.</summary>
        public const string GetEntityItems = "data_getentityitems";
        /// <summary>Get all keys for specified entity from sync cache.</summary>
        public const string GetEntityKeys = "data_getentitykeys";
        /// <summary>Get report of all items for specified entity from sync cache.</summary>
        public const string GetItemsReport = "data_getitemsreport";
        /// <summary>Get all entites names from sync cache.</summary>
        public const string GetAllEntityNames = "data_getallentitynames";

        /// <summary>Get all items count for specified entity from sync cache.</summary>
        public const string GetEntityItemsCount = "data_getentityitemscount";


    }

    /// <summary>
    /// Represent session commands.
    /// </summary>
    public class SessionCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "sess_reply";
        /// <summary>Add new session to sessions cache.</summary>
        public const string CreateSession = "sess_createsession";
        /// <summary>Remove session from sessions cache.</summary>
        public const string RemoveSession = "sess_removesession";
        /// <summary>Clear all item from specified session.</summary>
        public const string ClearItems = "sess_clearitems";
        /// <summary>Clear all sessions from session cache.</summary>
        public const string ClearAll = "sess_clearall";
        ///// <summary>Get existing session, if session not exists return null.</summary>
        //public const string GetSessionStream = "sess_GetSessionStream";

        ///// <summary>Get existing session, if session not exists return null.</summary>
        //public const string GetExistingSession = "sess_GetExistingSession";
        ///// <summary>Get existing session, if session not exists return null.</summary>
        //public const string GetExistingSessionRecord = "sess_GetExistingSessionRecord";

        /// <summary>Get session items.</summary>
        public const string GetSessionItems = "sess_getsessionitems";

        /// <summary>Get existing session, if session not exists create new one.</summary>
        public const string GetOrCreateSession = "sess_getorcreatesession";
        /// <summary>Get existing session, if session not exists create new one.</summary>
        public const string GetOrCreateRecord = "sess_getorcreaterecord";
        /// <summary>Refresh session.</summary>
        public const string Refresh = "sess_refresh";
        /// <summary>Refresh sfcific session in session cache or create a new session bag if not exists.</summary>
        public const string RefreshOrCreate = "sess_refreshorcreate";

        /// <summary>Remove item from specified session.</summary>
        public const string Remove = "sess_remove";
        /// <summary>Add item to existing session, if session not exists do nothing.</summary>
        public const string Add = "sess_add";
        /// <summary>Add item to session, if session not exists create new one and add the the item to session created.</summary>
        public const string Set = "sess_set";
        /// <summary>Get item from specified session.</summary>
        public const string GetEntry = "sess_getentry";
        /// <summary>Get value from specified session.</summary>
        public const string Get = "sess_get";
        /// <summary>Get item from specified session.</summary>
        public const string GetRecord = "sess_getrecord";
        /// <summary>Fetch item from specified session.</summary>
        public const string Fetch = "sess_fetch";
        /// <summary>Fetch item from specified session.</summary>
        public const string FetchRecord = "sess_fetchrecord";
        /// <summary>Copy item from specified session to a new place.</summary>
        public const string CopyTo = "sess_copyto";
        /// <summary>Cut item from specified session to a new place..</summary>
        public const string CutTo = "sess_cutto";
        /// <summary>Get indicate whether the session is exists.</summary>
        public const string Exists = "sess_exists";

        /// <summary>Get all sessions keys.</summary>
        public const string ViewAllSessionsKeys = "sess_viewallsessionskeys";
        /// <summary>Get all sessions keys using SessionState state.</summary>
        public const string ViewAllSessionsKeysByState = "sess_viewallsessionskeysbystate";
        /// <summary>Get all items keys from specified session.</summary>
        public const string ViewSessionKeys = "sess_viewsessionkeys";
        /// <summary>Get existing session, if session not exists return null.</summary>
        public const string ViewSessionStream = "sess_viewtsessionstream";

        /// <summary>View item from specified session.</summary>
        public const string ViewEntry = "sess_viewentry";
    }

    /// <summary>
    /// Represent sync cache commands.
    /// </summary>
    public class SyncCacheCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "sync_reply";
        /// <summary>Get item from sync cache.</summary>
        public const string Get = "sync_get";
        /// <summary>Get item as dictionary from sync cache.</summary>
        public const string GetRecord = "sync_getrecord";
        /// <summary>Get item as Entity from sync cache.</summary>
        public const string GetEntity = "sync_getentity";
        /// <summary>Get item as Entity copy using stream convert from sync cache.</summary>
        public const string GetAs = "sync_getas";
        /// <summary>Get indicate werher the item exists in sync cache.</summary>
        public const string Contains = "sync_contains";
        /// <summary>Add new item to sync cache.</summary>
        public const string AddSyncItem = "sync_addsyncitem";
        /// <summary>Add new entity to sync cache.</summary>
        public const string AddEntity = "sync_addentity";
        /// <summary>Remove item from sync cache.</summary>
        public const string Remove = "sync_remove";

        ///// <summary>Get item as entity stream from sync cache.</summary>
        //public const string GetEntityStream = "sync_GetEntityStream";

        /// <summary>Refresh all items in sync cache.</summary>
        public const string RefreshAll = "sync_refreshall";
        /// <summary>Refresh specified item in sync cache.</summary>
        public const string Refresh = "sync_refresh";
        /// <summary>Get all items copy for specified entity from sync cache.</summary>
        public const string GetEntityItems = "sync_getentityitems";
        /// <summary>Get all keys for specified entity from sync cache.</summary>
        public const string GetEntityKeys = "sync_getentitykeys";
        /// <summary>Get report of all items for specified entity from sync cache.</summary>
        public const string GetItemsReport = "sync_getitemsreport";
        /// <summary>Get all entites names from sync cache.</summary>
        public const string GetAllEntityNames = "sync_getallentitynames";
        /// <summary>Restart all items in sync cache.</summary>
        public const string Reset = "sync_reset";
        /// <summary>Get all items count for specified entity from sync cache.</summary>
        public const string GetEntityItemsCount = "sync_getentityitemscount";
        /// <summary>Get item properties.</summary>
        public const string GetItemProperties = "sync_getitemproperties";

        ///// <summary>Get sync cache statistic.</summary>
        //public const string GetSyncStatistic = "sync_GetSyncStatistic";

        /// <summary>Get item EntityPrimaryKey.</summary>
        public const string GetEntityPrimaryKey = "sync_getentityprimarykey";
        /// <summary>Find entity.</summary>
        public const string FindEntity = "sync_findentity";

    }

    #endregion

    #region commands
#if(false)
    /// <summary>
    /// Represent all cache api command.
    /// </summary>
    public class CacheCmd
    {
        /// <summary>
        /// Reply for test.
        /// </summary>
        public const string Reply = "cach_Reply";
        /// <summary>Remove item from cache.</summary>
        public const string Remove = "cach_Remove";
        /// <summary>Remove item from cache async.</summary>
        public const string RemoveAsync = "cach_RemoveAsync";
        /// <summary>Get item properties from cache.</summary>
        public const string ViewEntry = "cach_ViewEntry";
        /// <summary>Get item value and properties from cache.</summary>
        public const string GetEntry = "cach_GetEntry";
        /// <summary>Get value from cache.</summary>
        public const string GetRecord = "cach_GetRecord";
        /// <summary>Get value from cache.</summary>
        public const string Get = "cach_Get";
        /// <summary>Fetch value from cache.</summary>
        public const string Fetch = "cach_Fetch";
        /// <summary>Fetch item properties and value from cache.</summary>
        public const string FetchEntry = "cach_FetchEntry";
        /// <summary>Add new item to cache.</summary>
        public const string Add = "cach_Add";
        /// <summary>Add new item to cache.</summary>
        public const string Set = "cach_Set";
        /// <summary>Keep alive item in cache.</summary>
        public const string KeepAliveItem = "cach_KeepAliveItem";
        /// <summary>Load data item to or from cache.</summary>
        public const string LoadData = "cach_LoadData";

        /// <summary>Duplicate item to a new destination in cache.</summary>
        public const string CopyTo = "cach_CopyTo";
        /// <summary>Duplicate item to a new destination in cache and remove the old item.</summary>
        public const string CutTo = "cach_CutTo";
        ///// <summary>Merge item with a new collection items, this feature is valid only for items implements <see cref="System.Collections.ICollection"/> or <see cref="System.ComponentModel.IListSource"/> and return <see cref="CacheState"/>.</summary>
        //public const string MergeItem = "cach_MergeItem";
        ///// <summary>Merge item by remove all items from the current item that match the collection items in argument, this feature is valid only for items implements <see cref="System.Collections.ICollection"/> or <see cref="System.ComponentModel.IListSource"/> and return <see cref="CacheState"/>.</summary>
        //public const string MergeItemRemove = "cach_MergeItemRemove";
        /// <summary>Remove all items from cache that belong to specified session..</summary>
        public const string RemoveItemsBySession = "cach_RemoveItemsBySession";
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
    /// Represent data cache commands.
    /// </summary>
    public class DataCacheCmd
    {
        /// <summary>Reply.</summary>
        public const string Reply = "data_Reply";
        /// <summary>Set value to specified field in row.</summary>
        public const string Set = "data_SetValue";
        /// <summary>Add value to specified field in row.</summary>
        public const string Add = "data_AddValue";
        /// <summary>Get value from specified field in row.</summary>
        public const string Get = "data_Value";
        /// <summary>Get row from specified table.</summary>
        public const string GetRecord = "data_GetRecord";
        /// <summary>Get row stream from specified table.</summary>
        public const string GetStream = "data_GetStream";

        /// <summary>Add a new data item to data cache.</summary>
        public const string AddTable = "data_AddTable";
        /// <summary>Add a new data item to data cache with sync.</summary>
        public const string AddTableWithSync = "data_AddTableWithSync";

        /// <summary>Set a new data item with override to data cache.</summary>
        public const string SetTable = "data_SetTable";
        /// <summary>Get data table from specified data cache.</summary>
        public const string GetTable = "data_GetTable";
        /// <summary>Remove table from data cache.</summary>
        public const string RemoveTable = "data_RemoveTable";

        /// <summary>Add Item to SyncTables.</summary>
        public const string AddSyncItem = "data_AddSyncItem";
        /// <summary>Get item properties.</summary>
        public const string GetDataItem = "data_GetDataItem";
        /// <summary>Refresh specified table in db cache.</summary>
        public const string Refresh = "data_Refresh";
        /// <summary>Restart all items in db cache.</summary>
        public const string Reset = "data_Reset";
        /// <summary>Get indicate werher the table exists in db cache.</summary>
        public const string Contains = "data_Contains";

        /// <summary>Get all items copy for specified entity from sync cache.</summary>
        public const string GetEntityItems = "data_GetEntityItems";
        /// <summary>Get all keys for specified entity from sync cache.</summary>
        public const string GetEntityKeys = "data_GetEntityKeys";
        /// <summary>Get report of all items for specified entity from sync cache.</summary>
        public const string GetItemsReport = "data_GetItemsReport";
        /// <summary>Get all entites names from sync cache.</summary>
        public const string GetAllEntityNames = "data_GetAllEntityNames";

        /// <summary>Get all items count for specified entity from sync cache.</summary>
        public const string GetEntityItemsCount = "data_GetEntityItemsCount";
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
        public const string CreateSession = "sess_CreateSession";
        /// <summary>Remove session from sessions cache.</summary>
        public const string RemoveSession = "sess_RemoveSession";
        /// <summary>Clear all item from specified session.</summary>
        public const string ClearItems = "sess_ClearItems";
        /// <summary>Clear all sessions from session cache.</summary>
        public const string ClearAll = "sess_ClearAll";
        ///// <summary>Get existing session, if session not exists return null.</summary>
        //public const string GetSessionStream = "sess_GetSessionStream";

        ///// <summary>Get existing session, if session not exists return null.</summary>
        //public const string GetExistingSession = "sess_GetExistingSession";
        ///// <summary>Get existing session, if session not exists return null.</summary>
        //public const string GetExistingSessionRecord = "sess_GetExistingSessionRecord";

        /// <summary>Get session items.</summary>
        public const string GetSessionItems = "sess_GetSessionItems";

        /// <summary>Get existing session, if session not exists create new one.</summary>
        public const string GetOrCreateSession = "sess_GetOrCreateSession";
        /// <summary>Get existing session, if session not exists create new one.</summary>
        public const string GetOrCreateRecord = "sess_GetOrCreateRecord";
        /// <summary>Refresh session.</summary>
        public const string Refresh = "sess_Refresh";
        /// <summary>Refresh sfcific session in session cache or create a new session bag if not exists.</summary>
        public const string RefreshOrCreate = "sess_RefreshOrCreate";

        /// <summary>Remove item from specified session.</summary>
        public const string Remove = "sess_Remove";
        /// <summary>Add item to existing session, if session not exists do nothing.</summary>
        public const string Add = "sess_Add";
        /// <summary>Add item to session, if session not exists create new one and add the the item to session created.</summary>
        public const string Set = "sess_Set";
        /// <summary>Get item from specified session.</summary>
        public const string GetEntry = "sess_GetEntry";
        /// <summary>Get value from specified session.</summary>
        public const string Get = "sess_Get";
        /// <summary>Get item from specified session.</summary>
        public const string GetRecord = "sess_GetRecord";
        /// <summary>Fetch item from specified session.</summary>
        public const string Fetch = "sess_Fetch";
        /// <summary>Fetch item from specified session.</summary>
        public const string FetchRecord = "sess_FetchRecord";
        /// <summary>Copy item from specified session to a new place.</summary>
        public const string CopyTo = "sess_CopyTo";
        /// <summary>Cut item from specified session to a new place..</summary>
        public const string CutTo = "sess_CutTo";
        /// <summary>Get indicate whether the session is exists.</summary>
        public const string Exists = "sess_Exists";

        /// <summary>Get all sessions keys.</summary>
        public const string ViewAllSessionsKeys = "sess_ViewAllSessionsKeys";
        /// <summary>Get all sessions keys using SessionState state.</summary>
        public const string ViewAllSessionsKeysByState = "sess_ViewAllSessionsKeysByState";
        /// <summary>Get all items keys from specified session.</summary>
        public const string ViewSessionKeys = "sess_ViewSessionKeys";
        /// <summary>Get existing session, if session not exists return null.</summary>
        public const string ViewSessionStream = "sess_ViewtSessionStream";

        /// <summary>View item from specified session.</summary>
        public const string ViewEntry = "sess_ViewEntry";
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
        public const string Get = "sync_Get";
        /// <summary>Get item as dictionary from sync cache.</summary>
        public const string GetRecord = "sync_GetRecord";
        /// <summary>Get item as Entity from sync cache.</summary>
        public const string GetEntity = "sync_GetEntity";
        /// <summary>Get item as Entity copy using stream convert from sync cache.</summary>
        public const string GetAs = "sync_GetAs";
        /// <summary>Get indicate werher the item exists in sync cache.</summary>
        public const string Contains = "sync_Contains";
        /// <summary>Add new item to sync cache.</summary>
        public const string AddSyncItem = "sync_AddSyncItem";
        /// <summary>Add new entity to sync cache.</summary>
        public const string AddEntity = "sync_AddEntity";
        /// <summary>Remove item from sync cache.</summary>
        public const string Remove = "sync_Remove";

        ///// <summary>Get item as entity stream from sync cache.</summary>
        //public const string GetEntityStream = "sync_GetEntityStream";

        /// <summary>Refresh all items in sync cache.</summary>
        public const string RefreshAll = "sync_RefreshAll";
        /// <summary>Refresh specified item in sync cache.</summary>
        public const string Refresh = "sync_Refresh";
        /// <summary>Get all items copy for specified entity from sync cache.</summary>
        public const string GetEntityItems = "sync_GetEntityItems";
        /// <summary>Get all keys for specified entity from sync cache.</summary>
        public const string GetEntityKeys = "sync_GetEntityKeys";
        /// <summary>Get report of all items for specified entity from sync cache.</summary>
        public const string GetItemsReport = "sync_GetItemsReport";
        /// <summary>Get all entites names from sync cache.</summary>
        public const string GetAllEntityNames = "sync_GetAllEntityNames";
        /// <summary>Restart all items in sync cache.</summary>
        public const string Reset = "sync_Reset";
        /// <summary>Get all items count for specified entity from sync cache.</summary>
        public const string GetEntityItemsCount = "sync_GetEntityItemsCount";
        ///// <summary>Get sync cache statistic.</summary>
        //public const string GetSyncStatistic = "sync_GetSyncStatistic";

        /// <summary>Get item EntityPrimaryKey.</summary>
        public const string GetEntityPrimaryKey = "sync_getentityprimarykey";
        /// <summary>Find entity.</summary>
        public const string FindEntity = "sync_findentity";


    }

#endif
    #endregion
}
