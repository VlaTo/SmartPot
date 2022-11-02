

#nullable enable

using System.Collections;

namespace SmartPot.Core.Messaging
{
    internal sealed class MessageQueue
    {
        private const int MaxQueueSize = 100;

        private readonly Queue queue;
        private readonly object gate;

        public int PendingCount => queue.Count;

        public MessageQueue()
        {
            queue = new Queue();
            gate = new object();
        }

        public bool TryEnqueue(IMessage message)
        {
            lock (gate)
            {
                if (MaxQueueSize <= queue.Count)
                {
                    return false;
                }

                queue.Enqueue(message);
            }

            return true;
        }

        public IMessage? TryDequeue()
        {
            lock (gate)
            {
                return 0 < queue.Count ? (IMessage)queue.Dequeue() : null;
            }
        }

        public IMessage? TryPeek()
        {
            lock (gate)
            {
                return 0 < queue.Count ? (IMessage)queue.Peek() : null;
            }
        }

        public void Pop()
        {
            lock (gate)
            {
                if (0 < queue.Count)
                {
                    queue.Dequeue();
                }
            }
        }
    }
}

#nullable restore