using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace Nistec.Channels.Pipe
{
    public class IpcServerBase : IpcCallback
    {
        private IpcServer m_srv;
        private Int32 m_count;

        public void Start(string pipeName, int connections)
        {
            // Create ten instances of listening pipes so that if there are many clients
            // trying to connect in a short interval (with short timeouts) the clients are
            // less likely to fail to connect.
            m_srv = new IpcServer(pipeName, this, connections);
        }

        public void Stop()
        {
            m_srv.IpcServerStop();
        }

        public void OnAsyncConnect(PipeStream pipe, out Object state)
        {
            Int32 count = Interlocked.Increment(ref m_count);
            Console.WriteLine("Connected: " + count);
            state = count;
        }

        public void OnAsyncDisconnect(PipeStream pipe, Object state)
        {
            Console.WriteLine("Disconnected: " + (Int32)state);
        }

        public void OnAsyncMessage(PipeStream pipe, Byte[] data, Int32 bytes, Object state)
        {
            Console.WriteLine("Message: " + (Int32)state + " bytes: " + bytes);
            data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data, 0, bytes).ToUpper().ToCharArray());

            // Write results
            try
            {
                pipe.BeginWrite(data, 0, bytes, OnAsyncWriteComplete, pipe);
            }
            catch (Exception)
            {
                pipe.Close();
            }
        }

        private void OnAsyncWriteComplete(IAsyncResult result)
        {
            PipeStream pipe = (PipeStream)result.AsyncState;
            pipe.EndWrite(result);
        }
    }
}
