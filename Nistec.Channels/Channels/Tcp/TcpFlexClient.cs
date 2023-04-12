using Nistec.Runtime;
using Nistec.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TCP = System.Net.Sockets;

namespace Nistec.Channels.Tcp
{
    /// <summary>
    /// TcpFlexClient
    /// </summary>
    public class TcpFlexClient : TcpClient<MessageFlex>, IDisposable
    {
        static readonly Dictionary<string, TcpFlexClient> ClientsCache = new Dictionary<string, TcpFlexClient>();
        static TcpFlexClient GetClient(string hostName)
        {
            TcpFlexClient client = null;
            if (ClientsCache.TryGetValue(hostName, out client))
            {
                return client;
            }
            client = new TcpFlexClient(hostName);
            if (client == null)
            {
                throw new Exception("Invalid configuration for tcp client with host name:" + hostName);
            }
            ClientsCache[hostName] = client;
            return client;
        }

        #region static send methods

        public static bool Ping(string HostAddress, int Port, int ConnectTimeout = 5000)
        {

            TCP.TcpClient tcpClient = null;
            string rawAddress = HostAddress;
            try
            {
                rawAddress = string.Format("{0}:{1}", HostAddress, Port);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(HostAddress), Port);
                tcpClient = new TCP.TcpClient();
                tcpClient.SendTimeout = ConnectTimeout;
                tcpClient.SendBufferSize = TcpSettings.DefaultSendBufferSize;
                tcpClient.ReceiveBufferSize = TcpSettings.DefaultReceiveBufferSize;
                tcpClient.ReceiveTimeout = TcpSettings.DefaultReadTimeout;
                tcpClient.Connect(ep);

                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp address: " + rawAddress);
                }
                else
                {
                    tcpClient.Close();
                    return true;
                }
            }
            catch (TimeoutException toex)
            {
                throw new ChannelException(ChannelState.TimeoutError, "Unable to connect to tcp address: " + rawAddress, toex);
            }
            catch (Exception pex)
            {
                throw new ChannelException(ChannelState.ConnectionError, "Unable to connect to tcp address: " + rawAddress, pex);
            }
        }

        /// <summary>
        /// Send Duplex
        /// </summary>
        /// <param name="request"></param>
        /// <param name="hostName"></param>
        /// <param name="enableException"></param>
        /// <returns></returns>
        public static TransStream SendDuplexStream(MessageFlex request, string hostName, bool enableException = false)
        {
            request.IsDuplex = true;
            request.TransformType = TransformType.Stream;
            using (TcpFlexClient client = new TcpFlexClient(hostName))
            {
                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageFlex request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static TransStream SendDuplexStream(MessageFlex request, string HostAddress, int port, int connectTimeout, int readTimeout, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {

                return client.Execute<TransStream>(request, enableException);
            }
        }
        public static void SendDuplexStreamAsync(MessageFlex request, string HostAddress, int port, int connectTimeout, Action<TransStream> onCompleted, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, IsAsync))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static void SendDuplexStreamAsync(MessageFlex request, string HostAddress, int port, int connectTimeout, int readTimeout, Action<TransStream> onCompleted, bool IsAsync, bool enableException = false)
        {
            request.TransformType = TransformType.Stream;
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {
                client.ExecuteAsync<TransStream>(request, onCompleted, enableException);
            }
        }

        public static string SendJsonDuplex(string json, string HostAddress, int port, int connectTimeout, int readTimeout, bool IsAsync, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json, null);
            using (TcpClient client = new TcpClient(HostAddress, port, connectTimeout, readTimeout, IsAsync))
            {
                var response = client.Execute(message, enableException);
                return JsonSerializer.Serialize(response);
            }
        }

        public static object SendJsonDuplex(string json, string HostName, bool enableException = false)
        {
            TcpMessage message = new TcpMessage();
            message.EntityRead(json, null);
            using (TcpClient client = new TcpClient(HostName))
            {
                return client.Execute(message, enableException);
            }
        }

        public static object SendDuplex(MessageFlex request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, IsAsync))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageFlex request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, IsAsync))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageFlex request, string HostAddress, int port, int connectTimeout, bool IsAsync, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, port, connectTimeout, IsAsync))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static object SendDuplex(MessageFlex request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostName))
            {
                return client.Execute(request, enableException);
            }
        }

        public static T SendDuplex<T>(MessageFlex request, string HostName, bool enableException = false)
        {
            request.IsDuplex = true;
            using (TcpFlexClient client = new TcpFlexClient(HostName))
            {
                return client.Execute<T>(request, enableException);
            }
        }

        public static void SendOut(MessageFlex request, string HostName, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpFlexClient client = new TcpFlexClient(HostName))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        public static void SendOut(MessageFlex request, string HostAddress, int Port, bool enableException = false)
        {
            //Type type = request.BodyType;
            request.IsDuplex = false;
            using (TcpFlexClient client = new TcpFlexClient(HostAddress, Port))
            {
                client.ExecuteOut(request, enableException);
            }
        }

        #endregion

        #region ctor

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        public TcpFlexClient(string hostAddress, int port)
            : base(hostAddress, port, TcpSettings.DefaultConnectTimeout, TcpSettings.DefaultReadTimeout, false)
        {

        }
        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="isAsync"></param>
        public TcpFlexClient(string hostAddress, int port, int connectTimeout, bool isAsync)
            : base(hostAddress, port, connectTimeout, isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="isAsync"></param>
        public TcpFlexClient(string hostAddress, int port, int connectTimeout, int readTimeout, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout, isAsync)
        {

        }

        /// <summary>
        /// Constractor with arguments
        /// </summary>
        /// <param name="hostAddress"></param>
        /// <param name="port"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="readTimeout"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        /// <param name="isAsync"></param>
        public TcpFlexClient(string hostAddress, int port, int connectTimeout, int readTimeout, int inBufferSize, int outBufferSize, bool isAsync)
            : base(hostAddress, port, connectTimeout, readTimeout, inBufferSize, outBufferSize, isAsync)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> from configuration.
        /// </summary>
        /// <param name="configHost"></param>
        public TcpFlexClient(string configHost)
            : base(configHost)
        {

        }

        /// <summary>
        /// Initialize a new instance of <see cref="TcpClient"/> with given <see cref="TcpSettings"/> settings.
        /// </summary>
        /// <param name="settings"></param>
        public TcpFlexClient(TcpSettings settings)
            : base(settings)
        {

        }

        #endregion

        #region override

        protected override void ExecuteOneWay(NetworkStream stream, MessageFlex message)
        {
            // Send a request from client to server
            message.EntityWrite(stream, null);
        }

        protected override object ExecuteMessage(NetworkStream stream, MessageFlex message)//, Type type)
        {
            object response = null;

            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.IsDuplex == false)
            {
                return response;
            }

            // Receive a response from server.
            response = message.ReadResponse(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize, false);

            return response;
        }
        //protected override object ExecuteMessage(NetworkStream stream, MessageStream message)//, Type type)
        //{
        //    object response = null;

        //    // Send a request from client to server
        //    message.EntityWrite(stream, null);

        //    if (message.IsDuplex == false)
        //    {
        //        return response;
        //    }

        //    // Receive a response from server.
        //    response = message.ReadResponse(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize, message.TransformType, false);

        //    return response;
        //}
        protected override TResponse ExecuteMessage<TResponse>(NetworkStream stream, MessageFlex message)
        {
            TResponse response = default(TResponse);

            // Send a request from client to server
            message.EntityWrite(stream, null);

            if (message.IsDuplex == false)
            {
                return response;
            }

            // Receive a response from server.

            response = message.ReadResponse<TResponse>(stream, Settings.ReadTimeout, Settings.ReceiveBufferSize);

            return response;
        }

        /// <summary>
        /// connect to the tcp channel and execute request.
        /// </summary>
        public new MessageAck Execute(MessageFlex message, bool enableException = false)
        {
            return Execute<MessageAck>(message, enableException);
        }

        #endregion

    }
}
