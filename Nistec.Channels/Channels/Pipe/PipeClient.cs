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
using System.Security.Principal;
using Nistec.Serialization;

namespace Nistec.Channels
{

    public abstract class PipeClient<TRequest> : IDisposable where TRequest: ITransformMessage
    {
        #region members
        protected NamedPipeClientStream pipeClientStream = null;
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
        /// <param name="hostName"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isDuplex"></param>
        /// <param name="option"></param>
        protected PipeClient(string hostName, int inBufferSize, int outBufferSize, bool isDuplex, PipeOptions option)
        {
            Settings = new PipeSettings()
            {
                HostName = hostName,
                PipeName = hostName,
                ConnectTimeout =PipeSettings.DefaultConnectTimeout,
                ReceiveBufferSize = inBufferSize,
                SendBufferSize = outBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = option,// isAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None,
                VerifyPipe = hostName
            };
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

        }
        protected PipeClient(string hostName,int timeout, int inBufferSize, int outBufferSize, bool isDuplex, PipeOptions option)
        {
            Settings = new PipeSettings()
            {
                HostName = hostName,
                PipeName = hostName,
                ConnectTimeout = timeout,
                ReceiveBufferSize = inBufferSize,
                SendBufferSize = outBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = option,// isAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None,
                VerifyPipe = hostName
            };
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

        }
        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="isDuplex"></param>
        /// <param name="option"></param>
        protected PipeClient(string hostName, bool isDuplex, PipeOptions option)
        {
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

            Settings = new PipeSettings()
            {
                HostName = hostName,
                PipeName = hostName,
                ConnectTimeout = PipeSettings.DefaultConnectTimeout,
                ReceiveBufferSize = PipeSettings.DefaultReceiveBufferSize,
                SendBufferSize = PipeSettings.DefaultSendBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = option,// isAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None,
                VerifyPipe = hostName,
            };
        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="isDuplex"></param>
        /// <param name="option"></param>
        /// <param name="connectTimeout"></param>
        protected PipeClient(string hostName, bool isDuplex, PipeOptions option, int connectTimeout)
        {
            this.PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;

            Settings = new PipeSettings()
            {
                HostName= hostName,
                PipeName = hostName,
                ConnectTimeout = connectTimeout,
                ReceiveBufferSize = PipeSettings.DefaultReceiveBufferSize,
                SendBufferSize = PipeSettings.DefaultSendBufferSize,
                PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out,
                PipeOptions = option,// PipeOptions.Asynchronous | PipeOptions.WriteThrough, //PipeOptions.None,
                VerifyPipe = hostName
            };
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (pipeClientStream != null)
            {
                pipeClientStream.Dispose();
                pipeClientStream = null;
            }
        }
        #endregion

        #region Read/Write
       
        protected abstract void ExecuteOneWay(TRequest message);

        protected abstract object ExecuteMessage(TRequest message);

        protected abstract TResponse ExecuteMessage<TResponse>(TRequest message);
        //protected abstract TransStream ExecuteMessageStream(TRequest message);
        
        #endregion

        #region Run

        NamedPipeClientStream CreatePipe()
        {
            return new NamedPipeClientStream(
                    ServerName,                           // The server name
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
                    if(Settings.ConnectTimeout<=0)
                        pipeClientStream.Connect();
                    else
                        pipeClientStream.Connect((int)Settings.ConnectTimeout);
                    if (!pipeClientStream.IsConnected)
                    {
                        retry++;
                        if (retry >= MaxRetry)
                        {
                            throw new ChannelException(ChannelState.ConnectionError,"Unable to connect to pipe: " + Settings.PipeName);
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
                        throw new ChannelException(ChannelState.TimeoutError, string.Format("PipeClient connection error after retry: {0}, PipeName: {1}", retry, Settings.PipeName), toex);
                    }
                    retry++;
                }
                catch (Exception pex)
                {
                    if (retry >= MaxRetry)
                    {
                        Log.Error("PipeClient connection error after retry: {0}, msg: {1}", retry, pex.Message);
                        throw new ChannelException(ChannelState.ConnectionError, string.Format("PipeClient connection error after retry: {0}, PipeName: {1}", retry, Settings.PipeName), pex);
                    }
                    retry++;
                }
            }

            return pipeClientStream.IsConnected;
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public void ExecuteOut(TRequest message,  bool enableException = false)
        {
            Execute(message, enableException);
        }

        /// <summary>
        /// connect to the named pipe and execute request.
        /// </summary>
        public object Execute(TRequest message, bool enableException=false)
        {

            object response = null;// default(TResponse);

            try
            {
                // Try to open the named pipe identified by the pipe name.

                pipeClientStream = new NamedPipeClientStream(
                    ServerName,                          // The server name
                    Settings.PipeName,                   // The unique pipe name
                    Settings.PipeDirection,              // The pipe is duplex
                    Settings.PipeOptions                 // No additional parameters
                    );
                
                bool ok=Connect();
                if (!ok)
                {
                    throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to pipe:" + PipeName);
                }
                
                // Set the read mode and the blocking mode of the named pipe.
                pipeClientStream.ReadMode = PipeTransmissionMode.Message;

                if (message.DuplexType.IsDuplex())
                {
                    return ExecuteMessage(message);
                }
                else
                {
                    ExecuteOneWay(message);
                    return null;
                }
            }
            catch (ChannelException mex)
            {
                Log.Exception("The client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
                return response;
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
                if (pipeClientStream != null)
                {
                    pipeClientStream.Close();
                    pipeClientStream = null;
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

                pipeClientStream = new NamedPipeClientStream(
                    ServerName,                 // The server name
                    Settings.PipeName,                   // The unique pipe name
                    Settings.PipeDirection,              // The pipe is duplex
                    Settings.PipeOptions                 // No additional parameters
                    );


                Connect();

                // Set the read mode and the blocking mode of the named pipe.
                pipeClientStream.ReadMode = PipeTransmissionMode.Message;

                if (message.DuplexType.IsDuplex())
                    return ExecuteMessage<TResponse>(message);
                else
                {
                    ExecuteOneWay(message);
                    return default(TResponse);
                }

            }
            catch (ChannelException mex)
            {
                Log.Exception("The client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
                return response;
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
                if (pipeClientStream != null)
                {
                    pipeClientStream.Close();
                    pipeClientStream = null;
                }
            }
        }

        public void ExecuteAsync<TResponse>(TRequest message, Action<TResponse> onCompleted, bool enableException = false)
        {

            TResponse response = default(TResponse);

            try
            {
                // Try to open the named pipe identified by the pipe name.

                pipeClientStream = new NamedPipeClientStream(
                    ServerName,                 // The server name
                    Settings.PipeName,                   // The unique pipe name
                    Settings.PipeDirection,              // The pipe is duplex
                    Settings.PipeOptions                 // No additional parameters
                    );


                Connect();

                // Set the read mode and the blocking mode of the named pipe.
                pipeClientStream.ReadMode = PipeTransmissionMode.Message;

                if (message.DuplexType.IsDuplex())
                    onCompleted(ExecuteMessage<TResponse>(message));
                else
                {
                    ExecuteOneWay(message);
                    onCompleted(default(TResponse));
                }

            }
            catch (ChannelException mex)
            {
                Log.Exception("The client throws the ChannelException : ", mex, true);
                if (enableException)
                    throw mex;
                onCompleted(response);
            }
            catch (TimeoutException toex)
            {
                Log.Exception("The client throws the TimeoutException : ", toex, true);
                if (enableException)
                    throw toex;
                onCompleted(response);
            }
            catch (SerializationException sex)
            {
                Log.Exception("The client throws the SerializationException : ", sex, true);
                if (enableException)
                    throw sex;
                onCompleted(response);
            }
            catch (Exception ex)
            {
                Log.Exception("The client throws the error: ", ex, true);

                if (enableException)
                    throw ex;

                onCompleted(response);
            }
            finally
            {
                // Close the pipe.
                if (pipeClientStream != null)
                {
                    pipeClientStream.Close();
                    pipeClientStream = null;
                }
            }
        }


        #endregion
    }


    /// <summary>
    /// Represent pipe client channel
    /// </summary>
    public class PipeClient : PipeClient<MessageStream>, IDisposable
    {
        #region static send methods

        public static bool Ping(string ServerName, string PipeName, int ConnectTimeout = 3000)
        {
           try
            {
                NamedPipeClientStream pipeClientStream = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.None);

                pipeClientStream.Connect(ConnectTimeout);

                return pipeClientStream.IsConnected;
            }
            catch (TimeoutException toex)
            {
                throw toex;
            }
            catch (Exception pex)
            {
                throw pex;
            }
        }

        /// <summary>
        /// Send Duplex message with return value.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="option"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static TransStream SendDuplexStream(MessageStream request, string hostName, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (PipeClient client = new PipeClient(hostName, true, option))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string hostName,int timeout, int inBufferSize, int outBufferSize, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (PipeClient client = new PipeClient(hostName, timeout, inBufferSize, outBufferSize, true, option))
            {
               
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageStream request, string hostName, int timeout, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (PipeClient client = new PipeClient(hostName, timeout, PipeSettings.DefaultReceiveBufferSize, PipeSettings.DefaultSendBufferSize, true, option))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static void SendDuplexStreamAsync(MessageStream request, string hostName, Action<TransStream> onCompleted, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (PipeClient client = new PipeClient(hostName, true, option))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted,enableException);
            }
        }
        public static void SendDuplexStreamAsync(MessageStream request, string hostName, int timeout, Action<TransStream> onCompleted, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            request.TransformType = TransformType.Stream;
            using (PipeClient client = new PipeClient(hostName, timeout, PipeSettings.DefaultReceiveBufferSize, PipeSettings.DefaultSendBufferSize, true, option))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }
        public static string SendJsonDuplex(string request, string hostName, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            PipeMessage msg = new PipeMessage();
            msg.EntityRead(request, new JsonSerializer(JsonSerializerMode.Read, JsonSerializer.DefaultOption));
            using (PipeClient client = new PipeClient(hostName, true, option))
            {
                client.PipeDirection = PipeDirection.InOut;
                var o= client.Execute(msg, enableException);
                return o == null ? null : JsonSerializer.Serialize(o);
            }
        }
        public static void SendJsonOut(string request, string hostName, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            PipeMessage msg = new PipeMessage();
            msg.EntityRead(request, new JsonSerializer(JsonSerializerMode.Read, JsonSerializer.DefaultOption));
            using (PipeClient client = new PipeClient(hostName, true, option))
            {
                client.PipeDirection = PipeDirection.Out;
                client.ExecuteOut(msg, enableException);
            }
        }

        public static object SendDuplex(MessageStream request, string hostName, bool enableException=false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (PipeClient client = new PipeClient(hostName, true, option))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageStream request, string hostName, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.Respond;
            using (PipeClient client = new PipeClient(hostName, true, option))
            {
                return client.Execute<T>(request, enableException);
            }
        }

      
        public static void SendOut(MessageStream request, string hostName, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.None;
            using (PipeClient client = new PipeClient(hostName, false, option))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static void SendIn(MessageStream request, string hostName, bool enableException = false, PipeOptions option = PipeOptions.None)
        {
            request.DuplexType = DuplexTypes.None;
            using (PipeClient client = new PipeClient(hostName, false, option))
            {
                client.PipeDirection = PipeDirection.In;
                client.Execute(request, enableException);
            }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="isDuplex"></param>
        /// <param name="option"></param>
        public PipeClient(string hostName, bool isDuplex,PipeOptions option)
            : base(hostName, isDuplex, option)
        {
           
        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isDuplex"></param>
        /// <param name="option"></param>
        public PipeClient(string hostName, int inBufferSize, int outBufferSize, bool isDuplex, PipeOptions option)
            : base(hostName, inBufferSize, outBufferSize, isDuplex, option)
        {

        }

        public PipeClient(string hostName,int timeout, int inBufferSize, int outBufferSize, bool isDuplex, PipeOptions option)
            : base(hostName, timeout, inBufferSize, outBufferSize, isDuplex, option)
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

        protected override void ExecuteOneWay(MessageStream message)
        {
            // Send a request from client to server
            message.EntityWrite(pipeClientStream, null);
        }

        protected override object ExecuteMessage(MessageStream message)//, Type type)
        {
            object response = null;

            if (PipeDirection != System.IO.Pipes.PipeDirection.In)
            {
                // Send a request from client to server
                message.EntityWrite(pipeClientStream, null);
            }

            if (PipeDirection == System.IO.Pipes.PipeDirection.Out)
            {
                return response;
            }

            // Receive a response from server.
            //response = message.ReadResponse(pipeClientStream,  Settings.ReceiveBufferSize, message.TransformType, false);
            var ts = message.ReadResponse<TransStream>(pipeClientStream, Settings.ReceiveBufferSize);
            if (ts != null)
                response = ts.ReadValue();
            return response;
        }

        protected override TResponse ExecuteMessage<TResponse>(MessageStream message)
        {
            TResponse response = default(TResponse);

            if (PipeDirection != System.IO.Pipes.PipeDirection.In)
            {
                // Send a request from client to server
                message.EntityWrite(pipeClientStream, null);
           }

            if (PipeDirection == System.IO.Pipes.PipeDirection.Out)
            {
                return response;
            }
            //var ts = message.ReadResponse<TransStream>(pipeClientStream, Settings.ReceiveBufferSize);
            //if (ts != null)
            //{
            //    response = ts.ReadValue<TResponse>();
            //}
            // Receive a response from server.
            response = message.ReadResponse<TResponse>(pipeClientStream, Settings.ReceiveBufferSize);

            return response;
        }

        //protected override TransStream ExecuteMessageStream(PipeMessage message)//, Type type)
        //{
        //    TransStream response = null;

        //    if (PipeDirection != System.IO.Pipes.PipeDirection.In)
        //    {
        //        // Send a request from client to server
        //        message.EntityWrite(pipeClientStream, null);
        //    }

        //    if (PipeDirection == System.IO.Pipes.PipeDirection.Out)
        //    {
        //        return response;
        //    }

        //    // Receive a response from server.
        //    response = new TransStream(pipeClientStream, message.TransformType, Settings.ReceiveBufferSize);// message.ReadAck(pipeClientStream, message.TransformType, Settings.ReceiveBufferSize);

        //    return response;
        //}

        ///// <summary>
        ///// connect to the named pipe and execute request.
        ///// </summary>
        //public MessageAck Execute(PipeMessage message, bool enableException = false)
        //{
        //    return Execute<MessageAck>(message, enableException);
        //}

        #endregion

    }

}