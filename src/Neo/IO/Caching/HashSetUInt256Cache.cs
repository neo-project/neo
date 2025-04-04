// Copyright (C) 2015-2025 The Neo Project.
//
// HashSetUInt256Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Cryptography;
using Neo.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class HashSetUInt256Cache : IReadOnlyCollection<UInt256>
    {
        private class Bucket : IEnumerable<UInt256>
        {
            private readonly HashSet<UInt256> _set;
            private readonly BloomFilter _filter;

            public int Count => _set.Count;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="set">Set</param>
            public Bucket(params UInt256[] set)
            {
                _set = new HashSet<UInt256>(set);
                _filter = new BloomFilter(1024 * 1024 * 1024, 7, 0);

                foreach (var hash in set)
                {
                    _filter.Add(hash.ToArray());
                }
            }

            /// <summary>
            /// Check if hash is in the bucket
            /// </summary>
            /// <param name="hash">Hash</param>
            /// <returns>True if exists</returns>
            public bool Contains(UInt256 hash)
            {
                // If is not in the bloom filter is not in the set

                if (!_filter.Check(hash.ToArray()))
                {
                    return false;
                }

                return _set.Contains(hash);
            }

            /// <summary>
            /// Remove entry from bucket
            /// </summary>
            /// <param name="hash">Hash</param>
            /// <returns>True if was removed</returns>
            public bool Remove(UInt256 hash)
            {
                // Remove don't touch the bloom filter
                return _set.Remove(hash);
            }

            /// <summary>
            /// Add hash to bucket
            /// </summary>
            /// <param name="hash">Hash</param>
            /// <returns>True if was added</returns>
            public bool Add(UInt256 hash)
            {
                if (_set.Add(hash))
                {
                    _filter.Add(hash.ToArray());
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Clear
            /// </summary>
            public void Clear()
            {
                _set.Clear();
                _filter.Reset();
            }

            public IEnumerator<UInt256> GetEnumerator() => _set.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
        }

        /// <summary>
        /// Sets where the Hashes are stored
        /// </summary>
        private readonly LinkedList<Bucket> _sets;

        /// <summary>
        /// Maximum capacity of each bucket inside each HashSet of <see cref="_sets"/>.
        /// </summary>
        private readonly int _bucketCapacity;

        /// <summary>
        /// Maximum number of buckets for the LinkedList, meaning its maximum cardinality.
        /// </summary>
        private readonly int _maxBucketCount;

        /// <summary>
        /// Entry count
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the HashSetCache class with the specified bucket capacity and maximum number of buckets.
        /// The total entries will be bucketCapacity * maxBucketCount
        /// </summary>
        /// <param name="bucketCapacity">The maximum number of items each HashSet bucket can hold. Must be greater than 0.</param>
        /// <param name="maxBucketCount">The maximum number of HashSet buckets the cache can maintain. Must be greater than 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either bucketCapacity or maxBucketCount is less than or equal to 0.</exception>
        public HashSetUInt256Cache(int bucketCapacity, int maxBucketCount = 10)
        {
            if (bucketCapacity <= 0) throw new ArgumentOutOfRangeException($"{nameof(bucketCapacity)} should be greater than 0");
            if (maxBucketCount <= 0) throw new ArgumentOutOfRangeException($"{nameof(maxBucketCount)} should be greater than 0");

            Count = 0;
            _bucketCapacity = bucketCapacity;
            _maxBucketCount = maxBucketCount;
            _sets = new LinkedList<Bucket>([]);
        }

        public bool Add(UInt256 item)
        {
            if (Contains(item)) return false;
            Count++;
            if (_sets.First?.Value.Count < _bucketCapacity)
            {
                return _sets.First.Value.Add(item);
            }

            _sets.AddFirst(new Bucket(item));
            if (_sets.Count > _maxBucketCount)
            {
                Count -= _sets.Last!.Value.Count;
                _sets.RemoveLast();
            }
            return true;
        }

        public bool Contains(UInt256 item)
        {
            foreach (var set in _sets)
            {
                if (set.Contains(item)) return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var set in _sets)
            {
                set.Clear();
            }

            _sets.Clear();
            Count = 0;
        }

        public void ExceptWith(IEnumerable<UInt256> items)
        {
            List<Bucket> removeList = [];
            foreach (var item in items)
            {
                foreach (var set in _sets)
                {
                    if (set.Remove(item))
                    {
                        Count--;
                        if (set.Count == 0)
                        {
                            removeList.Add(set);
                        }
                        break;
                    }
                }
            }
            foreach (var set in removeList)
            {
                _sets.Remove(set);
            }
        }

        public IEnumerator<UInt256> GetEnumerator()
        {
            foreach (var set in _sets)
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

#nullable disable
