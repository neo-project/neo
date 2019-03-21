using System;
using System.Collections.Specialized;

namespace Neo.IO.Caching
{
    internal class FIFOSet<T> where T : IEquatable<T>, IComparable<T>
    {
        private readonly int maxCapacity;
        private readonly int removeCount;
        private readonly OrderedDictionary data;

        public FIFOSet(int maxCapacity, decimal? batchSize = 0.1m)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            this.maxCapacity = maxCapacity;
            this.removeCount = batchSize != null ? (int)(batchSize <= 1.0m ? maxCapacity * batchSize : maxCapacity) : 1;
            this.data = new OrderedDictionary(maxCapacity);
        }

        public bool Add(T item)
        {
            if (data.Contains(item)) return false;
            if (data.Count >= maxCapacity)
            {
                if (removeCount == maxCapacity)
                {
                    data.Clear();
                }
                else
                {
                    for (int i = 0; i < removeCount; i++)
                        data.RemoveAt(0);
                }
            }
            data.Add(item, null);
            return true;
        }
    }
}