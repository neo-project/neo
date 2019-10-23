using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    internal class FIFOSet<T> : IEnumerable<T> where T : IEquatable<T>
    {
        class Entry
        {
            public readonly T Value;
            public Entry Next;

            public Entry(T current)
            {
                Value = current;
            }
        }

        private readonly int maxCapacity;
        private readonly int removeCount;

        private int _count = 0;
        private Entry _firstEntry = null;
        private Entry _lastEntry = null;

        public int Count => _count;

        public FIFOSet(int maxCapacity, decimal batchSize = 0.1m)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            if (batchSize <= 0 || batchSize > 1) throw new ArgumentOutOfRangeException(nameof(batchSize));

            this.maxCapacity = maxCapacity;
            this.removeCount = Math.Max((int)(maxCapacity * batchSize), 1);
        }

        public bool Add(T item)
        {
            if (Contains(item)) return false;
            if (Count >= maxCapacity)
            {
                if (removeCount == maxCapacity)
                {
                    _lastEntry = _firstEntry = null;
                    _count = 0;
                }
                else
                {
                    for (int i = 0; i < removeCount; i++)
                    {
                        RemoveFirst();
                    }
                }
            }

            if (_lastEntry == null)
            {
                _firstEntry = _lastEntry = new Entry(item);
            }
            else
            {
                _lastEntry = _lastEntry.Next = new Entry(item);
            }

            _count++;
            return true;
        }

        private void RemoveFirst()
        {
            if (_firstEntry != null)
            {
                _firstEntry = _firstEntry.Next;
                _count--;
            }
        }

        public bool Contains(T item)
        {
            var current = _firstEntry;

            while (current != null)
            {
                if (current.Value.Equals(item)) return true;
                current = current.Next;
            }

            return false;
        }

        private void Remove(T item)
        {
            var prev = _firstEntry;
            var current = _firstEntry;

            while (current != null)
            {
                if (current.Value.Equals(item))
                {
                    if (prev == null)
                    {
                        // First entry
                        _firstEntry = current.Next;
                    }
                    else
                    {
                        if (current == _firstEntry)
                        {
                            _firstEntry = prev.Next;
                        }
                        else
                        {
                            prev.Next = current.Next;
                        }
                    }
                    _count--;
                    return;
                }

                prev = current;
                current = current.Next;
            }
        }

        public void ExceptWith(IEnumerable<T> entries)
        {
            foreach (var entry in entries)
            {
                Remove(entry);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var current = _firstEntry;

            while (current != null)
            {
                yield return current.Value;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
