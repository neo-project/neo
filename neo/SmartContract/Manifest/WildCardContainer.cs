using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Manifest
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
        public bool IsWildcard => _data is null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Data</param>
        private WildCardContainer(T[] data)
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

        public static WildCardContainer<T> FromJson(JObject json, Func<JObject, T> elementSelector)
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

        public JObject ToJson()
        {
            if (IsWildcard) return "*";
            return _data.Select(p =>
            {
                switch (p)
                {
                    case IJsonSerializable serializable:
                        return serializable.ToJson();
                    default:
                        return p.ToString();
                }
            }).ToArray();
        }
    }
}