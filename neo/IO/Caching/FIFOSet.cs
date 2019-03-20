using System;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    internal class FIFOSet<T> where T : IEquatable<T>
    {
        private int maxCapacity;
        private int removeCount;
        private List<T> orderedList;

        public FIFOSet(int maxCapacity, decimal? batchSize = 0.1m)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

            this.maxCapacity = maxCapacity;
            this.removeCount = batchSize != null ? (int)(batchSize <= 1.0m ? maxCapacity * batchSize : maxCapacity) : 1;
            this.orderedList = new List<T>(maxCapacity);
        }

        public bool Add(T item)
        {
            if (orderedList.Contains(item)) return false;
            if (orderedList.Count >= maxCapacity)
            {
                if (removeCount == maxCapacity)
                {
                    orderedList.Clear();
                }
                else
                {
                    for (int i = 0; i < removeCount; i++)
                        orderedList.RemoveAt(0);
                }
            }
            orderedList.Add(item);
            return true;
        }
    }
}