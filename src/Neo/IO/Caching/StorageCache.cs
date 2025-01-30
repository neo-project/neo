// Copyright (C) 2015-2025 The Neo Project.
//
// StorageCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO.Caching
{
    /// <summary>
    /// Implements <see cref="IStorageCache{TKey, TValue}"/> using a dictionary to
    /// store its entries.
    /// </summary>
    internal class StorageCache<TKey, TValue>(IStore store) : IStorageCache<TKey, TValue>
        where TKey : class, IKeySerializable
        where TValue : class, ISerializable, new()
    {
        public int Size => _cacheEntries.Count;

        private static readonly TimeSpan s_scanTimeIntervals = TimeSpan.FromMinutes(1);

        public static DateTime UtcNow => DateTime.UtcNow;

        private readonly ConcurrentDictionary<TKey, StorageEntry<TKey, TValue>> _cacheEntries = new(KeyValueSerializableEqualityComparer<TKey>.Instance);
        private readonly IStore _store = store ?? throw new ArgumentNullException(nameof(store));

        private DateTime _lastExpirationScan;
        private bool _disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the cache and clears all entries.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to dispose the object resources; <see langword="false" /> to take no action.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing)
                {
                    _cacheEntries.Clear();
                    GC.SuppressFinalize(this);
                }

                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void AddOrUpdate(TKey key, TValue value)
        {
            ValidateCacheKey(key);
            CheckDisposed();

            var utcNow = UtcNow;

            if (_cacheEntries.TryGetValue(key, out var tmp))
            {
                var entry = tmp;

                entry.LastAccessed = utcNow;
                entry.Value = value;
                entry.SetExpirationTimeRelativeTo(utcNow);

                _store.Put(key.ToArray(), value.ToArray());
            }
            else
            {
                var entry = new StorageEntry<TKey, TValue>(key, this)
                {
                    Value = value,
                    LastAccessed = utcNow,
                };

                entry.SetExpirationTimeRelativeTo(utcNow);

                if (_cacheEntries.TryAdd(key, entry))
                    _store.Put(key.ToArray(), value.ToArray());
            }

            StartScanForExpiredItemsIfNeeded(utcNow);
        }

        /// <inheritdoc />
        public void Remove(TKey key)
        {
            ValidateCacheKey(key);
            CheckDisposed();

            if (_cacheEntries.TryRemove(key, out var entry))
                entry.SetExpired(CacheEvictionReason.Removed);

            StartScanForExpiredItemsIfNeeded(UtcNow);
        }

        public void Delete(TKey key)
        {
            ValidateCacheKey(key);
            CheckDisposed();

            if (_cacheEntries.TryRemove(key, out var entry))
            {
                entry.SetExpired(CacheEvictionReason.Removed);
                _store.Delete(key.ToArray());
            }

            StartScanForExpiredItemsIfNeeded(UtcNow);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue value)
        {
            ValidateCacheKey(key);
            CheckDisposed();

            var utcNow = UtcNow;

            if (_cacheEntries.TryGetValue(key, out var tmp))
            {
                var entry = tmp;

                if (entry.CheckExpired(utcNow))
                {
                    if (_store.TryGet(key.ToArray(), out var rawEntryValue) == false)
                        _cacheEntries.TryRemove(entry.Key, out _);
                    else
                    {
                        entry.LastAccessed = utcNow;
                        value = rawEntryValue.AsSerializable<TValue>();
                        entry.Value = value;

                        entry.SetExpirationTimeRelativeTo(utcNow);
                        StartScanForExpiredItemsIfNeeded(utcNow);

                        return true;
                    }
                }
                else
                {
                    entry.LastAccessed = utcNow;
                    value = entry.Value;

                    entry.SetExpirationTimeRelativeTo(utcNow);
                    StartScanForExpiredItemsIfNeeded(utcNow);

                    return true;
                }
            }
            else
            {
                if (_store.TryGet(key.ToArray(), out var rawEntryValue))
                {
                    var entry = new StorageEntry<TKey, TValue>(key, this)
                    {
                        Value = rawEntryValue.AsSerializable<TValue>(),
                        LastAccessed = utcNow,
                    };

                    entry.SetExpirationTimeRelativeTo(utcNow);

                    if (_cacheEntries.TryAdd(key, entry))
                    {
                        value = entry.Value;
                        return true;
                    }
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);

            value = default;
            return false;
        }

        /// <summary>
        /// Removes all keys and values from the cache.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            foreach (var entry in _cacheEntries.Values)
            {
                entry.SetExpired(CacheEvictionReason.Removed);

                if (_cacheEntries.TryRemove(entry.Key, out _))
                    StartScanForExpiredItemsIfNeeded(UtcNow);
            }
        }

        private static void ValidateCacheKey(TKey key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
        }

        private void CheckDisposed()
        {
            if (_disposed)
                Throw();

            [DoesNotReturn]
            static void Throw() => throw new ObjectDisposedException(typeof(StorageCache<TKey, TValue>).FullName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartScanForExpiredItemsIfNeeded(DateTime utcNow)
        {
            if (utcNow - _lastExpirationScan >= s_scanTimeIntervals)
                ScheduleTask(utcNow);

            void ScheduleTask(DateTime utcNow)
            {
                _lastExpirationScan = utcNow;
                Task.Factory.StartNew(state => ((StorageCache<TKey, TValue>)state!).ScanForExpiredItems(), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private void ScanForExpiredItems()
        {
            var utcNow = _lastExpirationScan = UtcNow;

            foreach (var entry in _cacheEntries.Values)
            {
                if (entry.CheckExpired(utcNow))
                    _cacheEntries.TryRemove(entry.Key, out _);
            }
        }
    }
}
