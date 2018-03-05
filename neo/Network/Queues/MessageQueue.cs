using Neo.IO;
using System.Collections.Generic;

namespace Neo.Network.Queues
{
    public abstract class MessageQueue<T>
    {
        protected readonly Queue<T> QueueHigh = new Queue<T>();
        protected readonly Queue<T> QueueLow = new Queue<T>();

        /// <summary>
        /// Dequeue object according to priority
        /// </summary>
        public T Dequeue()
        {
            T ret = default(T);

            lock (QueueHigh)
            {
                if (QueueHigh.Count > 0)
                {
                    ret = QueueHigh.Dequeue();
                }
            }

            if (ret == null)
            {
                lock (QueueLow)
                {
                    if (QueueLow.Count > 0)
                    {
                        ret = QueueLow.Dequeue();
                    }
                }
            }

            return ret;
        }
    }
}