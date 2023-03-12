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
using System.IO.Pipes;
using System.Text;
using System.IO;
using Nistec.Generic;
using Nistec.Runtime;
using System.Collections;
using Nistec.IO;
using System.Threading;
using System.Runtime.Serialization;
using Nistec.Logging;
using System.Diagnostics;


namespace Nistec.Channels
{

    /// <summary>
    /// Represent pipe client channel
    /// </summary>
    public abstract class AnonymousPipeClient : IDisposable
    {

        #region membrs

        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            DisposePipe();
        }

        #endregion


        #region static

        

        #endregion
        public void Execute(string[] args)
        {

            Console.WriteLine("Client start...");
            bool isOneWay = false;
            try
            {

                //get pipe handle id
                ParentSenderId = args[0];
                ParentReceiverId = args[1];

                Console.WriteLine("Parent sender:{0}, receiver:{1}", ParentSenderId, ParentReceiverId);

                isOneWay = (ParentReceiverId == null || ParentReceiverId == "0");

                //create streams
                _Receiver = new AnonymousPipeClientStream(PipeDirection.In, ParentSenderId);
                _Receiver.ReadMode = PipeTransmissionMode.Byte;

                var request = AnonymousMessage.ReadRequest(_Receiver);

                Console.WriteLine("Client ReadRequest Command:{0}, Key:{1}, Size:{1}", request.Command, request.Id, request.Size);

                if (isOneWay)
                {
                    ExecRequest(request);
                }
                else
                {

                    _Sender = new AnonymousPipeClientStream(PipeDirection.Out, ParentReceiverId);
                    _Sender.ReadMode = PipeTransmissionMode.Byte;

                    var response = ExecRequest(request);
                    if (response == null)
                    {
                        throw new Exception("Error invalid response");
                    }
                    Console.WriteLine("Client Response :Command:{0}, Key:{1}, Size:{1}", response.Command, response.Id, response.Size);
                    AnonymousMessage.WriteResponse(_Sender, response.ToStream());
                    
                    

                    //byte[] responseData = response.ToStream();

                    ////read data
                    //using (NetStream stream = new NetStream())
                    //{
                    //    stream.CopyFrom(_Receiver);

                    //    var response = ExecRequest(stream);

                    //    if (response == null)
                    //    {

                    //    }

                    //    responseData = response.ToArray();
                    //}

                    ////write data
                    //using (BinaryWriter writer = new BinaryWriter(_Sender))
                    //{
                    //    writer.Write(responseData);
                    //    writer.Flush();
                    //}
                }

                ////read data
                //int dataReceive = _Receiver.ReadByte();
                //Console.WriteLine("Client receive: " + dataReceive.ToString());

                ////write data
                //byte dataSend = 24;
                //_Sender.WriteByte(dataSend);
                //Console.WriteLine("Client send: " + dataSend.ToString());
            }
            catch(Exception ex)
            {
                Log.Exception("AnonymousPipeClient error ", ex);

                AnonymousMessage.WriteResponse(_Sender, AnonymousMessage.CreateAnonymousAck(ChannelState.Failed,ex.Message));
            }
            finally
            {
                DisposePipe();
            }
        }

        AnonymousPipeClientStream _Sender;
        AnonymousPipeClientStream _Receiver;
        string ParentSenderId;
        string ParentReceiverId;

        void DisposePipe()
        {
            try
            {
                if (_Sender != null)
                {
                    _Sender.Dispose();
                    _Sender = null;
                    ParentSenderId = null;
                }

                if (_Receiver != null)
                {
                    _Receiver.Dispose();
                    _Receiver = null;
                    ParentReceiverId = null;
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
        }

        protected abstract AnonymousMessage ExecRequest(AnonymousMessage request);
 
    }

    
}