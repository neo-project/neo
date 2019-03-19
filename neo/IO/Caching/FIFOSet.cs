using System;
using System.Collections.Specialized;

namespace Neo.IO.Caching
{
    internal class FIFOSet<T>
    {
        private int maxCapacity;
        private int removeCount;
        private OrderedDictionary orderedDictionary;

        public FIFOSet(int maxCapacity, decimal? batchSize = 0.1m)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            this.maxCapacity = maxCapacity;
            this.removeCount = batchSize != null ? (int)(batchSize <= 1.0m ? maxCapacity * batchSize : maxCapacity) : 1;
            this.orderedDictionary = new OrderedDictionary(maxCapacity);
        }

        public bool Add(T item)
        {
            if (orderedDictionary.Contains(item)) return false;
            if (orderedDictionary.Count >= maxCapacity)
            {
                if (this.removeCount == this.maxCapacity)
                {
                    orderedDictionary.Clear();
                }
                else
                {
                    for (int i = 0; i < this.removeCount; i++)
                        orderedDictionary.RemoveAt(0);
                }
            }
            orderedDictionary.Add(item, null);
            return true;
        }
    }
}
