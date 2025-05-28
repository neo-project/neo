// Copyright (C) 2015-2025 The Neo Project.
//
// BlockReadSetStorage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Neo.Plugins.LedgerDebugger
{
    /// <summary>
    /// Implementation for storing and retrieving block-level read sets using a persistent store.
    /// Uses content-addressable storage for values to reduce duplication.
    /// </summary>
    public class BlockReadSetStorage : IDisposable
    {
        #region Constants and Prefixes

        /// <summary>
        /// Magic code used for prefixing all keys in the store (LDBG in ASCII)
        /// </summary>
        private static readonly int PrefixId = 0x4C444247;

        /// <summary>
        /// Prefix byte for block read set entries
        /// </summary>
        private static readonly byte PrefixBlockReadSet = 0xf0;

        /// <summary>
        /// Prefix byte for value storage in content-addressable storage
        /// </summary>
        private static readonly byte PrefixValue = 0xf1;

        /// <summary>
        /// Threshold in bytes for determining when to use content-addressable storage.
        /// Values less than or equal to this size are stored directly.
        /// Values larger than this size are stored by reference (hash).
        /// </summary>
        private const int ValueHashThreshold = 32;

        #endregion

        #region Private Fields

        /// <summary>
        /// The underlying persistent store
        /// </summary>
        private readonly IStore _store;

        /// <summary>
        /// The current snapshot of the store
        /// </summary>
        private IStoreSnapshot? _snapshot;

        /// <summary>
        /// SHA256 hasher used for content-addressable storage
        /// </summary>
        private readonly SHA256 _sha256 = SHA256.Create();

        /// <summary>
        /// Maximum number of read sets to keep
        /// </summary>
        private readonly int _maxReadSetsToKeep;

        /// <summary>
        /// Lock for thread-safe operations
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        /// Cache for recently accessed values to improve performance
        /// </summary>
        private readonly ConcurrentDictionary<string, byte[]> _valueCache = new();

        /// <summary>
        /// Maximum size of the value cache
        /// </summary>
        private const int MaxCacheSize = 1000;

        /// <summary>
        /// Tracks if the instance has been disposed
        /// </summary>
        private volatile bool _disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockReadSetStorage"/> class.
        /// </summary>
        /// <param name="path">The path where the storage will be created</param>
        /// <param name="storeProvider">The storage provider to use (default: LevelDBStore)</param>
        /// <param name="maxReadSetsToKeep">Maximum number of read sets to keep (default: 10000)</param>
        /// <exception cref="ArgumentException">Thrown if the path is invalid</exception>
        /// <exception cref="IOException">Thrown if there is an error accessing the storage location</exception>
        public BlockReadSetStorage(string path, string storeProvider = "LevelDBStore", int maxReadSetsToKeep = 10000)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Storage path cannot be null or empty", nameof(path));

            if (string.IsNullOrEmpty(storeProvider))
                throw new ArgumentException("Store provider cannot be null or empty", nameof(storeProvider));

            if (maxReadSetsToKeep < 0)
                throw new ArgumentException("Max read sets to keep cannot be negative", nameof(maxReadSetsToKeep));

            var fullPath = Path.GetFullPath(path);
            _store = StoreFactory.GetStore(storeProvider, fullPath);
            _snapshot = _store.GetSnapshot();
            _maxReadSetsToKeep = maxReadSetsToKeep;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockReadSetStorage"/> class with a custom store.
        /// </summary>
        /// <param name="store">The store implementation to use</param>
        /// <param name="maxReadSetsToKeep">Maximum number of read sets to keep before pruning old ones</param>
        /// <exception cref="ArgumentNullException">Thrown if store is null</exception>
        public BlockReadSetStorage(IStore store, int maxReadSetsToKeep = 10000)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _snapshot = _store.GetSnapshot();
            _maxReadSetsToKeep = maxReadSetsToKeep;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Adds or updates a block read set in the storage.
        /// </summary>
        /// <param name="blockIndex">The block index to associate with the read set</param>
        /// <param name="readSet">The read set to store</param>
        /// <exception cref="ArgumentNullException">Thrown if readSet is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the storage has been disposed</exception>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails</exception>
        /// <returns>True if the operation was successful</returns>
        public virtual bool Add(uint blockIndex, Dictionary<StorageKey, StorageItem> readSet)
        {
            ArgumentNullException.ThrowIfNull(readSet);
            ThrowIfDisposed();

            if (readSet.Count == 0)
            {
                // No point storing empty read sets
                return true;
            }

            _lock.EnterWriteLock();
            try
            {
                // Refresh the snapshot to ensure we have a clean state
                _snapshot?.Dispose();
                _snapshot = _store.GetSnapshot();

                var key = CreateBlockReadSetKey(blockIndex);
                var serializedData = SerializeBlockReadSet(readSet);
                _snapshot.Put(key, serializedData);

                // Apply the read set limit if needed
                if (_maxReadSetsToKeep > 0 && blockIndex >= _maxReadSetsToKeep)
                {
                    // Delete the oldest block(s) if we exceed the limit
                    var oldKey = CreateBlockReadSetKey(blockIndex - (uint)_maxReadSetsToKeep);
                    _snapshot.Delete(oldKey);

                    // Also clean up orphaned values from the cache
                    CleanupValueCache();
                }

                _snapshot.Commit();
                return true;
            }
            catch (Exception ex)
            {
                // Log the specific error for debugging
                throw new InvalidOperationException($"Failed to store read set for block {blockIndex}: {ex.Message}", ex);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Attempts to retrieve a block read set from storage.
        /// </summary>
        /// <param name="blockIndex">The block index to look up</param>
        /// <param name="readSet">The retrieved read set, or null if not found</param>
        /// <returns>True if the read set was found, otherwise false</returns>
        public bool TryGet(uint blockIndex, out Dictionary<byte[], byte[]>? readSet)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                var key = CreateBlockReadSetKey(blockIndex);

                // Ensure we have a valid snapshot
                _snapshot ??= _store.GetSnapshot();

                if (_snapshot.TryGet(key, out var value))
                {
                    readSet = DeserializeBlockReadSet(value);
                    return true;
                }

                readSet = null;
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve read set for block {blockIndex}: {ex.Message}", ex);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Disposes all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _lock.EnterWriteLock();
            try
            {
                if (!_disposed)
                {
                    _sha256?.Dispose();
                    _snapshot?.Dispose();
                    _store?.Dispose();
                    _valueCache.Clear();
                    _disposed = true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
                _lock.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks if the instance has been disposed and throws an exception if it has.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlockReadSetStorage));
        }

        /// <summary>
        /// Cleans up the value cache when it gets too large.
        /// </summary>
        private void CleanupValueCache()
        {
            if (_valueCache.Count > MaxCacheSize)
            {
                // Simple cleanup: remove half the entries
                var keysToRemove = _valueCache.Keys.Take(_valueCache.Count / 2).ToArray();
                foreach (var key in keysToRemove)
                {
                    _valueCache.TryRemove(key, out _);
                }
            }
        }

        #endregion

        #region Content-Addressable Storage Implementation

        /// <summary>
        /// Stores a value using content-addressable storage if it exceeds the threshold size.
        /// Small values (≤ ValueHashThreshold) are returned directly.
        /// Large values (> ValueHashThreshold) are stored by their hash, and the hash is returned.
        /// </summary>
        /// <param name="valueBytes">The value bytes to store</param>
        /// <returns>The value itself for small values, or its hash for large values</returns>
        private byte[] StoreValue(byte[] valueBytes)
        {
            // For small values, return them directly without additional storage
            if (valueBytes.Length <= ValueHashThreshold)
                return valueBytes;

            // For larger values, calculate hash and store value using content-addressable storage
            var hash = _sha256.ComputeHash(valueBytes);
            var valueKey = CreateValueKey(hash);

            // Only store if not already present (deduplication)
            if (!_snapshot!.Contains(valueKey))
            {
                _snapshot.Put(valueKey, valueBytes);
            }

            return hash;
        }

        /// <summary>
        /// Retrieves a value from storage, handling both direct values and hash references.
        /// Uses caching to improve performance for frequently accessed values.
        /// </summary>
        /// <param name="keyOrValue">Either a direct value (≤ ValueHashThreshold) or a hash reference</param>
        /// <returns>The actual value bytes</returns>
        private byte[] RetrieveValue(byte[] keyOrValue)
        {
            // If it's a small value, it's stored directly - return it as is
            if (keyOrValue.Length <= ValueHashThreshold)
                return keyOrValue;

            // For a hash reference, check cache first
            var hashString = Convert.ToHexString(keyOrValue);
            if (_valueCache.TryGetValue(hashString, out var cachedValue))
            {
                return cachedValue;
            }

            // Retrieve from storage and cache the result
            var valueKey = CreateValueKey(keyOrValue);
            if (_snapshot!.TryGet(valueKey, out var storedValue))
            {
                // Add to cache if not too large
                if (_valueCache.Count < MaxCacheSize)
                {
                    _valueCache.TryAdd(hashString, storedValue);
                }
                return storedValue;
            }

            // Fallback: return the hash itself if value not found
            return keyOrValue;
        }

        #endregion

        #region Key Construction and Serialization

        /// <summary>
        /// Creates a prefix for block read set queries.
        /// </summary>
        /// <returns>The prefix bytes</returns>
        private byte[] CreateBlockReadSetPrefix()
        {
            return new KeyBuilder(PrefixId, PrefixBlockReadSet).ToArray();
        }

        /// <summary>
        /// Creates a key for a specific block read set.
        /// </summary>
        /// <param name="blockIndex">The block index</param>
        /// <returns>The key bytes</returns>
        private static byte[] CreateBlockReadSetKey(uint blockIndex)
        {
            return new KeyBuilder(PrefixId, PrefixBlockReadSet)
                .AddBigEndian(blockIndex)
                .ToArray();
        }

        /// <summary>
        /// Creates a key for a value in content-addressable storage.
        /// </summary>
        /// <param name="valueHash">The hash of the value</param>
        /// <returns>The key bytes</returns>
        private static byte[] CreateValueKey(byte[] valueHash)
        {
            return new KeyBuilder(PrefixId, PrefixValue)
                .Add(valueHash)
                .ToArray();
        }

        /// <summary>
        /// Serializes a block read set to a byte array.
        /// </summary>
        /// <param name="readSet">The read set to serialize</param>
        /// <returns>The serialized block read set</returns>
        private byte[] SerializeBlockReadSet(Dictionary<StorageKey, StorageItem> readSet)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Write the number of entries
            writer.WriteVarInt(readSet.Count);

            foreach (var kvp in readSet)
            {
                // Serialize StorageKey
                writer.WriteVarBytes(kvp.Key.ToArray());

                // Serialize StorageItem Value using content-addressable storage:
                // - Small values (<= ValueHashThreshold) are stored directly
                // - Large values (> ValueHashThreshold) are stored by hash reference
                var valueBytes = kvp.Value.ToArray();
                var storedValue = StoreValue(valueBytes);

                // Write the value reference (or the small value itself)
                writer.WriteVarBytes(storedValue);
            }

            writer.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a block read set from a byte array.
        /// </summary>
        /// <param name="data">The serialized block read set</param>
        /// <returns>The deserialized block read set</returns>
        private Dictionary<byte[], byte[]> DeserializeBlockReadSet(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var count = (int)reader.ReadVarInt();
            var reads = new Dictionary<byte[], byte[]>(count);

            for (var i = 0; i < count; i++)
            {
                // Deserialize StorageKey
                var keyBytes = reader.ReadVarBytes();

                // Deserialize StorageItem value or hash reference
                var storedValue = reader.ReadVarBytes();

                // Resolve the actual value (direct or via hash lookup)
                var actualValue = RetrieveValue(storedValue);

                reads.Add(keyBytes, actualValue);
            }

            return reads;
        }

        #endregion
    }
}

