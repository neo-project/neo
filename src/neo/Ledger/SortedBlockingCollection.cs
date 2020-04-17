using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Neo.Ledger
{
    public class SortedBlockingCollection<TKey, TValue>
    {
        /// <summary>
        /// _oracleTasks will consume from this pool
        /// </summary>
        private readonly BlockingCollection<TValue> _asyncPool = new BlockingCollection<TValue>();

        /// <summary>
        /// Queue
        /// </summary>
        private readonly SortedConcurrentDictionary<TKey, TValue> _queue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="comparer">Comparer</param>
        /// <param name="capacity">Capacity</param>
        public SortedBlockingCollection(IComparer<KeyValuePair<TKey, TValue>> comparer, int capacity)
        {
            _queue = new SortedConcurrentDictionary<TKey, TValue>(comparer, capacity);
        }

        /// <summary>
        /// Add entry
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public void Add(TKey key, TValue value)
        {
            if (_queue.TryAdd(key, value) && _asyncPool.Count <= 0)
            {
                Pop();
            }
        }

        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            _queue.Clear();

            while (_asyncPool.Count > 0)
            {
                _asyncPool.TryTake(out _);
            }
        }

        /// <summary>
        /// Get consuming enumerable
        /// </summary>
        /// <param name="token">Token</param>
        public IEnumerable<TValue> GetConsumingEnumerable(CancellationToken token)
        {
            foreach (var entry in _asyncPool.GetConsumingEnumerable(token))
            {
                // Prepare other item in _asyncPool

                Pop();

                // Iterate items

                yield return entry;
            }
        }

        /// <summary>
        /// Move one item from the sorted queue to _asyncPool, this will ensure that the threads process the entries according to the priority
        /// </summary>
        private void Pop()
        {
            if (_queue.TryPop(out var entry))
            {
                _asyncPool.Add(entry);
            }
        }
    }
}
