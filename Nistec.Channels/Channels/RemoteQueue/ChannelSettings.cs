using Nistec.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Channels.RemoteQueue
{
    public class ChannelSettings
    {

        public static NetProtocol DefaultProtocol = NetProtocol.Pipe;
        //public const bool DefaultIsAsync = false;

        public const string DefaultHttpMethod = "post";
        //public const int DefaultHttpTimeout = 10000;

        //public const int TcpPort = 10000;
        //public const int DefaultTcpTimeout = 10000;



        //public const string RemoteQueueHostName ="";
        public const bool DefaultEnableRemoteException = false;

        public string _HttpMethod = DefaultHttpMethod;
        public string HttpMethod { get { return _HttpMethod; } set { _HttpMethod = value; } }
        //==================================================

        public const int DefaultConnectTimeout = 6000;
        public const int DefaultReadTimeout = 180000;
        public const int DefaultWaitTimeout = 180000;
        public const int DefaultWaitInterval = 100;


        //protected NetProtocol Protocol;
        //protected string RemoteHostAddress;
        //protected int RemoteHostPort;
        //protected bool EnableRemoteException;


        public string RemoteHostAddress { get; protected set; }
        public int RemoteHostPort { get; protected set; }
        public bool EnableRemoteException { get; protected set; }

        NetProtocol _Protocol= NetProtocol.Pipe;
        public NetProtocol Protocol { get { return _Protocol; } protected set { _Protocol = value; } }


        bool _IsAsync = false;
        public bool IsAsync { get { return _IsAsync; } set { _IsAsync = value; } }
        int _MaxRetry = 1;
        public int MaxRetry { get { return _MaxRetry; } set { _MaxRetry = value <=0 ? 1: value > 5 ? 5: value; } }

        int _WaitInterval = DefaultWaitInterval;
        public int WaitInterval { get { return _WaitInterval; } set { _WaitInterval = value <= 10 ? DefaultWaitInterval : value; } }
        int _ConnectTimeout = DefaultConnectTimeout;
        public int ConnectTimeout { get { return _ConnectTimeout; } set { _ConnectTimeout = (value <= 0 ? DefaultConnectTimeout : value); } }
        int _ReadTimeout = DefaultReadTimeout;
        public int ReadTimeout { get { return _ReadTimeout; } set { _ReadTimeout = ((value == 0 ) ? DefaultReadTimeout : value); } }//|| value < -1
        int _WaitTimeout = DefaultWaitTimeout;
        public int WaitTimeout { get { return _WaitTimeout; } set { _WaitTimeout = ((value == 0 ) ? DefaultWaitTimeout : value); } }//|| value <= 0

        public int EnsureConnectTimeout(int timeout)
        {

            if (timeout <= 0)
                return ConnectTimeout;
            return timeout;
        }
        public string EnsureHost(string host)
        {
            return (host == null || host == "") ? QueueName : host;
        }
        //public string QueueName
        //{
        //    get { return _QueueName; }
        //}

        public string QueueName { get; protected set; }
        #region members

        /// <summary>
        /// No limit timeout.
        /// </summary>
        public const int InfiniteTimeout = 0;
        /// <summary>
        /// 5 minute timeout.
        /// </summary>
        public const int ShortTimeout = 307200;//5 minute
        /// <summary>
        /// 30 minute timeout.
        /// </summary>
        public const int LongTimeout = 1843200;//30 minute

        //string _ServerName = ".";
        //string _HostAddress;
       // protected HostProtocol _HostProtocol;
        public HostProtocol HostProtocol { get; protected set; }
        public bool IsCoverable { get; set; }

        #endregion

    }
}
