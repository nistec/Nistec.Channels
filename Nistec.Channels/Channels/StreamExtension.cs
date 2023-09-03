using Nistec.IO;
using Nistec.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Nistec.Channels
{
    public static class ChannelsExtension
    {
        
        public static bool IsDuplex(this DuplexTypes dtype)
        {
            return dtype != DuplexTypes.None;
        }
        public static bool IsStateOk(this ChannelState State)
        {
            return (int)State > 0 && (int)State < 300;
        }
        public static bool IsConnectionError(this ChannelState state)
        {
            return state == ChannelState.ConnectionError;
        }

        public static ChannelStateSection SectionState(this ChannelState State)
        {
            if ((int)State > 0 && (int)State < 300)
                return ChannelStateSection.Ok;
            if ((int)State > 399 && (int)State < 500)
                return ChannelStateSection.ClientError;
            if ((int)State > 499 && (int)State < 590)
                return ChannelStateSection.ServerError;
            if ((int)State <0 ||((int)State > 499 && (int)State < 590))
                return ChannelStateSection.FatalError;
            else
                return ChannelStateSection.None;
        }

        public static TransType ToTransType(this TransformType type)
        {
            return (TransType)(int)type;
            //switch (type)
            //{
            //    case TransformType.Stream:
            //        return TransType.Stream;
            //    case TransformType.Json:
            //        return TransType.Json;
            //    default:
            //        return TransType.Object;
            //}
        }

        //public static bool IsClientError(this ChannelState State)
        //{
        //    return (int)State > 399 && (int)State < 500;
        //}
        //public static bool IsServerError(this ChannelState State)
        //{
        //    return (int)State > 499 && (int)State < 590;
        //}
        //public static bool IsFatalError(this ChannelState State)
        //{
        //    return (int)State > 499 && (int)State < 590;
        //}
    }
    public static class StreamExtension
    {

        public static int WriteStreamWithCount(this NetworkStream stream, NetStream outStream)
        {
            return WriteStreamWithCount(stream, outStream.ToArray());
        }

        public static int WriteStreamWithCount(this NetworkStream stream, byte[] outBuffer)
        {
            int len = outBuffer.Length;
            WriteValue(stream, len);
            stream.Write(outBuffer, 0, len);
            stream.Flush();

            return outBuffer.Length + 4;
        }


        /// <summary>
        /// Read from the given <see cref="NetworkStream"/> to byte array.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="BufferSize"></param>

        public static byte[] ReadStream(this NetworkStream stream, int BufferSize=8192)
        {
            if (BufferSize <= 0)
                BufferSize = 8192;
            int totalRead = 0;

            int BytesRead = 0;
            byte[] ReadBuffer = new byte[BufferSize];
            using (MemoryStream ns = new MemoryStream())
            {
                do
                {
                    BytesRead = stream.Read(ReadBuffer, 0, ReadBuffer.Length);
                    ns.Write(ReadBuffer, 0, BytesRead);
                    totalRead += BytesRead;
                }
                while (stream.DataAvailable);//BytesRead > 0);

                return ns.ToArray();
            }
        }

        /// <summary>
        /// Read from the given <see cref="PipeStream"/> to byte array.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="BufferSize"></param>
        public static byte[] ReadStream(this PipeStream stream, int BufferSize = 8192)
        {
            if (BufferSize <= 0)
                BufferSize = 8192;
            //int totalRead = 0;
            int BytesRead = 0;
            using (MemoryStream ns = new MemoryStream())
            {
                do
                {
                    byte[] bytes = new byte[BufferSize];
                    BytesRead = stream.Read(bytes, 0, bytes.Length);
                    ns.Write(bytes, 0, BytesRead);
                }
                while (!stream.IsMessageComplete);
                return ns.ToArray();
            }
        }

        /// <summary>
        /// Read from the given <see cref="Stream"/> to byte array.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="BufferSize"></param>
        public static byte[] ReadStream(this Stream stream, int BufferSize = 8192)
        {

            if (BufferSize <= 0)
                BufferSize = 8192;
            //int totalRead = 0;
            int BytesRead = 0;
            using (MemoryStream ns = new MemoryStream())
            {
                do
                {
                    byte[] bytes = new byte[BufferSize];
                    BytesRead = stream.Read(bytes, 0, bytes.Length);
                    ns.Write(bytes, 0, BytesRead);
                }
                while (BytesRead > 0);
                return ns.ToArray();
            }
        }

        public static int ReadStreamWithCount(this PipeStream stream, out byte[] array)
        {

            byte[] intbytes = new byte[4];
            stream.Read(intbytes, 0, 4);
            int len = ReadInt32(intbytes, 0);
            byte[] buffer = new byte[len];

            int totalRead = 0;
            int BytesRead = 0;

            do
            {
                BytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                totalRead += BytesRead;
            }
            while (!stream.IsMessageComplete && totalRead < len);

            array = buffer;

            return totalRead;
        }

        public static int ReadStreamWithCount(this NetworkStream stream, out byte[] array)
        {

            byte[] intbytes = new byte[4];
            stream.Read(intbytes, 0, 4);
            int len = ReadInt32(intbytes, 0);
            Console.WriteLine("ReadString length: " + len.ToString());
            byte[] buffer = new byte[len];

            int totalRead = 0;

            int BytesRead = 0;
            do
            {
                BytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                totalRead += BytesRead;
            }
            while (BytesRead > 0 && totalRead < len);

            array = buffer;

            return totalRead;
        }

        public static int ReadStreamWithCount(this Stream stream, out byte[] array)
        {

            byte[] intbytes = new byte[4];
            stream.Read(intbytes, 0, 4);
            int len = ReadInt32(intbytes, 0);
            byte[] buffer = new byte[len];

            int totalRead = 0;
            int BytesRead = 0;
            do
            {
                BytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                totalRead += BytesRead;
            }
            while (BytesRead > 0 && totalRead < len);

            array = buffer;

            return totalRead;
        }


        public static string ReadStreamWithCount(this NetworkStream stream, Encoding encoding)
        {
            byte[] buffer = null;
            int totalRead = ReadStreamWithCount(stream, out buffer);
            
            return encoding.GetString(buffer);
        }
        public static void WriteValue(this NetworkStream stream, int value)
        {
            byte[] buffer = new byte[4];

            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 0x10);
            buffer[3] = (byte)(value >> 0x18);
            stream.Write(buffer, 0, 4);
        }

        public static int ReadInt32(byte[] buffer, int offset)
        {
            return (((buffer[offset + 0] | (buffer[offset + 1] << 8)) | (buffer[offset + 2] << 0x10)) | (buffer[offset + 3] << 0x18));
        }
    }
}
