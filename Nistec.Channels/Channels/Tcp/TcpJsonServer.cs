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
using System.Net.Sockets;
using System.Text;

namespace Nistec.Channels.Tcp
{
    /// <summary>
    /// Represent a json tcp server listner.
    /// </summary>
    public abstract class TcpJsonServer : TcpServer<StringMessage>
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

        
        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="hostName"></param>
        public TcpJsonServer(string hostName)
            : base(hostName)
        {
            //LoadRemoteCache();
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="settings"></param>
        public TcpJsonServer(TcpSettings settings)
            : base(settings)
        {
            //LoadRemoteCache();
        }

        #endregion

        #region abstract methods

        ///// <summary>
        ///// Execute client request and return response as stream.
        ///// </summary>
        ///// <param name="message"></param>
        ///// <returns></returns>
        //protected override NetStream ExecRequset(StringMessage message)
        //{
        //    if (ActionRequset != null)
        //        return ActionRequset(message.Message);
        //    else
        //    {
        //        string response=null;
        //        DoActionRequset(message.Message, ref response);
        //        if (response == null)
        //            return null;
        //        var nstream = new NetStream();
        //        StringMessage.WriteString(response, nstream);
        //        return nstream;
        //    }
        //}

        /// <summary>
        /// ReadRequest
        /// </summary>
        /// <param name="networkStream"></param>
        /// <returns></returns>
        protected override StringMessage ReadRequest(NetworkStream networkStream)
        {
            //StringMessage.ReadString(pipeServer);
            return new StringMessage(networkStream);
        }

        #endregion
    }

}
