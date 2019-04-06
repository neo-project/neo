using System;
using System.Collections.Specialized;

namespace Neo.IO.Caching
{
    internal class FIFOSet<T> where T : IEquatable<T>
    {
        private readonly int maxCapacity;
        private readonly int removeCount;
        private readonly OrderedDictionary dictionary;

        public FIFOSet(int maxCapacity, decimal batchSize = 0.1m)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            if (batchSize <= 0 || batchSize > 1) throw new ArgumentOutOfRangeException(nameof(batchSize));

            this.maxCapacity = maxCapacity;
            this.removeCount = Math.Max((int)(maxCapacity * batchSize), 1);
            this.dictionary = new OrderedDictionary(maxCapacity);
        }

        public bool Add(T item)
        {
            if (dictionary.Contains(item)) return false;
            if (dictionary.Count >= maxCapacity)
            {
                if (removeCount == maxCapacity)
                {
                    dictionary.Clear();
                }
                else
                {
                    for (int i = 0; i < removeCount; i++)
                        dictionary.RemoveAt(0);
                }
            }
            dictionary.Add(item, null);
            return true;
        }
    }
}
