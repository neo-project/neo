using System;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class HashCache<T> where T : IEquatable<T>
    {
        /// <summary>
        /// Sets where the Hashes are stored
        /// </summary>      
        private readonly LinkedList<HashSet<T>> sets = new LinkedList<HashSet<T>>();

        /// <summary>
        /// Maximum number of buckets for the LinkedList, meaning its maximum cardinality.
        /// </summary>
        private readonly int maxBucketCount;

        public int Depth => sets.Count;

        public HashCache(int maxBucketCount = 10)
        {
            if (maxBucketCount <= 0) throw new ArgumentOutOfRangeException($"{nameof(maxBucketCount)} should be greater than 0");

            this.maxBucketCount = maxBucketCount;
            sets.AddFirst(new HashSet<T>());
        }

        public bool Add(T item)
        {
            //if (Contains(item)) return false;
            return sets.First.Value.Add(item);
        }

        public bool Contains(T item)
        {
            foreach (var set in sets)
            {
                if (set.Contains(item)) return true;
            }
            return false;
        }

        public void Refresh ()
        {
            var newSet = new HashSet<T>();
            sets.AddFirst(newSet);
            if (sets.Count > maxBucketCount)
            {
                sets.RemoveLast();
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
    }
}
