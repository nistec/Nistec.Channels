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
using Nistec.Logging;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Nistec.Channels
{


 /*   
    {
    "Key":"",
    "Body":"",
    "Id":"",
    "Command":"",
    "IsDuplex": true,
    "Expiration":1440,
    "Modified":"",
    "TypeName":"String",
    "Formatter","Json"
    }
*/
  
    /// <summary>
    /// Represent json pipe client channel
    /// </summary>
    public class PipeJsonClient : IDisposable
    {
        #region static send methods

        public static string SendDuplex(string request, string hostAddress, PipeOptions option, int inBufferSize = 8192, int outBufferSize = 8192, bool enableException = true)
        {
            using (PipeJsonClient client = new PipeJsonClient(hostAddress, inBufferSize, outBufferSize, true, option))
            {
                return client.Execute(request, enableException);
            }
        }

        public static string SendDuplex(string request, string configHostName, bool enableException = true)
        {
            using (PipeJsonClient client = new PipeJsonClient(configHostName))
            {
                client.PipeDirection = PipeDirection.InOut;
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(PipeMessage message, string configHostName, bool enableException = true)
        {
            string request = JsonSerializer.Serialize(message);
            using (PipeJsonClient client = new PipeJsonClient(configHostName))
            {
                client.PipeDirection = PipeDirection.InOut;
                string response = client.Execute(request, enableException);
                return JsonSerializer.Deserialize<T>(response);
            }
        }

        public static T SendDuplex<T>(string request, string configHostName, bool enableException = true)
        {
            using (PipeJsonClient client = new PipeJsonClient(configHostName))
            {
                client.PipeDirection = PipeDirection.InOut;
                string response = client.Execute(request, enableException);
                return JsonSerializer.Deserialize<T>(response);
            }
        }

        public static void SendOut(string request, string configHostName, bool enableException = true)
        {
            using (PipeJsonClient client = new PipeJsonClient(configHostName))
            {
                client.PipeDirection = PipeDirection.Out;
                client.Execute(request, enableException);
            }
        }

        //public static void SendIn(string request, string hostName, bool IsAsync, bool enableException = false)
        //{
        //    using (PipeJsonClient client = new PipeJsonClient(hostName))
        //    {
        //        client.PipeDirection = PipeDirection.In;
        //        client.PipeOptions = IsAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None;
        //        client.Execute(request);//, enableException);
        //    }
        //}

        #endregion

        #region members

        ILogger _Logger = Logger.Instance;
        /// <summary>
        /// Get or Set Logger that implements <see cref="ILogger"/> interface.
        /// </summary>
        public ILogger Log { get { return _Logger; } set { if (value != null) _Logger = value; } }


        /// <summary>
        /// DefaultInBufferSize
        /// </summary>
        public const int DefaultInBufferSize = 8192;
        /// <summary>
        /// DefaultOutBufferSize
        /// </summary>
        public const int DefaultOutBufferSize = 8192;
        /// <summary>
        /// DefaultConnectTimeout
        /// </summary>
        public const int DefaultConnectTimeout = 5000;
        /// <summary>
        ///  Get or Set HostName.
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        ///  Get or Set PipeName.
        /// </summary>
        public string PipeName { get; set; }
        /// <summary>
        /// Get or Set PipeDirection (Default=InOut).
        /// </summary>
        public PipeDirection PipeDirection { get; set; }
        /// <summary>
        /// Get or Set PipeOptions (Default=None).
        /// </summary>
        public PipeOptions PipeOptions { get; set; }

        /// <summary>
        /// Get or Set VerifyPipe.
        /// </summary>
        public string VerifyPipe { get; set; }
        /// <summary>
        /// Get or Set ConnectTimeout (Default=5000).
        /// </summary>
        public uint ConnectTimeout { get; set; }
        /// <summary>
        /// Get or Set ReceiveBufferSize (Default=8192).
        /// </summary>
        public int ReceiveBufferSize { get; set; }
        /// <summary>
        /// Get or Set SendBufferSize (Default=8192).
        /// </summary>
        public int SendBufferSize { get; set; }
        /// <summary>
        /// ServerName constant.
        /// </summary>
        public const string ServerName = ".";
        /// <summary>
        /// Get Full Pipe Name.
        /// </summary>
        public string FullPipeName { get { return @"\\" + ServerName + @"\pipe\" + PipeName; } }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isDuplex"></param>
        /// <param name="option"></param>
        protected PipeJsonClient(string hostAddress, int inBufferSize = 8192, int outBufferSize = 8192, bool isDuplex = true, PipeOptions option = PipeOptions.None)
        {
            HostName = hostAddress;
            PipeName = hostAddress;
            ConnectTimeout = (uint)PipeSettings.DefaultConnectTimeout;
            ReceiveBufferSize = inBufferSize <= 0 ? 8192 : inBufferSize;
            SendBufferSize = outBufferSize <= 0 ? 8192 : outBufferSize;
            PipeDirection = isDuplex ? PipeDirection.InOut : System.IO.Pipes.PipeDirection.Out;
            PipeOptions = option;// isAsync ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.None;
            VerifyPipe = hostAddress;
        }

        /// <summary>
        /// Constractor with extra parameters
        /// </summary>
        /// <param name="configHostName"></param>
        protected PipeJsonClient(string configHostName)
        {
            var settings = PipeClientSettings.GetPipeClientSettings(configHostName);

            HostName = settings.HostName;
            PipeName = settings.PipeName;
            ConnectTimeout = (uint) settings.ConnectTimeout;
            ReceiveBufferSize = settings.ReceiveBufferSize;
            SendBufferSize = settings.SendBufferSize;
            PipeDirection = settings.PipeDirection;
            PipeOptions = settings.PipeOptions;
            VerifyPipe = settings.VerifyPipe;
          
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            //if (pipeClientStream != null)
            //{
            //    pipeClientStream.Dispose();
            //    pipeClientStream = null;
            //}
        }
        #endregion

        #region override

        public string Execute(string message, bool enableException = true)
        {
            string response = null;
            try
            {

                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                if (ConnectTimeout <= 0)
                    ConnectTimeout = DefaultConnectTimeout;

                using (NamedPipeClientStream pipeClientStream =
                         new NamedPipeClientStream(ServerName, PipeName,
                             PipeDirection, PipeOptions,
                             System.Security.Principal.TokenImpersonationLevel.Impersonation))
                {

                    Console.WriteLine("Connecting to server...\n");

                    pipeClientStream.Connect((int)ConnectTimeout);

                    Console.WriteLine("Connected to server...\n");

                    // Set the read mode and the blocking mode of the named pipe.
                    pipeClientStream.ReadMode = PipeTransmissionMode.Message;

                    //using (TransString.WriteString strStream = new StreamString(pipeClientStream))
                    //{

                    if (PipeDirection != System.IO.Pipes.PipeDirection.In)
                    {
                        // Send a request from client to server
                        TransString.WriteString(message, pipeClientStream);
                    }

                    if (PipeDirection == System.IO.Pipes.PipeDirection.Out)
                    {
                        //return response;
                    }
                    else
                    {
                        // Receive a response from server.
                        response = TransString.ReadString(pipeClientStream);
                    }
                    //}

                    pipeClientStream.Close();

                    return response;
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
        }

        #endregion
    }

}
