// Copyright (C) 2015-2025 The Neo Project.
//
// SerializedCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neo.Persistence
{
    public class SerializedCache
    {
        private readonly ConcurrentDictionary<Type, object> _cache = new();

        /// <summary>
        /// Get cached entry
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>Cache</returns>
        public T? Get<T>()
        {
            if (_cache.TryGetValue(typeof(T), out var ret))
            {
                return (T)ret;
            }

            return default;
        }

        /// <summary>
        /// Set entry
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="value">Value</param>
        public void Set<T>(T? value)
        {
            Set(typeof(T), value);
        }

        /// <summary>
        /// Set entry
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="value">Value</param>
        public void Set(Type type, object? value)
        {
            if (value == null) _cache.Remove(type, out _);
            else _cache[type] = value;
        }
    }
}
