// Copyright (C) 2015-2025 The Neo Project.
//
// HashSetCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    internal class HashSetCache<T> : IReadOnlyCollection<T> where T : IEquatable<T>
    {
        /// <summary>
        /// Sets where the Hashes are stored
        /// </summary>
        private readonly LinkedList<HashSet<T>> _sets;

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

        public HashSetCache(int bucketCapacity, int maxBucketCount = 10)
        {
            if (bucketCapacity <= 0) throw new ArgumentOutOfRangeException($"{nameof(bucketCapacity)} should be greater than 0");
            if (maxBucketCount <= 0) throw new ArgumentOutOfRangeException($"{nameof(maxBucketCount)} should be greater than 0");

            Count = 0;
            _bucketCapacity = bucketCapacity;
            _maxBucketCount = maxBucketCount;
            _sets = new LinkedList<HashSet<T>>([]);
        }

        public bool Add(T item)
        {
            if (Contains(item)) return false;
            Count++;
            if (_sets.First?.Value.Count < _bucketCapacity)
            {
                return _sets.First.Value.Add(item);
            }

            var newSet = new HashSet<T>
            {
                item
            };
            _sets.AddFirst(newSet);
            if (_sets.Count > _maxBucketCount)
            {
                Count -= _sets.Last!.Value.Count;
                _sets.RemoveLast();
            }
            return true;
        }

        public bool Contains(T item)
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
        }

        public void ExceptWith(IEnumerable<T> items)
        {
            List<HashSet<T>> removeList = [];
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

        public IEnumerator<T> GetEnumerator()
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
