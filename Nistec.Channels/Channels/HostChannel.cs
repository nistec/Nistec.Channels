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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nistec.Runtime;
using Nistec.Serialization;
using Nistec.Channels;
#pragma warning disable CS1591
namespace Nistec.Channels
{


    public enum HostProtocol : byte
    {
        local = 0,
        ipc = 1,
        tcp = 2,
        http = 3,
        file = 4,
        db = 5
    }
    /// <summary>
    /// HostChannel settings
    /// </summary>
    [Serializable]
    public class HostChannel //: ISerialEntity,IDisposable
    {

        #region ctor

        protected HostChannel()
        {
            Segments = new string[5];
        }
        /// <summary>
        /// tcp:localhost:1500?host[timeout,buffer]
        /// </summary>
        /// <param name="address">tcp:localhost:1500?host[timeout,buffer]</param>
        public HostChannel(string address)
        {
            Segments = new string[5];
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            string[] args = address.Replace("//", "").TrimStart('/').Split(':', '/', '?', '&');

            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid hostAddress");
            }

            for (int i = 0; i < args.Length; i++)
            {
                Segments[i] = args[i];
            }
            Port = Types.ToInt(Segments[2]);
            Protocol = GetProtocol(Segments[0]);
            RawHostAddress = GetRawAddress(Protocol, Segments[1], Segments[2], Segments[3]);
        }

        public HostChannel(HostProtocol protocol, string serverAddress, string hostPort, string hostName)
        {
            Segments = new string[5];
            Segments[0] = protocol.ToString();
            Segments[1] = serverAddress;
            Segments[2] = hostPort;
            Segments[3] = hostName;
            Port = Types.ToInt(Segments[2]);
            Protocol = protocol;
            RawHostAddress = GetRawAddress(Protocol, serverAddress, hostPort, hostName);
        }

        public HostChannel(HostProtocol protocol, string address, string hostName)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            Segments = new string[5];
            Segments[0] = protocol.ToString();

            string[] args = address.Replace("//", "").TrimStart('/').Split(':');

            Segments[1] = args[0];
            Segments[2] = (args.Length > 1) ? args[1]:"";
            Segments[3] = hostName;
            Port = Types.ToInt(Segments[2]);
            Protocol = protocol;
            RawHostAddress = GetRawAddress(Protocol, Segments[1], Segments[2], hostName);
        }

        #endregion

        #region properties
        public string HostId
        {
            get
            {
                return string.Format("{0}-{1}-{2}", HostAddress, HostPort, HostName);
            }
        }

        public string[] Segments { get; protected set; }
        public string RawHostAddress
        {
            get; protected set;
        }
        public HostProtocol Protocol
        {
            get; protected set;
        }

        public int Port
        {
            get; protected set;
        }
        public string HostProtcol { get { return Segments[0]; } }
        public string HostAddress { get { return Segments[1]; } }
        public string HostPort { get { return Segments[2]; } }
        public string HostName { get { return Segments[3]; } }
        public string HostArgs { get { return Segments[4]; } }

        /// <summary>
        /// Get indicate wether this host can distrebute.
        /// </summary>
        public bool CanDistribute
        {
            get { return !string.IsNullOrEmpty(RawHostAddress) && RawHostAddress.StartsWith("tcp:"); }
        }

        /// <summary>
        /// Get indicate wether this host is local.
        /// </summary>
        public bool IsLocal
        {
            get { return Types.NZ(HostAddress, ".") == "."; }
        }
        #endregion

        #region Convert

        public NetProtocol NetProtocol
        {
            get
            {
                switch (Protocol)
                {
                    case HostProtocol.ipc:
                        return NetProtocol.Pipe;
                    case HostProtocol.tcp:
                        return NetProtocol.Tcp;
                    case HostProtocol.http:
                        return NetProtocol.Http;
                    default:
                        return NetProtocol.NA;
                }
            }
        }

        public string NetAddress
        {
            get
            {
                switch (Protocol)
                {

                    case HostProtocol.ipc://ipc:.:nistec_queue
                        return string.Format("{0}/{1}", HostAddress, HostPort);
                    case HostProtocol.file://file:root/folder
                        return string.Format("{0}/{1}", HostAddress, HostPort);
                    case HostProtocol.tcp://tcp:127.0.0.1:9015
                        return string.Format("{0}:{1}", HostAddress, Port);
                    case HostProtocol.http://127.0.0.1:9015
                        return string.Format("{0}:{1}", HostAddress, Port);
                    case HostProtocol.db://db:serve/catalog
                        return string.Format("{0}/{1}", HostAddress, HostPort);
                    default:
                        throw new Exception("Incorrect address or HostProtocol not supported");
                }
            }
        }

        #endregion

        #region parse

        public static HostChannel Parse(string hostAddress)
        {
            HostChannel host = new HostChannel(hostAddress);
            return host;
        }

        public static HostProtocol GetProtocol(string protocol)
        {

            switch (protocol)
            {

                case "ipc"://ipc:.:nistec_queue
                    return HostProtocol.ipc;
                case "tcp"://tcp:127.0.0.1:9015
                    return HostProtocol.tcp;
                case "http"://127.0.0.1:9015
                    return HostProtocol.http;
                case "file"://file:root/folder
                    return HostProtocol.file;
                case "db"://db:serve/catalog
                    return HostProtocol.db;
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
            }

        }

        public static string GetRawAddress(HostProtocol protocol, string serverAddress, string hostPort, string hostName)
        {
            if (string.IsNullOrEmpty(hostName))
                return GetRawAddress(protocol.ToString(), serverAddress, hostPort);
            else
                return GetRawAddress(protocol.ToString(), serverAddress, hostPort, hostName);

            //switch (protocol)
            //{
            //    case HostProtocol.ipc://ipc:.:nistec_queue/hostName
            //        return string.Format("ipc:{0}/{1}/{2}", serverAddress, hostPort, hostName);
            //    case HostProtocol.tcp://tcp:127.0.0.1:9015/hostName
            //        return string.Format("tcp:{0}:{1}/{2}", serverAddress, hostPort, hostName);
            //    case HostProtocol.http://http://127.0.0.1:9015/hostName
            //        return string.Format("http://{0}:{1}/{2}", serverAddress, hostPort, hostName);
            //    case HostProtocol.file://file:root/folder/hostName
            //        return string.Format("file:{0}/{1}/{2}", serverAddress, hostPort, hostName);
            //    case HostProtocol.db://db:serve/catalog/hostName
            //        return string.Format("db:{0}/{1}/{2}", serverAddress, hostPort, hostName);
            //    default:
            //        throw new Exception("Incorrect address or HostProtocol not supported");
            //}
        }
        private static string GetRawAddress(string protocol, string serverAddress, string hostPort)
        {
            switch (protocol)
            {
                case "ipc"://ipc:.:nistec_queue/hostName
                    return string.Format("ipc:{0}/{1}", serverAddress, hostPort);
                case "tcp"://tcp:127.0.0.1:9015/hostName
                    return string.Format("tcp:{0}:{1}", serverAddress, hostPort);
                case "http"://http://127.0.0.1:9015/hostName
                    return string.Format("http://{0}:{1}", serverAddress, hostPort);
                case "file"://file:root/folder/hostName
                    return string.Format("file:{0}/{1}", serverAddress, hostPort);
                case "db"://db:serve/catalog/hostName
                    return string.Format("db:{0}/{1}", serverAddress, hostPort);
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
            }
        }
        private static string GetRawAddress(string protocol, string serverAddress, string hostPort, string hostName)
        {
            switch (protocol)
            {
                case "ipc"://ipc:.:nistec_queue/hostName
                    return string.Format("ipc:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                case "tcp"://tcp:127.0.0.1:9015/hostName
                    return string.Format("tcp:{0}:{1}/{2}", serverAddress, hostPort, hostName);
                case "http"://http://127.0.0.1:9015/hostName
                    return string.Format("http://{0}:{1}/{2}", serverAddress, hostPort, hostName);
                case "file"://file:root/folder/hostName
                    return string.Format("file:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                case "db"://db:serve/catalog/hostName
                    return string.Format("db:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
            }
        }

        private static string GetRawAddress(string[] segments)
        {
            if(segments==null || segments.Length < 3)
            {
                throw new ArgumentException("segments is null or incorrect");
            }
            if (segments.Length < 4 || string.IsNullOrEmpty(segments[3]))
                return GetRawAddress(segments[0], segments[1], segments[2]);
            else
                return GetRawAddress(segments[0], segments[1], segments[2], segments[3]);

            //switch (segments[0])
            //{

            //    case "ipc"://ipc:.:nistec_queue/hostName
            //        return string.Format("ipc:{0}/{1}/{2}", segments[1], segments[2], segments[3]);
            //    case "tcp"://tcp:127.0.0.1:9015/hostName
            //        return string.Format("tcp:{0}:{1}/{2}", segments[1], segments[2], segments[3]);
            //    case "http"://http://127.0.0.1:9015/hostName
            //        return string.Format("http://{0}:{1}/{2}", segments[1], segments[2], segments[3]);
            //    case "file"://file:root/folder/hostName
            //        return string.Format("file:{0}/{1}/{2}", segments[1], segments[2], segments[3]);
            //    case "db"://db:serve/catalog/hostName
            //        return string.Format("db:{0}/{1}/{2}", segments[1], segments[2], segments[3]);
            //    default:
            //        throw new Exception("Incorrect address or HostProtocol not supported");
            //}
        }

        #endregion

        #region assists

        /// <summary>
        /// Get or Set Endpoint Address
        /// </summary>
        public string Endpoint
        {
            get
            {
                return HostAddress;

                //switch (Protocol)
                //{
                //    case HostProtocol.ipc:
                //        return HostAddress;
                //    case HostProtocol.tcp:
                //        return HostAddress;
                //    case HostProtocol.http:
                //        return HostAddress;
                //    default:
                //        return HostAddress;
                //}
            }
        }

        //public bool IsPingOk { get; private set; }
        public bool PingValidate()
        {
            try
            {
                switch (Protocol)
                {
                    case HostProtocol.ipc:
                        return Nistec.Channels.PipeClient.Ping(HostAddress, HostPort, 5000);
                    case HostProtocol.tcp:
                        return Nistec.Channels.Tcp.TcpStreamClient.Ping(HostAddress, Port, 5000);
                    case HostProtocol.http:
                        return Nistec.Channels.Http.HttpClient.Ping(HostAddress, Port, 5000);
                    default:
                        return false;// NetProtocol.NA;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PingValidate error: " + ex.Message);
            }
            return false;
        }


        public void EnsureHost()
        {
            if (string.IsNullOrEmpty(HostName))
            {
                throw new Exception("QueueHost HostName");
            }
            if (string.IsNullOrEmpty(HostAddress))
            {
                throw new Exception("QueueHost OriginalHostAddress");
            }
            if (string.IsNullOrEmpty(RawHostAddress))
            {
                throw new Exception("QueueHost RawHostAddress");
            }
            if ((Protocol == HostProtocol.tcp || Protocol == HostProtocol.http) && Port <= 0)
            {
                throw new Exception("QueueHost Port requred for tcp|http protocol");
            }
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(HostName) || string.IsNullOrEmpty(HostAddress) || string.IsNullOrEmpty(RawHostAddress))
            {
                return false;
            }
            if ((Protocol == HostProtocol.tcp || Protocol == HostProtocol.http) && Port <= 0)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}
