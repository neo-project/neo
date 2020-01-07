using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class HashSetCache<T> : IReadOnlyCollection<T> where T : IEquatable<T>
    {
        /// <summary>
        /// Sets where the Hashes are stored
        /// </summary>      
        private readonly LinkedList<HashSet<T>> sets = new LinkedList<HashSet<T>>();

        /// <summary>
        /// Maximum capacity of each bucket inside each HashSet of <see cref="sets"/>.
        /// </summary>        
        private readonly int bucketCapacity;

        /// <summary>
        /// Maximum number of buckets for the LinkedList, meaning its maximum cardinality.
        /// </summary>
        private readonly int maxBucketCount;

        /// <summary>
        /// Entry count
        /// </summary>
        public int Count { get; }
        
        public HashSetCache(int bucketCapacity, int maxBucketCount = 10)
        {
            if (bucketCapacity <= 0) throw new ArgumentOutOfRangeException($"{nameof(bucketCapacity)} should be greater than 0");
            if (maxBucketCount <= 0) throw new ArgumentOutOfRangeException($"{nameof(maxBucketCount)} should be greater than 0");

            this.Count = 0;
            this.bucketCapacity = bucketCapacity;
            this.maxBucketCount = maxBucketCount;
            sets.AddFirst(new HashSet<T>());
        }

        public bool Add(T item)
        {
            if (Contains(item)) return false;
            Count++;
            if (sets.First.Value.Count < bucketCapacity) return sets.First.Value.Add(item);
            var newSet = new HashSet<T>
            {
                item
            };
            sets.AddFirst(newSet);
            if (sets.Count > maxBucketCount) sets.RemoveLast();
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
                    if (set.Remove(item))
                    {
                        Count--;
                        break;
                    }
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
