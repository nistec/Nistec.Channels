  using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nistec.Channels;
using Nistec.Generic;
using System.Collections;
using Nistec.Runtime;
using System.IO.Pipes;
using Nistec.IO;
using Nistec.Serialization;
using Nistec.Data;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Nistec.Threading;

namespace Nistec.Channels.RemoteQueue
{
    /// <summary>
    /// Represent Queue Api for client.
    /// </summary>
    public class QueueApi : RemoteApi, IQueueClient
    {

        #region members

        CancellationTokenSource canceller = new CancellationTokenSource();

        #endregion

        #region ctor


        public QueueApi(NetProtocol protocol = NetProtocol.Tcp, int connectTimeout = 0)
        {
            if (protocol == NetProtocol.NA)
            {
                protocol = ChannelSettings.DefaultProtocol;
            }
            Protocol = protocol;
            ConnectTimeout = (connectTimeout <= 0) ? DefaultConnectTimeout : connectTimeout;
            //RemoteHostName = ChannelSettings.RemoteQueueHostName;
            EnableRemoteException = ChannelSettings.DefaultEnableRemoteException;
        }

        public QueueApi(string queueName, string hostAddress) 
            : this()
        {
            var qh = HostChannel.Parse(hostAddress);

            QueueName = queueName;
            HostProtocol = qh.Protocol;
            RemoteHostAddress = qh.HostAddress;
            RemoteHostPort = qh.Port;
            Protocol = qh.Protocol.GetProtocol();
        }
        public QueueApi(string queueName, HostProtocol protocol, string endpoint, int port, string hostName) 
            : this()
        {
            QueueName = queueName;
            HostProtocol = protocol;
            RemoteHostAddress = endpoint;// HostChannel.GetRawAddress(protocol,serverName,port, hostName);
            RemoteHostPort = port;
            Protocol = protocol.GetProtocol();
        }

        public QueueApi(HostChannel host) 
            : this()
        {
            QueueName = host.HostName;
            HostProtocol = host.Protocol;
            RemoteHostAddress = host.Endpoint;
            RemoteHostPort = host.Port;
            Protocol = host.NetProtocol;
        }

        public static QueueApi Get(string hostAddress, int connectTimeout = 0, bool isAsync=false)
        {
            var host = HostChannel.Parse(hostAddress);
            var api = new QueueApi(host);
            api.IsAsync = isAsync;
            api.ConnectTimeout = connectTimeout;
            return api;
        }
        public static QueueApi Get(HostChannel host, int connectTimeout = 0, bool isAsync = false)
        {
            var api = new QueueApi(host);
            api.IsAsync = isAsync;
            api.ConnectTimeout = connectTimeout;
            return api;
        }

        #endregion

        #region Enqueue

        public IQueueAck Enqueue(GenericMessage message, int connectTimeout=0)
        {
            message.Command = "Enqueue";
            //message.Host = this._QueueName;

            return PublishItem(message, EnsureConnectTimeout(connectTimeout));

        }

        public void EnqueueAsync(GenericMessage message, int connectTimeout, Action<IQueueAck> onCompleted)
        {
            message.Command = "Enqueue";

            PublishItem(message, EnsureConnectTimeout(connectTimeout), onCompleted);
        }
        #endregion

        #region Dequeue

        public GenericMessage Dequeue(int connectTimeout = 0)
        {
            GenericMessage message = new GenericMessage()
            {
                Command = "Dequeue",
                Host = QueueName,
                DuplexType = DuplexTypes.Respond
            };

            return RequestItem(message, connectTimeout);

        }

        public GenericMessage Dequeue(GenericMessage message, int connectTimeout=0)
        {
            message.Command = "Dequeue";
            //message.Host = this._QueueName;

            return RequestItem(message, connectTimeout);

        }
 
        public void DequeueAsync(GenericMessage message, int connectTimeout, Action<GenericMessage> onCompleted, IDynamicWait aw)
        {
            message.Command = "Dequeue";
            //message.Host = this._QueueName;
            //message.MessageState = MessageState.Sending;

            RequestItem(message, connectTimeout, onCompleted, aw);

        }
        public GenericMessage Dequeue(Priority priority)
        {
            GenericMessage request = new GenericMessage()//_QueueName, QueueCmd.DequeuePriority, null);
            {
                Host = QueueName,
                Command = QueueCmd.DequeuePriority.ToString()
            };
            request.Priority = priority;

            return Dequeue(request);
        }

        #endregion

        #region Consume
        public GenericMessage Consume(int maxWaitSecond)
        {
            GenericMessage message = new GenericMessage()
            {
                Command = QueueCmd.Consume.ToString(),
                Host = QueueName,
                DuplexType = DuplexTypes.Respond
            };

            return ConsumeItem(message, maxWaitSecond);

        }
        #endregion

        #region Peek

        public GenericMessage Peek(int connectTimeout = 0)
        {
            GenericMessage message = new GenericMessage()
            {
                Command = QueueCmd.Peek.ToString(),
                Host = QueueName,
            };

            return RequestItem(message, connectTimeout);

        }

        public GenericMessage Peek(GenericMessage message, int connectTimeout=0)
        {
            message.Command = QueueCmd.Peek.ToString();
            //message.Host = this._QueueName;

            return RequestItem(message, connectTimeout);

        }

        public void PeekAsync(GenericMessage message, int connectTimeout, Action<GenericMessage> onCompleted)
        {
            message.Command = QueueCmd.Peek.ToString();
            RequestItem(message, connectTimeout, onCompleted, DynamicWait.Empty);

        }
        #endregion

        #region Send

        public IQueueAck SendAsync(GenericMessage message, int connectTimeout)
        {
            using (

                    Task<IQueueAck> task = Task<IQueueAck>.Factory.StartNew(() =>
                        PublishItem(message, EnsureConnectTimeout(connectTimeout))
                    ,
                    canceller.Token,
                    TaskCreationOptions.None,
                    TaskScheduler.Default))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    IQueueAck ack = task.Result;
                    return ack;
                }
                else if (task.IsCanceled)
                {
                    return new QueueAck(MessageState.OperationCanceled, message);
                }
                else if (task.IsFaulted)
                {
                    return new QueueAck(MessageState.OperationFailed, message);
                }
                else
                {
                    return new QueueAck(MessageState.UnExpectedError, message);
                }
            }
        }

        public void SendAsync(GenericMessage message, int connectTimeout, Action<IQueueAck> action)
        {
            using (

                    Task<IQueueAck> task = Task<IQueueAck>.Factory.StartNew(() =>
                        PublishItem(message, EnsureConnectTimeout(connectTimeout))
                    ,
                    canceller.Token,
                    TaskCreationOptions.None,
                    TaskScheduler.Default))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    action(task.Result);
                }
                else if (task.IsCanceled)
                {
                    if (action != null)
                        action(new QueueAck(MessageState.OperationCanceled, message));
                }
                else if (task.IsFaulted)
                {
                    if (action != null)
                        action(new QueueAck(MessageState.OperationFailed, message));
                }
                else
                {
                    if (action != null)
                        action(new QueueAck(MessageState.UnExpectedError, message));
                }
            }
        }

        public Task<IQueueAck> SendAsyncTask(GenericMessage message, int connectTimeout)
        {
            Task<IQueueAck> task = Task<IQueueAck>.Factory.StartNew(() =>
                PublishItem(message, EnsureConnectTimeout(connectTimeout))
            ,
            canceller.Token,
            TaskCreationOptions.None,
            TaskScheduler.Default);
                task.Wait();
            return task;
        }
        #endregion

        #region Report

        public GenericMessage Report(QueueCmdReport command, string host)
        {
            GenericMessage request = new GenericMessage()
            {
                Command =command.ToString(),// (QueueCmd)(int)command,
                Host = host
            };

            var ack = ConsumeItem(request, ConnectTimeout);
            if (ack == null)
            {
                ack = new GenericMessage()//MessageState.UnExpectedError, "Server was not responsed for this message", command.ToString(), host);
                {
                    MessageState = MessageState.UnExpectedError,
                    Label = "Server was not responsed for this message",
                    Host = host
                };
            }
            return ack;
        }

        public void ReportAsync(GenericMessage command, string host, Action<GenericMessage> action)
        {
            using (

                    Task<GenericMessage> task = Task<GenericMessage>.Factory.StartNew(() =>
                        Report(command, host)
                    ,
                    canceller.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default))
            {
                task.Wait();
                if (task.IsCompleted)
                {
                    GenericMessage item = task.Result;
                    if (item != null)
                    {
                        if (action != null)
                            Task.Factory.StartNew(() => action(item));
                    }
                }
                else if (task.IsCanceled)
                {

                }
                else if (task.IsFaulted)
                {

                }
            }
        }
        #endregion

        #region Commit/Abort/Report

        public void Commit(Ptr ptr)
        {

            var message = new GenericMessage(ptr)
            {
                Host = ptr.Host,
                Command = QueueCmd.Commit.ToString(),
                MessageState= MessageState.TransCommited,
                //Identifier=ptr.Identifier,
                Retry = (byte)ptr.Retry
            };
            base.SendOut(message);
        }

        public void Abort(Ptr ptr)
        {
            var message = new GenericMessage(ptr)
            {
                Host = ptr.Host,
                Command = QueueCmd.Abort.ToString(),
                MessageState = MessageState.TransAborted,
                //Identifier = ptr.Identifier,
                Retry=(byte)ptr.Retry
            };
            //client.Exec(message, QueueCmd.Abort);
            base.SendOut(message);
        }


        public GenericMessage Report(GenericMessage cmd)
        {
            GenericMessage request = new GenericMessage()
            {
                Host = QueueName,
                Command = cmd.ToString()// (QueueCmd)(int)cmd,
                //Command = (QueueCmd)(int)cmd
            };
            var response = RequestItem(request, ConnectTimeout);
            return response;// == null ? null : response.ToMessage();
            //ReportApi client = new ReportApi(QueueDefaults.QueueManagerPipeName, true);
            //return (Message)client.Exec(message, (QueueCmd)(int)cmd);
        }

        public T Report<T>(GenericMessage cmd)
        {
            GenericMessage request = new GenericMessage()
            {
                Host = QueueName,
                Command = cmd.ToString()// (QueueCmd)(int)cmd,
                //Command = (QueueCmd)(int)cmd
            };
            var res = RequestItem(request, ConnectTimeout);
            //var res= response == null ? null : response.ToMessage();

            //ReportApi client = new ReportApi(QueueDefaults.QueueManagerPipeName, true);
            //var res = client.Exec(message, (QueueCmd)(int)cmd);
            if (res == null)
                return default(T);
            return res.GetBody<T>();
        }

        public GenericMessage OperateQueue(QueueCmdOperation cmd)
        {
            GenericMessage message = new GenericMessage()//queueName, (QueueCmd)(int)cmd)
            {
                Host = QueueName,
                Command = cmd.ToString()//(QueueCmd)(int)cmd,
                //Command = (QueueCmd)(int)cmd
            };
            var response= RequestItem(message, ConnectTimeout);
            return response;//==null? null: response.ToMessage();
        }

        public GenericMessage AddQueue(QProperties qp)
        {
            var message = new GenericMessage()
            {
                Host = QueueName,
                Command = QueueCmd.AddQueue.ToString(),
            };

            message.SetBody(qp.GetEntityStream(false), qp.GetType().FullName);
            var response = RequestItem(message, ConnectTimeout);
            return response;// == null ? null : response.ToMessage();
        }

        public GenericMessage AddQueue(CoverMode mode, bool isTrans, bool isTopic)
        {
           

            QProperties qp = new QProperties()
            {
                QueueName = QueueName,
                ServerPath = "localhost",
                Mode = mode,
                IsTrans = isTrans,
                MaxRetry = QueueDefaults.DefaultMaxRetry,
                ReloadOnStart = false,
                ConnectTimeout = 0,
                TargetPath = "",
                IsTopic=isTopic
            } ;
            return AddQueue(qp);
        }

        public GenericMessage RemoveQueue()
        {
            GenericMessage message = new GenericMessage()
            {
                Host = QueueName,
                Command = QueueCmd.RemoveQueue.ToString(),
            };
            var response= RequestItem(message, ConnectTimeout);
            return response;// == null ? null : response.ToMessage();
        }

        public GenericMessage QueueExists()
        {
            GenericMessage message = new GenericMessage()
            {
                Host = QueueName,
                Command = QueueCmd.Exists.ToString(),
            };
            var response= RequestItem(message, ConnectTimeout);
            return response;// == null ? null : response.ToMessage();
        }


        #endregion
   
        #region  asyncInvoke

        private AsyncCallback onRequestCompleted;
        private ManualResetEvent resetEvent;
        public event ReceiveMessageCompletedEventHandler ReceiveCompleted;

        private ManualResetEvent ResetEvent
        {
            get
            {
                if (resetEvent == null)
                    resetEvent = new ManualResetEvent(false);
                return resetEvent;
            }
        }

        protected virtual void OnReceiveCompleted(ReceiveMessageCompletedEventArgs e)
        {
            if (ReceiveCompleted != null)
                ReceiveCompleted(this, e);
        }


        private GenericMessage ReceiveItemWorker(TimeSpan timeout, object state)
        {
            GenericMessage item = null;
            TimeOut to = new TimeOut(timeout);
            while (item == null)
            {
                if (to.IsTimeOut())
                {
                    state = (int)ReceiveState.Timeout;
                    break;
                }
                item = Dequeue();// this.Receive();
                if (item == null)
                {
                    Thread.Sleep(100);
                }
            }
            if (item != null)
            {
                state = (int)ReceiveState.Success;
                Console.WriteLine("Dequeue item :{0}", item.Identifier);
            }
            return item;
        }

        public GenericMessage AsyncReceive()
        {
            return AsyncReceive(null);
        }

        public GenericMessage AsyncReceive(object state)
        {
            if (state == null)
            {
                state = new object();
            }
            TimeSpan timeout = TimeSpan.FromMilliseconds(QueueApi.LongTimeout);
            ReceiveMessageCallback caller = new ReceiveMessageCallback(this.ReceiveItemWorker);

            // Initiate the asychronous call.
            IAsyncResult result = caller.BeginInvoke(timeout, state, CreateCallBack(), caller);

            result.AsyncWaitHandle.WaitOne();

            // Call EndInvoke to wait for the asynchronous call to complete,
            // and to retrieve the results.
            GenericMessage item = caller.EndInvoke(result);
            AsyncCompleted(item);
            return item;

        }

        public IAsyncResult BeginReceive(object state)
        {
            return BeginReceive(TimeSpan.FromMilliseconds(QueueApi.LongTimeout), state, null);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, object state)
        {
            return BeginReceive(timeout, state, null);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, object state, AsyncCallback callback)
        {

            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if ((totalMilliseconds < 0L) || (totalMilliseconds > 4294967295L))
            {
                throw new ArgumentException("InvalidParameter", "timeout");
            }
            ReceiveMessageCallback caller = new ReceiveMessageCallback(ReceiveItemWorker);

            if (callback == null)
            {
                callback = CreateCallBack();
            }
            if (state == null)
            {
                state = new object();
            }
            state = (int)ReceiveState.Wait;

            IAsyncResult result = caller.BeginInvoke(timeout, state, callback, caller);

            this.ResetEvent.Set();
            return result;
        }


        // Callback method must have the same signature as the
        // AsyncCallback delegate.
        public GenericMessage EndReceive(IAsyncResult asyncResult)
        {

            // Retrieve the delegate.
            ReceiveMessageCallback caller = (ReceiveMessageCallback)asyncResult.AsyncState;

            // Call EndInvoke to retrieve the results.
            GenericMessage item = (GenericMessage)caller.EndInvoke(asyncResult);

            AsyncCompleted(item);
            this.ResetEvent.WaitOne();
            return item;
        }

        private AsyncCallback CreateCallBack()
        {
            if (this.onRequestCompleted == null)
            {
                this.onRequestCompleted = new AsyncCallback(this.OnRequestCompleted);
            }
            return this.onRequestCompleted;
        }

        private void AsyncCompleted(GenericMessage item)
        {
            //if (item != null)
            //{
            //    if (item != null && IsTrans)
            //    {
            //        //this.TransBegin(item);
            //    }
            //    else
            //    {
            //        this.Completed(item.ItemId, (int)ItemState.Commit);
            //    }
            //}
        }

        private void OnRequestCompleted(IAsyncResult asyncResult)
        {
            OnReceiveCompleted(new ReceiveMessageCompletedEventArgs(this, asyncResult));
        }

        #endregion


    }
}
