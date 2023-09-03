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
    [Serializable]
    public class HostChannel //: ISerialEntity,IDisposable
    {

        #region ctor

        protected HostChannel()
        {
            Segments = new string[5];
        }

        public HostChannel(string address)
        {
            Segments = new string[5];
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            string[] args = address.Replace("//", "").TrimStart('/').Split(':', '/', '?');

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
        public string HostAddress { get { return Segments[1]; } }
        public string HostPort { get { return Segments[2]; } }
        public string HostName { get { return Segments[3]; } }
                

        /// <summary>
        /// Get indicate wether this host can distrebute.
        /// </summary>
        public bool CanDistrebute
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
            switch (protocol)
            {

                case HostProtocol.ipc://ipc:.:nistec_queue/hostName
                    return string.Format("ipc:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.tcp://tcp:127.0.0.1:9015/hostName
                    return string.Format("tcp:{0}:{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.http://http://127.0.0.1:9015/hostName
                    return string.Format("http://{0}:{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.file://file:root/folder/hostName
                    return string.Format("file:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.db://db:serve/catalog/hostName
                    return string.Format("db:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
            }
        }
        public static string GetRawAddress(string[] segments)
        {
            switch (segments[0])
            {

                case "ipc"://ipc:.:nistec_queue/hostName
                    return string.Format("ipc:{0}/{1}/{2}", segments[1], segments[2], segments[3]);
                case "tcp"://tcp:127.0.0.1:9015/hostName
                    return string.Format("tcp:{0}:{1}/{2}", segments[1], segments[2], segments[3]);
                case "http"://http://127.0.0.1:9015/hostName
                    return string.Format("http://{0}:{1}/{2}", segments[1], segments[2], segments[3]);
                case "file"://file:root/folder/hostName
                    return string.Format("file:{0}/{1}/{2}", segments[1], segments[2], segments[3]);
                case "db"://db:serve/catalog/hostName
                    return string.Format("db:{0}/{1}/{2}", segments[1], segments[2], segments[3]);
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
            }
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

#if (false)
[Serializable]
    public class HostChannel //: ISerialEntity,IDisposable
    {

    #region ctor
        public HostChannel(string address)
        {

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            string[] args = address.Replace("//","").TrimStart('/').Split(':', '/');

            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid hostAddress");
            }

            string protocol = args[0];
            string serverAddress = args[1];
            string hostPort = args[2];
            string hostName = null;
            int port = 0;

            if (args.Length > 3)
            {
                hostName = args[3];

            }

            this.RawHostAddress = address;
            this.HostName = hostName;

            switch (protocol)
            {
                
                case "ipc"://ipc:./nistec_enqueue/hostName
                    this.Protocol = HostProtocol.ipc;
                    this.Port = port;
                    this.HostAddress = hostPort;
                    this.ServerName = serverAddress;
                    break;
                case "file"://file:root/folder/hostName
                    this.Protocol = HostProtocol.file;
                    this.Port = port;
                    this.HostAddress = hostPort;
                    this.ServerName = serverAddress;
                    break;
                case "tcp"://tcp:127.0.0.1:9015/hostName
                    port = Types.ToInt(hostPort);
                    if (port <= 0)
                    {
                        throw new Exception("Invalid port number for tcp.");
                    }
                    this.Protocol = HostProtocol.tcp;
                    this.Port = port;
                    this.HostAddress = serverAddress;
                    this.ServerName = ".";
                    break;
                case "http"://http://127.0.0.1:9015/hostName
                    port = Types.ToInt(hostPort);
                    if (port <= 0)
                    {
                        port = 80;
                    }
                    this.Protocol = HostProtocol.http;
                    this.Port = port;
                    this.HostAddress = serverAddress;
                    this.ServerName = ".";
                    break;
                case "db"://db:serve/catalog/hostName
                    this.Protocol = HostProtocol.db;
                    this.Port = port;
                    this.HostAddress = hostPort;
                    this.ServerName = serverAddress;
                    break;
                default:
                    throw new Exception("Incorrect address or AddressType not supported");

            }

        }
        
        public HostChannel(HostProtocol protocol, string serverAddress, string hostPort, string hostName)
        {
            HostName = hostName;
            Protocol = protocol;
            switch (protocol)
            {
                case HostProtocol.ipc:
                    ServerName = serverAddress;
                    HostAddress = hostPort;
                    Port = 0;
                    //RawHostAddress = string.Format("//{0}:{1}:{2}/{3}", protocol.ToString(), ServerName, HostAddress, hostName);//ipc:.:nistec?queuName
                    break;
                case HostProtocol.tcp:
                    ServerName = ".";
                    HostAddress = serverAddress;
                    Port = Types.ToInt(hostPort);
                    //RawHostAddress = string.Format("//{0}:{1}:{2}/{3}", protocol.ToString(), HostAddress, Port, hostName);//tcp:nistec.net:13000?queuName
                    break;
                case HostProtocol.http:
                    ServerName = ".";
                    HostAddress = serverAddress;
                    Port = Types.ToInt(hostPort);
                    if (Port <= 0)
                        Port = 80;

                    //RawHostAddress = string.Format("//{0}:{1}:{2}/{3}", protocol.ToString(), HostAddress, Port, hostName);//http:nistec.net:13010?queuName
                    break;
                case HostProtocol.db://db:serve/catalog/hostName
                    Port = 0;
                    HostAddress = hostPort;
                    ServerName = serverAddress;
                    //RawHostAddress = string.Format("//{0}:{1}:{2}/{3}", protocol.ToString(), HostAddress, Port, hostName);//http:nistec.net:13010?queuName
                    break;
                case HostProtocol.file://file:root/folder/hostName
                    Port = 0;
                    HostAddress = hostPort;
                    ServerName = serverAddress;
                    //RawHostAddress = string.Format("//{0}:{1}:{2}/{3}", protocol.ToString(), HostAddress, Port, hostName);//http:nistec.net:13010?queuName
                    break;
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
                    //    ServerName = serverAddress;
                    //    HostAddress = hostPort;
                    //    Port = 0;
                    //    RawHostAddress = string.Format("//{0}:{1}:{2}/{3}", protocol.ToString(), serverAddress, hostPort, hostName);
                    //    break;
            }

            RawHostAddress = GetRawAddress(Protocol, serverAddress, hostPort, hostName);
        }

    #endregion

    #region properties
        public string HostId
        {
            get
            {
                return string.Format("{0}-{1}-{2}", ServerName, Port, HostName);
                //return string.Format("{0}-{1}-{2}-{3}", ServerName, Port, HostName,QueueName);
            }
        }

        /// <summary>
        /// Get or Set HostName
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// Get or Set ServerName
        /// </summary>
        public string ServerName { get; private set; }

        /// <summary>
        /// Get or Set Endpoint Address
        /// </summary>
        public string Endpoint
        {
            get
            {
                switch (Protocol)
                {
                    case HostProtocol.ipc:
                        return HostAddress;
                    case HostProtocol.tcp:
                        return ServerName;
                    case HostProtocol.http:
                        return ServerName;
                    default:
                        return HostAddress;
                }
            }
        }
 
        public string RawHostAddress
        {
            get; private set;
        }

        public string HostAddress
        {
            get; private set;
        }
        public string HostPort { get; private set; }

        public HostProtocol Protocol
        {
            get; private set;
        }

        public int Port
        {
            get; private set;
        }


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
                        return string.Format("{0}/{1}", ServerName, HostAddress);
                    case HostProtocol.file://file:root/folder
                        return string.Format("{0}/{1}", ServerName, HostAddress);
                    case HostProtocol.tcp://tcp:127.0.0.1:9015
                        return string.Format("{0}:{1}", HostAddress, Port);
                    case HostProtocol.http://127.0.0.1:9015
                        return string.Format("{0}:{1}", HostAddress, Port);
                    case HostProtocol.db://db:serve/catalog
                        return string.Format("{0}/{1}", ServerName, HostAddress);
                    default:
                        throw new Exception("Incorrect address or HostProtocol not supported");
                }
            }
        }

        /// <summary>
        /// Get indicate wether this host can distrebute.
        /// </summary>
        public bool CanDistrebute
        {
            get { return !string.IsNullOrEmpty(RawHostAddress) && RawHostAddress.StartsWith("tcp:"); }
        }

        /// <summary>
        /// Get indicate wether this host is local.
        /// </summary>
        public bool IsLocal
        {
            get { return Types.NZ(ServerName, ".") == "."; }
        }
    #endregion

    #region parse
        public static HostChannel Parse(string hostAddress)
        {
            HostChannel host = new HostChannel(hostAddress);
            return host;
        }

        public static string GetRawAddress(HostProtocol protocol, string serverAddress, string hostPort, string hostName)
        {
            switch (protocol)
            {

                case HostProtocol.ipc://ipc:.:nistec_queue/hostName
                    return string.Format("ipc:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.tcp://tcp:127.0.0.1:9015/hostName
                    return string.Format("tcp:{0}:{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.http://http://127.0.0.1:9015/hostName
                    return string.Format("http://{0}:{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.file://file:root/folder/hostName
                    return string.Format("file:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                case HostProtocol.db://db:serve/catalog/hostName
                    return string.Format("db:{0}/{1}/{2}", serverAddress, hostPort, hostName);
                default:
                    throw new Exception("Incorrect address or HostProtocol not supported");
            }
        }
        //public static string GetRawAddress(HostProtocol protocol, string hostAddress, int hostPort, string hostName, string serverName = ".")
        //{
        //    switch (protocol)
        //    {

        //        case HostProtocol.ipc://ipc:.:nistec_enqueue/hostName
        //            return string.Format("ipc:{0}/{1}/{2}", serverName, hostAddress, hostName);
        //        case HostProtocol.file://file:root/folder/hostName
        //            return string.Format("file:{0}/{1}/{2}", serverName, hostAddress, hostName);
        //        case HostProtocol.tcp://tcp:127.0.0.1:9015/hostName
        //            return string.Format("tcp:{0}:{1}/{2}", hostAddress, hostPort, hostName);
        //        case HostProtocol.http://http://127.0.0.1:9015/hostName
        //            return string.Format("http://{0}:{1}/{2}", hostAddress, hostPort, hostName);
        //        case HostProtocol.db://db:serve/catalog/hostName
        //            return string.Format("db:{0}/{1}/{2}", serverName, hostAddress, hostName);
        //        default:
        //            throw new Exception("Incorrect address or HostProtocol not supported");
        //    }
        //}
    #endregion

    #region  ISerialEntity

        /// <summary>
        /// Write the current object include the body and properties to stream using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public void EntityWrite(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            streamer.WriteString(HostName);
            streamer.WriteString(ServerName);
            streamer.WriteString(RawHostAddress);
            streamer.WriteString(HostAddress);
            streamer.WriteValue((byte)Protocol);
            streamer.Flush();
        }


        /// <summary>
        /// Read stream to the current object include the body and properties using <see cref="IBinaryStreamer"/>, This method is a part of <see cref="ISerialEntity"/> implementation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamer"></param>
        public void EntityRead(Stream stream, IBinaryStreamer streamer)
        {
            if (streamer == null)
                streamer = new BinaryStreamer(stream);

            HostName = streamer.ReadString();
            ServerName = streamer.ReadString();
            RawHostAddress = streamer.ReadString();
            HostAddress = streamer.ReadString();
            Protocol = (HostProtocol)streamer.ReadValue<byte>();
        }

    #endregion

    #region assists

        //public bool IsPingOk { get; private set; }
        public bool PingValidate()
        {
            try
            {
                switch (Protocol)
                {
                    case HostProtocol.ipc:
                        return Nistec.Channels.PipeClient.Ping(ServerName, HostName, 5000);
                    case HostProtocol.tcp:
                        return Nistec.Channels.Tcp.TcpClient.Ping(ServerName, Port, 5000);
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
            if((Protocol== HostProtocol.tcp || Protocol== HostProtocol.http) && Port <=0)
            {
                return false;
            }
            return true;
        }

    #endregion
    }

#endif

}
