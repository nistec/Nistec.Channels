using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Nistec.Channels.RemoteQueue
{

    public class QueueApiPool
    {
        static readonly ConcurrentDictionary<string, QueueApi> Pool = new ConcurrentDictionary<string, QueueApi>();

        public void Set(string name, QueueApi api)
        {
            Pool[name] = api;
        }
        public bool TryAdd(string hostname, QueueApi api)
        {
            return Pool.TryAdd(hostname, api);
        }
        public bool TryUpdate(string hostname, QueueApi api)
        {
            QueueApi cur;
            if (Pool.TryGetValue(hostname, out cur))
            {
                return Pool.TryUpdate(hostname, api, cur);
            }
            return Pool.TryAdd(hostname, api);
        }

        public bool Remove(string name)
        {
            QueueApi api;
            return Pool.TryRemove(name, out api);
        }

        public void Clear()
        {
            Pool.Clear();
        }




        public static QueueApi GetApi(string hostname)
        {
            QueueApi api;
            if (Pool.TryGetValue(hostname, out api))
            {
                return api;
            }
            return null;
        }
        public static bool TryGetApi(string hostname, out QueueApi api)
        {
            return Pool.TryGetValue(hostname, out api);
        }

        public static IQueueAck Enqueue(QueueMessage message, string hostname, int connectTimeout = 0)
        {
            QueueApi api;
            if (Pool.TryGetValue(hostname, out api))
            {
                return api.Enqueue(message, connectTimeout);
            }

            return new QueueAck(MessageState.QueueNotFound, message);
        }

        public static IQueueMessage Dequeue(string hostname, int connectTimeout = 0)
        {
            QueueApi api;
            if (Pool.TryGetValue(hostname, out api))
            {
                return api.Dequeue(connectTimeout);
            }

            return null;
        }
    }


    public static class RemoteExtension
    {
        public static NetProtocol GetProtocol(this HostProtocol protocol)
        {
            if ((int)protocol > 3)
                return NetProtocol.NA;
            return (NetProtocol)(int)protocol;
        }


        public static IQueueAck Enqueue(this IQueueMessage message, string hostAddress,  int connectTimeout = 0)
        {
            var host = QueueHost.Parse(hostAddress);
            var api = new QueueApi(host);
            api.IsAsync = false;
            api.ConnectTimeout = connectTimeout;
            return api.Enqueue(message as QueueMessage);
        }

        public static IQueueAck Enqueue(this IQueueMessage message,  QueueHost host, int connectTimeout = 0)
        {
            var api = new QueueApi(host);
            api.IsAsync = false;
            api.ConnectTimeout = connectTimeout;
            return api.Enqueue(message as QueueMessage);
        }
 
    }
}
