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


namespace Nistec.Channels
{

    public abstract class PipeClient<TRequest> : IDisposable
    {
        #region members
        protected NamedPipeClientStream pipeClient = null;
        const int MaxRetry = 3;

        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null)_Logger = value; } }

        #endregion

        #region settings

        protected PipeSettings Settings;

        public PipeDirection PipeDirection { get; set; }
        public string PipeName { get { return Settings.PipeName; } }

        public const string ServerName = ".";
        public string FullPipeName { get { return @"\\" + ServerName + @"\pipe\" + PipeName; } }

        #endregion

        #region ctor

        

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="configHostName"></param>
        /// <param name="direction"></param>
        protected PipeClient(string configHostName, PipeDirection direction)
        {
            Settings = PipeClientSettings.GetPipeClientSettings(configHostName);
            this.PipeDirection = direction;

        }
        /// <summary>
        /// Constractor with settings parameters
        /// </summary>
        /// <param name="settings"></param>
        protected PipeClient(PipeSettings settings)
        {
            Settings = settings;
            this.PipeDirection = settings.PipeDirection;

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isDuplex"></param>
        /// <param name="isAsync"></param>
        protected PipeClient(string pipeName, int inBufferSize, int outBufferSize, bool isDuplex, bool isAsync)
        {
            Settings = new PipeSettings()
            {
                PipeName = pipeName,
                ConnectTimeout = (uint)PipeSettings.DefaultConnectTimeout,
                InBufferSize = inBufferSize,
                OutBufferSize = outBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = isAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None,
                VerifyPipe = pipeName
            };
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="isDuplex"></param>
        /// <param name="isAsync"></param>
        protected PipeClient(string pipeName, bool isDuplex, bool isAsync)
        {
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

            Settings = new PipeSettings()
            {
                PipeName = pipeName,
                ConnectTimeout = (uint)PipeSettings.DefaultConnectTimeout,
                InBufferSize = PipeSettings.DefaultInBufferSize,
                OutBufferSize = PipeSettings.DefaultOutBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = isAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None,
                VerifyPipe = pipeName
            };
        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="isDuplex"></param>
        /// <param name="connectTimeout"></param>
        protected PipeClient(string pipeName, bool isDuplex, int connectTimeout)
        {
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

            Settings = new PipeSettings()
            {
                PipeName = pipeName,
                ConnectTimeout = (uint)connectTimeout,
                InBufferSize = PipeSettings.DefaultInBufferSize,
                OutBufferSize = PipeSettings.DefaultOutBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = PipeOptions.Asynchronous | PipeOptions.WriteThrough, //PipeOptions.None,
                VerifyPipe = pipeName
            };
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (pipeClient != null)
            {
                pipeClient.Dispose();
                pipeClient = null;
            }
        }
        #endregion

        #region Read/Write

        protected abstract object ExecuteMessage(TRequest message, Type type);

        protected abstract TResponse ExecuteMessage<TResponse>(TRequest message);


        #endregion

        #region Run

        NamedPipeClientStream CreatePipe()
        {
            return new NamedPipeClientStream(
                    ServerName,                 // The server name
                    Settings.PipeName,                   // The unique pipe name
                    Settings.PipeDirection,              // The pipe is duplex
                    Settings.PipeOptions                 // No additional parameters
                    );
        }

        bool Connect()
        {
            int retry = 0;

            while (retry < MaxRetry)
            {

                try
                {
                    pipeClient.Connect((int)Settings.ConnectTimeout);
                    if (!pipeClient.IsConnected)
                    {
                        retry++;
                        if (retry >= MaxRetry)
                        {
                            throw new Exception("Unable to connect to pipe: " + Settings.PipeName);
                        }
                        Thread.Sleep(10);

                        //Netlog.WarnFormat("NativePipeClient retry: {0} ", retry);
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (TimeoutException toex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("PipeClient connection has timeout exception after retry: {0},timeout:{1}, msg: {2}", retry, Settings.ConnectTimeout, toex.Message);
                        throw toex;
                    }
                    retry++;
                }
                catch (Exception pex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("PipeClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
                        throw pex;
                    }
                    retry++;
                }
            }

            return pipeClient.IsConnected;
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public void ExecuteOut(TRequest message, Type type, bool enableException = false)
        {
            Execute(message, type, enableException);
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public object Execute(TRequest message, Type type, bool enableException=false)
        {

            object response = null;// default(TResponse);

            try
            {
                // Try to open the named pipe identified by the pipe name.

                pipeClient = new NamedPipeClientStream(
                    ServerName,                          // The server name
                    Settings.PipeName,                   // The unique pipe name
                    Settings.PipeDirection,              // The pipe is duplex
                    Settings.PipeOptions                 // No additional parameters
                    );
                
                bool ok=Connect();
                if (!ok)
                {
                    throw new Exception("Unable to connect to pipe:" + PipeName);
                }


                // Set the read mode and the blocking mode of the named pipe.
                pipeClient.ReadMode = PipeTransmissionMode.Message;


                return ExecuteMessage(message, type);

            }
            catch (TimeoutException toex)
            {
                Log.Exception("The client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                return response;
            }
            catch (SerializationException sex)
            {
                Log.Exception("The client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                return response;
            }
            catch (MessageException mex)
            {
                Log.Exception("The tcp client throws the MessageException : ", mex, true);
                if (enableException)
                    throw mex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
            finally
            {
                // Close the pipe.
                if (pipeClient != null)
                {
                    pipeClient.Close();
                    pipeClient = null;
                }
            }
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public TResponse Execute<TResponse>(TRequest message, bool enableException = false)
        {

            TResponse response = default(TResponse);
 
            try
            {
                // Try to open the named pipe identified by the pipe name.

                pipeClient = new NamedPipeClientStream(
                    ServerName,                 // The server name
                    Settings.PipeName,                   // The unique pipe name
                    Settings.PipeDirection,              // The pipe is duplex
                    Settings.PipeOptions                 // No additional parameters
                    );


                Connect();

                // Set the read mode and the blocking mode of the named pipe.
                pipeClient.ReadMode = PipeTransmissionMode.Message;

 
                return ExecuteMessage<TResponse>(message);


            }
            catch (TimeoutException toex)
            {
                Log.Exception("The client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                return response;
            }
            catch (SerializationException sex)
            {
                Log.Exception("The client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                return response;
            }
            catch (MessageException mex)
            {
                Log.Exception("The client throws the MessageException : ", mex, true);
                if (enableException)
                    throw mex;
                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("The client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                return response;
            }
            finally
            {
                // Close the pipe.
                if (pipeClient != null)
                {
                    pipeClient.Close();
                    pipeClient = null;
                }
            }
        }

 
        #endregion
    }


    /// <summary>
    /// Represent pipe client channel
    /// </summary>
    public class PipeClient : PipeClient<PipeMessage>, IDisposable
    {
        #region static send methods

        public static object SendDuplex(PipeMessage request, string PipeName, bool IsAsync ,bool enableException=false)
        {
            Type type = request.BodyType;
            request.IsDuplex = true;
            PipeDirection direction = request.IsDuplex ? PipeDirection.InOut : PipeDirection.Out;
            using (PipeClient client = new PipeClient(PipeName, true, IsAsync))
            {
                return client.Execute(request, type, enableException);
            }
        }

        public static T SendDuplex<T>(PipeMessage request, string PipeName, bool IsAsync, bool enableException = false)
        {
            request.IsDuplex = true;
            PipeDirection direction = request.IsDuplex ? PipeDirection.InOut : PipeDirection.Out;
            using (PipeClient client = new PipeClient(PipeName, true, IsAsync))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(PipeMessage request, string PipeName, bool IsAsync, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (PipeClient client = new PipeClient(PipeName, false, IsAsync))
            {
                client.Execute(request, type, enableException);
            }
        }

        public static void SendIn(PipeMessage request, string PipeName, bool IsAsync, bool enableException = false)
        {
            Type type = request.BodyType;
            request.IsDuplex = false;
            using (PipeClient client = new PipeClient(PipeName, false, IsAsync))
            {
                client.PipeDirection = PipeDirection.In;
                client.Execute(request, type, enableException);
            }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="isDuplex"></param>
        /// <param name="isAsync"></param>
        public PipeClient(string pipeName, bool isDuplex, bool isAsync)
            : base(pipeName, isDuplex, isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isDuplex"></param>
        /// <param name="isAsync"></param>
        public PipeClient(string pipeName, int inBufferSize, int outBufferSize, bool isDuplex, bool isAsync)
            : base(pipeName, inBufferSize, outBufferSize, isDuplex, isAsync)
        {

        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="configHostName"></param>
        /// <param name="direction"></param>
        public PipeClient(string configHostName, PipeDirection direction)//, PipeOptions options)
            : base(configHostName, direction)
        {

        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="configHostName"></param>
        public PipeClient(string configHostName)
            : base(configHostName, System.IO.Pipes.PipeDirection.InOut)
        {
            //this.PipeDirection = System.IO.Pipes.PipeDirection.InOut;
        }

        /// <summary>
        /// Constractor with settings parameters
        /// </summary>
        /// <param name="settings"></param>
        public PipeClient(PipeSettings settings)
            : base(settings)
        {

        }

        #endregion

        #region override

        protected override object ExecuteMessage(PipeMessage message, Type type)
        {
            object response = null;

            if (PipeDirection != System.IO.Pipes.PipeDirection.In)
            {
                // Send a request from client to server
                message.EntityWrite(pipeClient, null);
            }

            if (PipeDirection == System.IO.Pipes.PipeDirection.Out)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadAck(pipeClient, type,  Settings.InBufferSize);

            return response;
        }

        protected override TResponse ExecuteMessage<TResponse>(PipeMessage message)
        {
            TResponse response = default(TResponse);

            if (PipeDirection != System.IO.Pipes.PipeDirection.In)
            {
                // Send a request from client to server
                message.EntityWrite(pipeClient, null);
            }

            if (PipeDirection == System.IO.Pipes.PipeDirection.Out)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadAck<TResponse>(pipeClient, Settings.InBufferSize);

            return response;
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public MessageAck Execute(PipeMessage message, bool enableException = false)
        {
            return Execute<MessageAck>(message, enableException);
        }
       
        #endregion

    }

    
}