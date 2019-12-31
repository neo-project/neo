using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class HashSetCache<T> : IEnumerable<T> where T : IEquatable<T>
    {
        private readonly int hashSetCapacity;
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

        public HashSetCache(int hashSetCapacity, int hashSetCount = 10)
        {
            if (hashSetCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(hashSetCapacity));
            if (hashSetCount <= 0 || hashSetCount > 20) throw new ArgumentOutOfRangeException($"{nameof(hashSetCount)} should between 1 and 20");

            this.hashSetCapacity = hashSetCapacity;
            for (int i = 0; i < hashSetCount; i++)
            {
                sets.Add(new HashSet<T>());
            }
        }

        public bool Add(T item)
        {
            if (Contains(item)) return false;
            foreach (var set in sets)
            {
                if (set.Count < hashSetCapacity)
                {
                    return set.Add(item);
                }
            }
            sets.RemoveAt(0);
            var newSet = new HashSet<T>
            {
                item
            };
            sets.Add(newSet);
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
