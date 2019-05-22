using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public class WildCardContainer<T> : IReadOnlyList<T>
    {
        private readonly T[] _data;

        [JsonIgnore]
        public T this[int index] => _data[index];

        /// <summary>
        /// Number of items
        /// </summary>
        [JsonIgnore]
        public int Count => _data?.Length ?? 0;

        /// <summary>
        /// Is will card?
        /// </summary>
        [JsonIgnore]
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
        /// Constructor
        /// </summary>
        public WildCardContainer() : this(null) { }

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

        public IEnumerator<T> GetEnumerator()
        {
            if (_data == null) return ((IReadOnlyList<T>)new T[0]).GetEnumerator();

            return ((IReadOnlyList<T>)_data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}