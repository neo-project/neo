// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Json;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// A list that supports wildcard.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class WildcardContainer<T> : IReadOnlyList<T>
    {
        private readonly T[] _data;

        public T this[int index] => _data[index];

        public int Count => _data?.Length ?? 0;

        /// <summary>
        /// Indicates whether the list is a wildcard.
        /// </summary>
        public bool IsWildcard => _data is null;

        private WildcardContainer(T[] data)
        {
            _data = data;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WildcardContainer{T}"/> class with the initial elements.
        /// </summary>
        /// <param name="data">The initial elements.</param>
        /// <returns>The created list.</returns>
        public static WildcardContainer<T> Create(params T[] data) => new(data);

        /// <summary>
        /// Creates a new instance of the <see cref="WildcardContainer{T}"/> class with wildcard.
        /// </summary>
        /// <returns>The created list.</returns>
        public static WildcardContainer<T> CreateWildcard() => new(null);

        /// <summary>
        /// Converts the list from a JSON object.
        /// </summary>
        /// <param name="json">The list represented by a JSON object.</param>
        /// <param name="elementSelector">A converter for elements.</param>
        /// <returns>The converted list.</returns>
        public static WildcardContainer<T> FromJson(JToken json, Func<JToken, T> elementSelector)
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
            if (_data == null) return ((IReadOnlyList<T>)Array.Empty<T>()).GetEnumerator();

            return ((IReadOnlyList<T>)_data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Converts the list to a JSON object.
        /// </summary>
        /// <returns>The list represented by a JSON object.</returns>
        public JToken ToJson(Func<T, JToken> elementSelector)
        {
            if (IsWildcard) return "*";
            return _data.Select(p => elementSelector(p)).ToArray();
        }
    }
}
