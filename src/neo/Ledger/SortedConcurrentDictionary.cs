using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Neo.Ledger
{
    public class SortedConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private int _isDirty = 0;

        private readonly Dictionary<TKey, TValue> _keys;
        private readonly List<KeyValuePair<TKey, TValue>> _sortedValues;
        private readonly IComparer<KeyValuePair<TKey, TValue>> _comparer;

        public event EventHandler<KeyValuePair<TKey, TValue>> OnTrimEnd;

        private readonly object _lock = new object();

        /// <summary>
        /// Count
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _sortedValues.Count;
                }
            }
        }

        /// <summary>
        /// Capacity
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="comparer">Comparer</param>
        /// <param name="capacity">Capacity</param>
        public SortedConcurrentDictionary(IComparer<KeyValuePair<TKey, TValue>> comparer, int capacity)
        {
            Capacity = Math.Max(1, capacity);

            _comparer = comparer;
            _sortedValues = new List<KeyValuePair<TKey, TValue>>();
            _keys = new Dictionary<TKey, TValue>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                return _keys.TryGetValue(key, out value);
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_keys.Remove(key, out value))
                {
                    _sortedValues.RemoveAll(u => u.Key.Equals(key));
                    return true;
                }
            }

            return false;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_keys.TryAdd(key, value))
                {
                    Interlocked.Exchange(ref _isDirty, 0x01);
                    _sortedValues.Add(new KeyValuePair<TKey, TValue>(key, value));

                    if (_sortedValues.Count > Capacity)
                    {
                        // Trim the last element (sorted)

                        Sort();

                        var index = _sortedValues.Count - 1;
                        var last = _sortedValues[index];

                        if (_keys.Remove(last.Key))
                        {
                            _sortedValues.RemoveAt(index);

                            // Call the event

                            OnTrimEnd?.Invoke(this, last);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _sortedValues.Clear();
                _keys.Clear();
            }
        }

        public bool TryPop(out TValue value)
        {
            lock (_lock)
            {
                if (_sortedValues.Count > 0)
                {
                    Sort();

                    var entry = _sortedValues[0];
                    _sortedValues.RemoveAt(0);
                    _keys.Remove(entry.Key);

                    value = entry.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        #region Get sorted list

        /// <summary>
        /// Sort (thread not safe)
        /// </summary>
        private void Sort()
        {
            if (_comparer != null && Interlocked.Exchange(ref _isDirty, 0x00) == 0x01)
            {
                _sortedValues.Sort(_comparer);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            KeyValuePair<TKey, TValue>[] array;

            lock (_lock)
            {
                Sort();
                array = _sortedValues.ToArray();
            }

            return (IEnumerator<KeyValuePair<TKey, TValue>>)array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
