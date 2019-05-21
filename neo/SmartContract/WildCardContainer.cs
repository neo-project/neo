using System.Collections;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public class WildCardContainer<T> : IReadOnlyList<T>
    {
        private readonly T[] _data;

        public T this[int index] => _data[index];

        /// <summary>
        /// Number of items
        /// </summary>
        public int Count => _data?.Length ?? 0;

        /// <summary>
        /// Is will card?
        /// </summary>
        public bool IsWildcard => _data == null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Data</param>
        public WildCardContainer(T[] data)
        {
            _data = data;
        }

        /// <summary>
        /// Create a new WillCardContainer
        /// </summary>
        /// <param name="data">Data</param>
        /// <returns>WillCardContainer</returns>
        public static WildCardContainer<T> Create(params T[] data) => new WildCardContainer<T>(data);

        /// <summary>
        /// Create a will card
        /// </summary>
        /// <returns>WillCardContainer</returns>
        public static WildCardContainer<T> CreateWildcard() => new WildCardContainer<T>(null);

        public IEnumerator<T> GetEnumerator() => ((IReadOnlyList<T>)_data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}