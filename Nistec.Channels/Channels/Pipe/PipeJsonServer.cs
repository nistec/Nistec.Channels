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
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace Nistec.Channels
{
    /// <summary>
    /// Represent a json pipe server listner.
    /// </summary>
    public abstract class PipeJsonServer : PipeServer<TransString>
    {

        //public Func<string, NetStream> ActionRequset { get; set; }

        #region override
        /// <summary>
        /// OnStart
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
        }
        /// <summary>
        /// OnStop
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();
        }
        /// <summary>
        /// OnLoad
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();
        }
        #endregion

        #region ctor

        ///// <summary>
        ///// Constractor default
        ///// </summary>
        //public PipeJsonServer()
        //    : base(CacheDefaults.DefaultCacheHostName, true)
        //{
        //    //LoadRemoteCache();
        //}

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="loadFromSettings"></param>
        public PipeJsonServer(string hostName, bool loadFromSettings)
            : base(hostName, loadFromSettings)
        {
            //LoadRemoteCache();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="settings"></param>
        public PipeJsonServer(PipeSettings settings)
            : base(settings)
        {
            //LoadRemoteCache();
        }
        
        #endregion

        #region abstract methods
    
        /// <summary>
        /// ReadRequest
        /// </summary>
        /// <param name="pipeServer"></param>
        /// <returns></returns>
        protected override TransString ReadRequest(NamedPipeServerStream pipeServer)
        {
            //TransString.ReadString(pipeServer);
            return new TransString(pipeServer);
        }

        /// <summary>
        /// Occured when new client connected to cache.
        /// </summary>
        protected override void OnClientConnected()
        {
            //Task.Factory.StartNew(() => AgentManager.Cache.PerformanceCounter.AddRequest());
        }

        #endregion
    }

}
