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
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using Nistec.Runtime;
using Nistec.Generic;
using Nistec.IO;
using System.Security.Principal;
using Nistec.Logging;
using System.Diagnostics;
using Nistec.Serialization;
using System.Threading.Tasks;


namespace Nistec.Channels
{


    /// <summary>
    /// Represent a anonymous pipe server
    /// </summary>
    public class AnonymousPipeServer : IDisposable
    {
        #region membrs

        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        #endregion

        #region settings
        /// <summary>
        /// Get or Set the file name.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Get or Set the connection timeout.
        /// </summary>
        public uint ConnectTimeout { get; set; }
        /// <summary>
        /// Get or Set the in buffer size in bytes.
        /// </summary>
        public int BufferSize { get; set; }

        const uint DefaultConnectTimeout = 5000;
        const int DefaultBufferSize = 1024;

        #endregion

        #region ctor

        public AnonymousPipeServer(string filename)
        {
            this.FileName = filename;
            this.ConnectTimeout = DefaultConnectTimeout;
            this.BufferSize = DefaultBufferSize;
        }

        public AnonymousPipeServer(string filename, int bufferSize, uint connectTimeout)
        {
            this.FileName = filename;
            this.ConnectTimeout = connectTimeout;
            this.BufferSize = bufferSize;
        }




        ///// <summary>
        ///// Constractor with extra parameters
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="loadFromSettings"></param>
        //protected AnonymousPipeServer(string name, bool loadFromSettings)
        //{
        //    FileName = name;

        //    AnonymousPipeSettings settings = new AnonymousPipeSettings(name, true, loadFromSettings);
        //    this.FileName = settings.FileName;
        //    this.ConnectTimeout = settings.ConnectTimeout;
        //    this.BufferSize = settings.BufferSize;
        //    this.PipeDirection = settings.PipeDirection;
        //    this.PipeOptions = settings.PipeOptions;

        //}



        ///// <summary>
        ///// Constractor with settings
        ///// </summary>
        ///// <param name="settings"></param>
        //protected AnonymousPipeServer(AnonymousPipeSettings settings)
        //{
        //    this.FileName = settings.FileName;
        //    this.ConnectTimeout = settings.ConnectTimeout;
        //    this.BufferSize = settings.BufferSize;
        //    this.PipeDirection = settings.PipeDirection;
        //    this.PipeOptions = settings.PipeOptions;


        //}
        #endregion

        #region IDisposable

        public void Dispose()
        {
            DisposePipe();
        }

        #endregion

        #region static send methods

        public static AnonymousMessage SendDuplex(AnonymousMessage request, string Filename, bool IsAsync = false, bool enableException = false)
        {
            request.DuplexType= DuplexTypes.Respond;
            using (AnonymousPipeServer server = new AnonymousPipeServer(Filename))
            {
                if (IsAsync)
                    return server.SendMessageAsync(request, enableException);
                else
                    return server.SendMessage(request, enableException);
            }
        }

        public static T SendDuplex<T>(AnonymousMessage request, string Filename, bool IsAsync = false, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (AnonymousPipeServer server = new AnonymousPipeServer(Filename))
            {
                if (IsAsync)
                    return server.SendAsync<T>(request, enableException);
                else
                    return server.Send<T>(request, enableException);
            }
        }

        public static void SendOut(AnonymousMessage request, string Filename, bool IsAsync = false, bool enableException = false)
        {
            request.DuplexType = DuplexTypes.None;
            using (AnonymousPipeServer server = new AnonymousPipeServer(Filename))
            {
                if (IsAsync)
                    server.SendMessageOneWayAsync(request, enableException);
                else
                    server.SendMessageOneWay(request, enableException);
            }
        }

        #endregion

        #region create

        AnonymousPipeServerStream _Sender;
        AnonymousPipeServerStream _Receiver;
        string SenderId;
        string ReceiverId;
        Process _ClientProcess;

        void DisposePipe()
        {
            try
            {
                if (_Sender != null)
                {
                    _Sender.Dispose();
                    _Sender = null;
                    SenderId = null;
                }

                if (_Receiver != null)
                {
                    _Receiver.Dispose();
                    _Receiver = null;
                    ReceiverId = null;
                }

                if (_ClientProcess != null)
                {
                    _ClientProcess.Dispose();
                    _ClientProcess = null;
                }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
        }

        void Create()
        {
  
            //create streams
            _Sender = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _Receiver = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            _Sender.ReadMode = PipeTransmissionMode.Byte;
            _Receiver.ReadMode = PipeTransmissionMode.Byte;

            //start client, pass pipe ids as command line parameter 

            SenderId = _Sender.GetClientHandleAsString();
            ReceiverId = _Receiver.GetClientHandleAsString();

            var startInfo = new ProcessStartInfo(FileName, SenderId + " " + ReceiverId);
            startInfo.UseShellExecute = false;
            _ClientProcess = Process.Start(startInfo);

            //release resources handlet by client
            _Sender.DisposeLocalCopyOfClientHandle();
            _Receiver.DisposeLocalCopyOfClientHandle();

        }
        void CreateOneWay()
        {
 
            //create streams
            _Sender = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
 
            //start client, pass pipe ids as command line parameter 

            SenderId = _Sender.GetClientHandleAsString();
            ReceiverId = "0";

            var startInfo = new ProcessStartInfo(FileName, SenderId + " " + ReceiverId);
            startInfo.UseShellExecute = false;
            _ClientProcess = Process.Start(startInfo);

            //release resources handlet by client
            _Sender.DisposeLocalCopyOfClientHandle();

        }
        #endregion

        #region send/receive


        public TResponse Send<TResponse>(object request, bool enableException = false)
        {
            try
            {
                Create();

                SendRequest(request);

                return GetResponse<TResponse>();
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return default(TResponse);
            }
            finally
            {
                DisposePipe();
            }
        }

        public byte[] Send(byte[] data)
        {
            try
            {
                Create();

                SendRequestData(data);

                return GetResponseData();
            }
            finally
            {
                DisposePipe();
            }
        }

        public NetStream Send(NetStream stream)
        {
            try
            {
                Create();
                SendRequest(stream);

                return GetResponseStream();
            }
            finally
            {
                DisposePipe();
            }
        }

        public TResponse SendAsync<TResponse>(object request, bool enableException = false)
        {
            try
            {
                Create();

                Task task = new Task(() => SendRequest(request));
                {
                    task.Wait();
                };
                task.TryDispose();

                Task<TResponse> task2 = new Task<TResponse>(() => GetResponse<TResponse>());
                {
                    task2.Wait();

                    return task2.Result;
                };
                task2.TryDispose();
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return default(TResponse);
            }
            finally
            {
                DisposePipe();
            }
        }

        #endregion

        #region send\recieve message


        public AnonymousMessage SendMessage(AnonymousMessage request, bool enableException = false)
        {
            try
            {
                Create();

                SendRequestStream(request.ToStream());

                return GetResponseMessage();
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return null;
            }
            finally
            {
                DisposePipe();
            }
        }

        public AnonymousMessage SendMessageAsync(AnonymousMessage request, bool enableException = false)
        {
            try
            {
                Create();

                Task task = new Task(() => SendRequestStream(request.ToStream()));
                {
                    task.Wait();
                };
                task.TryDispose();

                Task<AnonymousMessage> task2 = new Task<AnonymousMessage>(() => GetResponseMessage());
                {
                    task.Wait();

                    return task2.Result;
                };
                task2.TryDispose();
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
                return null;
            }
            finally
            {
                DisposePipe();
            }
        }

        #endregion

        #region send one way
        public void SendOneWay(object request)
        {
            try
            {
                CreateOneWay();

                SendRequest(request);

                _ClientProcess.WaitForExit();

                Console.WriteLine("Client execution finished");
            }
            finally
            {
                DisposePipe();
            }
        }

        public void SendMessageOneWay(AnonymousMessage request, bool enableException = false)
        {
            try
            {
                CreateOneWay();

                SendRequest(request);

                _ClientProcess.WaitForExit();

                Console.WriteLine("Client execution finished");
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
            }
            finally
            {
                DisposePipe();
            }
        }

        public void SendMessageOneWayAsync(AnonymousMessage request, bool enableException = false)
        {
            try
            {
                CreateOneWay();

                Task task = new Task(() => SendRequest(request));
                {
                    task.Wait();
                };
                task.TryDispose();

                _ClientProcess.WaitForExit();

                Console.WriteLine("Client execution finished");
            }
            catch (Exception ex)
            {
                if (enableException)
                    throw ex;
            }
            finally
            {
                DisposePipe();
            }
        }
        #endregion

        #region request \ response
        void SendRequestStream(NetStream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(_Sender))
            {
                writer.Write(stream.ToArray());
                writer.Flush();
            }
        }

        NetStream GetResponseStream()
        {
            var stream = NetStream.CopyStream(_Receiver);

            //wait until client is closed
            _ClientProcess.WaitForExit();
            Console.WriteLine("Client execution finished");
            return stream;
        }

        void SendRequestData(byte[] data)
        {
            using (BinaryWriter writer = new BinaryWriter(_Sender))
            {
                writer.Write(data);
                writer.Flush();
            }
        }
        byte[] GetResponseData()
        {
            using (NetStream stream = AnonymousMessage.CopyStream(_Receiver))
            {
                //wait until client is closed
                _ClientProcess.WaitForExit();
                Console.WriteLine("Client execution finished");
                return stream.ToArray();
            }
        }
        

        void SendRequest(object request)
        {
            byte[] data = BinarySerializer.SerializeToBytes(request);

            using (BinaryWriter writer = new BinaryWriter(_Sender))
            {
                writer.Write(data);
                writer.Flush();
            }
        }

        AnonymousMessage GetResponseMessage()
        {
            NetStream stream = AnonymousMessage.CopyStream(_Receiver);
            AnonymousMessage message = (AnonymousMessage)AnonymousMessage.Create(stream, null);
            
            _ClientProcess.WaitForExit();
            Console.WriteLine("Client execution finished");

            return message;
        }

        T GetResponse<T>()
        {
 
            using (NetStream stream = AnonymousMessage.CopyStream(_Receiver))
            {
                T response = BinarySerializer.Deserialize<T>(stream.ToArray());

                //wait until client is closed
                _ClientProcess.WaitForExit();
                Console.WriteLine("Client execution finished");

                return response;
            }
        }
        #endregion
    }
}
