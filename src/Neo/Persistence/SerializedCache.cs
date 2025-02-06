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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.Persistence
{
    public class SerializedCache
    {
        private readonly Dictionary<Type, IStorageCacheEntry> _cache = [];

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(T? value) where T : IStorageCacheEntry
        {
            Set(typeof(T), value);
        }

        /// <summary>
        /// Set entry
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="value">Value</param>
        public void Set(Type type, IStorageCacheEntry? value)
        {
            if (value == null)
            {
                Remove(type);
            }
            else
            {
                lock (_cache)
                {
                    _cache[type] = value;
                }
            }
        }

        /// <summary>
        /// Remove entry
        /// </summary>
        /// <param name="type">Type</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(Type type)
        {
            lock (_cache)
            {
                _cache.Remove(type, out _);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            lock (_cache)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Copy from
        /// </summary>
        /// <param name="value">Value</param>
        public void CopyFrom(SerializedCache value)
        {
            lock (_cache) lock (value._cache)
                {
                    foreach (var serialized in value._cache)
                    {
                        _cache[serialized.Key] = serialized.Value;
                    }
                }
        }
    }
}
