using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Neo.IO.Caching
{
    internal class FIFOSet<T> : IReadOnlyCollection<T> where T : IEquatable<T>
    {
        private readonly int maxCapacity;
        private readonly int removeCount;
        private readonly OrderedDictionary dictionary;

        public int Count => dictionary.Count;

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

        public bool Contains(T item)
        {
            return dictionary.Contains(item);
        }

        public void ExceptWith(IEnumerable<T> entries)
        {
            foreach (var entry in entries)
            {
                dictionary.Remove(entry);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var entries = dictionary.Keys.Cast<T>().ToArray();
            foreach (var entry in entries) yield return entry;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
