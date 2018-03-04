using System.Collections.Generic;

namespace Neo.Network.Queues
{
    public abstract class MessageQueue<T>
    {
        protected readonly Queue<T> Queue_high = new Queue<T>();
        protected readonly Queue<T> Queue_low = new Queue<T>();

        /// <summary>
        /// Dequeue object according to priority
        /// </summary>
        public T Dequeue()
        {
            T ret = default(T);

            lock (Queue_high)
            {
                if (Queue_high.Count > 0)
                {
                    ret = Queue_high.Dequeue();
                }
            }

            if (ret == null)
            {
                lock (Queue_low)
                {
                    if (Queue_low.Count > 0)
                    {
                        ret = Queue_low.Dequeue();
                    }
                }
            }

            return ret;
        }
    }
}