using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nistec.Channels;
using Nistec.Generic;
using Nistec.IO;
using Nistec.Serialization;
using Nistec.Runtime;
using Nistec.Channels.Http;
using Nistec.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;
using Nistec.Threading;
using Nistec.Logging;

namespace Nistec.Channels.RemoteQueue
{


    public abstract class RemoteApi: ChannelSettings
    {
        /// <summary>
        /// CConvert stream to json format.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToJson(NetStream stream, JsonFormat format)
        {
            using (BinaryStreamer streamer = new BinaryStreamer(stream))
            {
                var obj = streamer.Decode();
                if (obj == null)
                    return null;
                else
                    return JsonSerializer.Serialize(obj, null, format);
            }
        }

        #region on completed

        protected void OnFault(string message)
        {
           Logger.Instance.Debug("QueueApi OnFault: " + message);
        }
 
        protected void OnItemCompleted(TransStream ts, GenericMessage message, Action<IQueueAck> onCompleted) {

            IQueueAck ack = OnItemCompleted(ts, message);

            onCompleted(ack);
        }

        protected QueueAck OnItemCompleted(TransStream ts, GenericMessage message)
        {

            QueueAck ack = (ts == null || ts.IsEmpty) ? null : ts.ReadValue<QueueAck>(OnFault);

            if (ack == null)
            {
                if (message.DuplexType.IsDuplex())
                    ack = new QueueAck(MessageState.UnExpectedError, "Server was not responsed for this message", message.Identifier, message.Host);
                else
                    ack = new QueueAck(MessageState.Arrived, "Message Arrived on way", message.Identifier, message.Host);

                //ack.HostAddress = message.HostAddress;
            }

            Assists.SetArrived(ack);

            return ack;
        }

        protected bool OnQItemCompleted(TransStream ts, Action<GenericMessage> onCompleted)
        {

            GenericMessage item = OnQItemCompleted(ts);
            if (item != null)
            {
                onCompleted(item);
                return true;
            }
            return false;
        }

        protected GenericMessage OnQItemCompleted(TransStream ts)//, IQueueRequest message)
        {

            GenericMessage item = (ts == null || ts.IsEmpty) ? null : ts.ReadValue<GenericMessage>(OnFault);

            if (item == null)
            {

                return null;
            }

            Assists.SetArrived(item);

            return item;
        }
        #endregion

        #region Publish

        public TransStream PublishItemStream(GenericMessage message, int timeout)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            timeout = EnsureConnectTimeout(timeout);
            EnableRemoteException = true;
            TransStream ts = ExecDuplexStream(message, timeout);
            return ts;
        }

        public IQueueAck PublishItem(GenericMessage message)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            try
            {
                Logger.Instance.Debug("RemoteApi PublishItem : Host:{0}, Identifier:{1}", message.Host, message.Identifier);

                TransStream ts = ExecDuplexStream(message, ConnectTimeout);
                return OnItemCompleted(ts, message);
            }
            catch (Exception ex)
            {
                OnFault("PublishItem error:" + ex.Message);
                return OnItemCompleted(null, message);
            }
        }

        public void PublishItemAsync(GenericMessage message, Action<IAck> ack)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            try
            {
                Logger.Instance.Debug("RemoteApi PublishItem : Host:{0}, Identifier:{1}", message.Host, message.Identifier);

                ExecDuplexStreamAsync(message, ConnectTimeout,(ts)=> {
                    ack(OnItemCompleted(ts, message));
                });
                //return OnItemCompleted(ts, message);
            }
            catch (Exception ex)
            {
                OnFault("PublishItem error:" + ex.Message);
                ack(OnItemCompleted(null, message));
            }
        }

        public IQueueAck PublishItem(GenericMessage message, int timeout)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            try
            {

                TransStream ts = ExecDuplexStream(message, EnsureConnectTimeout(timeout));
                return OnItemCompleted(ts, message);
            }
            catch (Exception ex)
            {
                OnFault("PublishItem error:" + ex.Message);
                return OnItemCompleted(null, message);
            }
        }

        public void PublishItemStream(GenericMessage message, int timeout, Action<TransStream> onCompleted)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;
            EnableRemoteException = true;
            
            Task task = Task.Factory.StartNew(() =>
            {
                ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                {
                    onCompleted(ts);
                    isCompleted = true;
                }, IsAsync);

            });

            task.Wait(WaitTimeout);
        }

        public void PublishItem(GenericMessage message, int timeout, Action<IQueueAck> onCompleted)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;

            try
            {

                Task task = Task.Factory.StartNew(() =>
                {
                    ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                    {
                        OnItemCompleted(ts, message, onCompleted);
                        isCompleted = true;
                    }, IsAsync);

                });

                task.Wait(WaitTimeout);
            }
            catch (Exception ex)
            {
                OnFault("PublishItem error:" + ex.Message);
                OnItemCompleted(null, message, onCompleted);
            }
        }

        public void PublishItemStream(GenericMessage message, int timeout, Action<string> onFault, Action<TransStream> onCompleted)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;

            try
            {

                Task task = Task.Factory.StartNew(() =>
                {
                    ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                    {
                        onCompleted(ts);
                        isCompleted = true;
                    }, IsAsync);

                });

                task.Wait(WaitTimeout);
            }
            catch (Exception ex)
            {
                onFault("PublishItem error:" + ex.Message);
            }
        }

        public void PublishItemStream(GenericMessage message, int timeout, Action<string> onFault, Action<TransStream> onCompleted, CancellationTokenSource cts)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Sending;
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;
            CancellationToken ct = cts.Token;

            try
            {

                Task task = Task.Factory.StartNew(() =>
                {

                    if (ct.WaitCancellationRequested(TimeSpan.FromMilliseconds(WaitTimeout)))
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                    ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                    {
                        onCompleted(ts);
                        isCompleted = true;
                    }, IsAsync);

                    while (!isCompleted)
                    {
                        // Poll on this property if you have to do
                        // other cleanup before throwing.
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            ct.ThrowIfCancellationRequested();
                        }
                        Thread.Sleep(WaitInterval);
                    }

                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                task.Wait(WaitTimeout);
            }
            catch (OperationCanceledException cex)
            {
                //Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {cex.Message}");
                onFault("PublishItem OperationCanceledException:" + cex.Message);
            }
            catch (Exception ex)
            {
                onFault("PublishAsync error:" + ex.Message);
            }
            finally
            {
                cts.Dispose();
            }
        }

        #endregion

        #region Consume

        public IQueueAck __ConsumItem(GenericMessage message, int timeout)
        {
            message.Host = EnsureHost(message.Host);
            message.MessageState = MessageState.Receiving;
            timeout = EnsureConnectTimeout(timeout);

            try
            {

                TransStream ts = ExecDuplexStream(message, timeout);
                return OnItemCompleted(ts, message);
            }
            catch (Exception ex)
            {
                OnFault("ConsumItem error:" + ex.Message);
                return OnItemCompleted(null, message);
            }
        }

        public IQueueMessage ConsumeItem(GenericMessage message, int maxWaitSecond)
        {
            message.Host = EnsureHost(message.Host);
            message.Expiration = maxWaitSecond;
            //message.MessageState = MessageState.Receiving;
            int timeout = 24 * 60 * 60 * 1000;
            try
            {

                TransStream ts = ExecDuplexStream(message, EnsureConnectTimeout(timeout),ReadTimeout);
                return OnQItemCompleted(ts);
            }
            catch (Exception ex)
            {
                OnFault("ConsumeItem error:" + ex.Message);
                return null;// OnQItemCompleted(null, message);
            }
        }

        public void ConsumeItem(GenericMessage message, int maxWaitSecond, Action<GenericMessage> onCompleted)//, IDynamicWait dw)//Action<bool> onAck)
        {
            message.Host = EnsureHost(message.Host);
            message.Expiration = maxWaitSecond;
            //message.MessageState = MessageState.Receiving;
            int timeout = 24 * 60 * 60 * 1000;
            int maxWait = Math.Max(maxWaitSecond, WaitTimeout);
            bool isCompleted = false;
            bool ack = false;
            try
            {

                Task task = Task.Factory.StartNew(() =>
                {
                    ExecDuplexStreamAsync(message, timeout,ReadTimeout, (TransStream ts) =>
                    {
                        if (TransStream.IsEmptyStream(ts))
                        {
                            ack = false;
                        }
                        else
                        {
                            OnQItemCompleted(ts, onCompleted);
                            ack = true;
                        }

                        isCompleted = true;
                    }, IsAsync);
                });

                task.Wait(maxWait);
            }
            catch (Exception ex)
            {
                OnFault("ConsumItem error:" + ex.Message);
                //OnQItemCompleted(null, message, onCompleted);
            }
        }
        #endregion

        #region RequestItem

        public IQueueMessage RequestItem(GenericMessage message, int timeout)
        {
            message.Host = EnsureHost(message.Host);
            //message.MessageState = MessageState.Receiving;

            try
            {
                TransStream ts = ExecDuplexStream(message, EnsureConnectTimeout(timeout), ReadTimeout);
                return OnQItemCompleted(ts);
            }
            catch (Exception ex)
            {
                 OnFault("RequestItem error:" + ex.Message);
                return null;// OnQItemCompleted(null, message);
            }
        }

        //Dequeue DynamicWait was ConsumItem
        public void RequestItem(GenericMessage message, int timeout, Action<GenericMessage> onCompleted, IDynamicWait dw)//Action<bool> onAck)
        {
            message.Host = EnsureHost(message.Host);
            //message.MessageState = MessageState.Receiving;
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;
            bool ack = false;
            try
            {

                Task task = Task.Factory.StartNew(() =>
                {
                    ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                    {
                        if (TransStream.IsEmptyStream(ts))
                        {
                            ack = false;
                        }
                        else
                        {
                            OnQItemCompleted(ts, onCompleted);
                            ack = true;
                        }
                        if (dw != null)
                            dw.DynamicWaitAck(ack);

                        isCompleted = true;
                    }, IsAsync);

                });

                task.Wait(WaitTimeout);
            }
            catch (Exception ex)
            {
                OnFault("RequestItem error:" + ex.Message);
                //OnQItemCompleted(null, message, onCompleted);
            }
        }

         public void RequestItem(GenericMessage message, Action<string> onFault, Action<GenericMessage> onCompleted)
        {
            message.Host = EnsureHost(message.Host);
            bool isCompleted = false;

            try
            {

                Task task = Task.Factory.StartNew(() =>
                {
                    ExecDuplexStreamAsync(message, ConnectTimeout, (TransStream ts) =>
                    {
                        OnQItemCompleted(ts, onCompleted);
                        isCompleted = true;
                    }, IsAsync);

                });
                task.Wait(WaitTimeout);
            }
            catch (Exception ex)
            {
                onFault("RequestItem error:" + ex.Message);
                //OnQItemCompleted(null, message, onCompleted);
            }
        }

        //Used for ManagementApi
        public TransStream RequestItemStream(GenericMessage message, int timeout)
        {
            message.Host = EnsureHost(message.Host);

            try
            {

                TransStream ts = ExecDuplexStream(message, EnsureConnectTimeout(timeout), ReadTimeout);
                return ts;
            }
            catch (Exception ex)
            {
                OnFault("RequestItem error:" + ex.Message);
                return null;
            }
        }

        public void ConsumeItemStream(GenericMessage message, int timeout, Action<string> onFault, Action<TransStream> onCompleted)
        {
            message.Host = EnsureHost(message.Host);
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;

            try
            {

                Task task = Task.Factory.StartNew(() =>
                {
                    ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                    {
                        onCompleted(ts);
                        isCompleted = true;
                    }, IsAsync);
                });

                task.Wait(WaitTimeout);
            }
            catch (Exception ex)
            {
                onFault("RequestItemStream error:" + ex.Message);
            }
        }

        public void RequestItemStream(GenericMessage message, int timeout, Action<string> onFault, Action<TransStream> onCompleted, CancellationTokenSource cts)
        {
            message.Host = EnsureHost(message.Host);
            timeout = EnsureConnectTimeout(timeout);
            bool isCompleted = false;
            CancellationToken ct = cts.Token;

            try
            {

                Task task = Task.Factory.StartNew(() =>
                {

                    if (ct.WaitCancellationRequested(TimeSpan.FromMilliseconds(WaitTimeout)))
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                    ExecDuplexStreamAsync(message, timeout, (TransStream ts) =>
                    {
                        onCompleted(ts);
                        isCompleted = true;
                    }, IsAsync);

                    while (!isCompleted)
                    {
                        // Poll on this property if you have to do
                        // other cleanup before throwing.
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            ct.ThrowIfCancellationRequested();
                        }
                        Thread.Sleep(WaitInterval);
                    }

                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                task.Wait(WaitTimeout);
            }
            catch (OperationCanceledException cex)
            {
                //Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {cex.Message}");
                onFault("RequestItemStream OperationCanceledException:" + cex.Message);
            }
            catch (Exception ex)
            {
                onFault("RequestItemStream error:" + ex.Message);
            }
            finally
            {
                cts.Dispose();
            }
        }

         #endregion

        internal void SendOut(GenericMessage message)
        {
            //message.Host = this._QueueName;
            //GenericMessage qs = new GenericMessage(message);

            switch (Protocol)
            {
                case NetProtocol.Http:
                    HttpClient.SendOut(message, RemoteHostAddress, RemoteHostPort, HttpMethod, ConnectTimeout, EnableRemoteException);
                    break;
                case NetProtocol.Pipe:
                    PipeClient.SendOut(message, RemoteHostAddress, EnableRemoteException, IsAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None);
                    break;
                case NetProtocol.Tcp:
                default:
                    TcpStreamClient.SendOut(message, RemoteHostAddress, RemoteHostPort, ConnectTimeout, IsAsync, EnableRemoteException);
                    break;
            }
        }

        #region message json

        public string SendHttpJsonDuplex(GenericMessage message, bool pretty = false)
        {
            string response = null;

            message.TransformType = TransformType.Json;
            //message.IsDuplex = true;
            message.DuplexType = DuplexTypes.Respond;
            response = HttpClient.SendDuplexJson(message, RemoteHostAddress, false);
            //response = HttpClientCache.SendDuplexJson(message, RemoteHostName, false);

            if (pretty)
            {
                if (response != null)
                    response = JsonSerializer.Print(response);
            }
            return response;
        }

        public void SendHttpJsonOut(GenericMessage message)
        {
            HttpClient.SendOutJson(message, RemoteHostAddress, false);
            //HttpClientCache.SendOut(message, RemoteHostName, false);
        }

        #endregion

        #region Exec GenericMessage Stream 

        public void ExecDuplexStreamAsync(GenericMessage message, int connectTimeout,  Action<TransStream> onCompleted, bool isChannelAsync = false)
        {
            message.TransformType = TransformType.Stream;

            switch (Protocol)
            {
                case NetProtocol.Http:
                    HttpClient.SendDuplexStreamAsync(message, RemoteHostAddress, RemoteHostPort, HttpMethod, ConnectTimeout, onCompleted, EnableRemoteException);
                    break;
                case NetProtocol.Pipe:
                    //ChannelSettings.IsAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None
                    PipeClient.SendDuplexStreamAsync(message, RemoteHostAddress, onCompleted, EnableRemoteException, isChannelAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None);
                    break;
                case NetProtocol.Tcp:
                    TcpStreamClient.SendDuplexStreamAsync(message, RemoteHostAddress, RemoteHostPort, connectTimeout, onCompleted, isChannelAsync, EnableRemoteException);
                    break;
            }
        }

        public TransStream ExecDuplexStream(GenericMessage message, int connectTimeout, bool isAsync = false)
        {
            message.TransformType = TransformType.Stream;

            switch (Protocol)
            {
                case NetProtocol.Http:
                    return HttpClient.SendDuplexStream(message, RemoteHostAddress, RemoteHostPort,HttpMethod, ConnectTimeout, EnableRemoteException);

                case NetProtocol.Pipe:
                    //ChannelSettings.IsAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None
                    return PipeClient.SendDuplexStream(message, RemoteHostAddress, EnableRemoteException, isAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None);

                case NetProtocol.Tcp:
                    break;
            }
            return TcpStreamClient.SendDuplexStream(message, RemoteHostAddress, RemoteHostPort, connectTimeout, isAsync, EnableRemoteException);
        }
        #endregion

        #region Exec RequestItem Stream 

        public void ExecDuplexStreamAsync(GenericMessage message, int connectTimeout,int readTimeout, Action<TransStream> onCompleted, bool isChannelAsync = false)
        {
            message.TransformType = TransformType.Stream;

            switch (Protocol)
            {
                case NetProtocol.Http:
                    HttpClient.SendDuplexStreamAsync(message, RemoteHostAddress, RemoteHostPort, HttpMethod, ConnectTimeout, onCompleted, EnableRemoteException);
                    break;
                case NetProtocol.Pipe:
                    //ChannelSettings.IsAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None
                    PipeClient.SendDuplexStreamAsync(message, RemoteHostAddress, onCompleted, EnableRemoteException, isChannelAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None);
                    break;
                case NetProtocol.Tcp:
                    TcpStreamClient.SendDuplexStreamAsync(message, RemoteHostAddress, RemoteHostPort, connectTimeout, readTimeout,onCompleted, isChannelAsync, EnableRemoteException);
                    break;
            }
        }

        public void ExecDuplexStreamAsync(GenericMessage message, int connectTimeout, Action<TransStream> onCompleted, bool isChannelAsync = false)
        {
            message.TransformType = TransformType.Stream;

            switch (Protocol)
            {
                case NetProtocol.Http:
                    HttpClient.SendDuplexStreamAsync(message, RemoteHostAddress, RemoteHostPort, HttpMethod, ConnectTimeout, onCompleted, EnableRemoteException);
                    break;
                case NetProtocol.Pipe:
                    //ChannelSettings.IsAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None
                    PipeClient.SendDuplexStreamAsync(message, RemoteHostAddress, onCompleted, EnableRemoteException, isChannelAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None);
                    break;
                case NetProtocol.Tcp:
                    TcpStreamClient.SendDuplexStreamAsync(message, RemoteHostAddress, RemoteHostPort, connectTimeout, onCompleted, isChannelAsync, EnableRemoteException);
                    break;
            }
        }
        
        public TransStream ExecDuplexStream(GenericMessage message, int connectTimeout, int readTimeout, bool isAsync = false)
        {
            message.TransformType = TransformType.Stream;

            switch (Protocol)
            {
                case NetProtocol.Http:
                    return HttpClient.SendDuplexStream(message, RemoteHostAddress, RemoteHostPort, HttpMethod, ConnectTimeout, EnableRemoteException);

                case NetProtocol.Pipe:
                    //ChannelSettings.IsAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None
                    return PipeClient.SendDuplexStream(message, RemoteHostAddress, EnableRemoteException, isAsync ? System.IO.Pipes.PipeOptions.Asynchronous : System.IO.Pipes.PipeOptions.None);

                case NetProtocol.Tcp:
                    break;
            }
            //return TcpClient.SendDuplexStream(message, RemoteHostAddress, RemoteHostPort, connectTimeout, isAsync, EnableRemoteException);
            using (TcpStreamClient client = new TcpStreamClient(RemoteHostAddress, RemoteHostPort, connectTimeout, readTimeout, isAsync))
            {
                message.TransformType = TransformType.Stream;
                //message.IsDuplex = true;
                message.DuplexType = DuplexTypes.Respond;
                return client.Execute<TransStream>(message, EnableRemoteException);
            }
        }
        #endregion

    }
}
