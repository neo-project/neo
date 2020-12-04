using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Manifest
{
    public class WildcardContainer<T> : IReadOnlyList<T>
    {
        private readonly T[] _data;

        public T this[int index] => _data[index];

        /// <summary>
        /// Number of items
        /// </summary>
        public int Count => _data?.Length ?? 0;

        /// <summary>
        /// Is wildcard?
        /// </summary>
        public bool IsWildcard => _data is null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Data</param>
        private WildcardContainer(T[] data)
        {
            _data = data;
        }

        /// <summary>
        /// Create a new WildCardContainer
        /// </summary>
        /// <param name="data">Data</param>
        /// <returns>WildCardContainer</returns>
        public static WildcardContainer<T> Create(params T[] data) => new WildcardContainer<T>(data);

        /// <summary>
        /// Create a wildcard
        /// </summary>
        /// <returns>WildCardContainer</returns>
        public static WildcardContainer<T> CreateWildcard() => new WildcardContainer<T>(null);

        public static WildcardContainer<T> FromJson(JObject json, Func<JObject, T> elementSelector)
        {
            switch (json)
            {
                case JString str:
                    if (str.Value != "*") throw new FormatException();
                    return CreateWildcard();
                case JArray array:
                    return Create(array.Select(p => elementSelector(p)).ToArray());
                default:
                    throw new FormatException();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_data == null) return ((IReadOnlyList<T>)new T[0]).GetEnumerator();

            return ((IReadOnlyList<T>)_data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T entry)
        {
            return _data.Contains(entry);
        }

        public JObject ToJson()
        {
            if (IsWildcard) return "*";
            return _data.Select(p => (JObject)p.ToString()).ToArray();
        }
    }
}
