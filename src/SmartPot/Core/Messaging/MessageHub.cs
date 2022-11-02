#nullable enable

using System.Collections;

namespace SmartPot.Core.Messaging
{
    internal sealed class MessageHub
    {
        private const int DefaultQueuesCount = 16;

        private readonly Hashtable queues;
        private readonly object gate;

        public int QueuesCount => queues.Count;

        public MessageQueue? this[string queueName]
        {
            get
            {
                lock (gate)
                {
                    var exists = queues.Contains(queueName);

                    if (exists)
                    {
                        return (MessageQueue)queues[queueName];
                    }

                    if (DefaultQueuesCount < queues.Count)
                    {
                        return null;
                    }

                    var queue = new MessageQueue();

                    queues.Add(queueName, queue);

                    return queue;
                }
            }
        }

        public MessageHub()
        {
            gate = new object();
            queues = new Hashtable(DefaultQueuesCount);
        }

        public bool Contains(string queueName)
        {
            lock (gate)
            {
                return queues.Contains(queueName);
            }
        }
    }
}

#nullable restore