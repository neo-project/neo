using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class HashSetCache<T> : IReadOnlyCollection<T> where T : IEquatable<T>
    {
        private readonly int bucketCapacity;
        private readonly int bucketCount;
        private readonly List<HashSet<T>> sets = new List<HashSet<T>>();

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var set in sets)
                {
                    count += set.Count;
                }
                return count;
            }
        }

        public HashSetCache(int bucketCapacity, int bucketCount = 10)
        {
            if (bucketCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bucketCapacity));
            if (bucketCount <= 0 || bucketCount > 20) throw new ArgumentOutOfRangeException($"{nameof(bucketCount)} should between 1 and 20");

            this.bucketCapacity = bucketCapacity;
            this.bucketCount = bucketCount;
            sets.Add(new HashSet<T>());
        }

        public bool Add(T item)
        {
            if (Contains(item)) return false;
            if (sets[0].Count < bucketCapacity) return sets[0].Add(item);
            var newSet = new HashSet<T>
            {
                item
            };
            sets.Insert(0, newSet);
            if (sets.Count > bucketCount) sets.RemoveAt(bucketCount);
            return true;
        }

        public bool Contains(T item)
        {
            foreach (var set in sets)
            {
                if (set.Contains(item)) return true;
            }
            return false;
        }

        public void ExceptWith(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                foreach (var set in sets)
                {
                    if (set.Remove(item)) break;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var set in sets)
            {
                foreach (var item in set)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
