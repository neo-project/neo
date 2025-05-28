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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        /// LRU cache for recently accessed values to improve performance
        /// </summary>
        private readonly LruCache<string, byte[]> _valueCache = new(1000);

        /// <summary>
        /// Bloom filter for fast existence checks
        /// </summary>
        private readonly BloomFilter _bloomFilter = new(100000, 0.01); // 100k items, 1% false positive rate

        /// <summary>
        /// Storage metrics for monitoring efficiency
        /// </summary>
        private readonly StorageMetrics _metrics = new();

        /// <summary>
        /// Tracks if the instance has been disposed
        /// </summary>
        private volatile bool _disposed = false;

        /// <summary>
        /// Minimum size for compression (bytes)
        /// </summary>
        private const int CompressionThreshold = 1024; // 1KB

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the BlockReadSetStorage class.
        /// </summary>
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
        /// Initializes a new instance with a custom store.
        /// </summary>
        public BlockReadSetStorage(IStore store, int maxReadSetsToKeep = 10000)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _snapshot = _store.GetSnapshot();
            _maxReadSetsToKeep = maxReadSetsToKeep;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Adds or updates a block read set in storage.
        /// </summary>
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

                    // LRU cache automatically handles cleanup, no manual intervention needed
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
        /// Disposes all resources.
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
                    _bloomFilter.Clear();
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
        /// Checks if disposed and throws exception if needed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlockReadSetStorage));
        }

        /// <summary>
        /// Gets storage efficiency metrics.
        /// </summary>
        public StorageMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Performs storage maintenance and optimization.
        /// </summary>
        public void PerformMaintenance()
        {
            _lock.EnterWriteLock();
            try
            {
                // Clear cache to free memory
                _valueCache.Clear();

                // Reset bloom filter periodically to prevent saturation
                if (_metrics.StoreAttempts > 50000)
                {
                    _bloomFilter.Clear();
                    _metrics.Reset();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Content-Addressable Storage Implementation

        /// <summary>
        /// Stores value using content-addressable storage with compression.
        /// </summary>
        private byte[] StoreValue(byte[] valueBytes)
        {
            _metrics.IncrementStoreAttempts();

            // For small values, return them directly without additional storage
            if (valueBytes.Length <= ValueHashThreshold)
            {
                _metrics.IncrementSmallValues();
                return valueBytes;
            }

            // Calculate hash for deduplication
            var hash = _sha256.ComputeHash(valueBytes);
            var hashString = Convert.ToHexString(hash);
            var valueKey = CreateValueKey(hash);

            // Check bloom filter first for fast negative lookups
            if (!_bloomFilter.Contains(hashString))
            {
                // Definitely not present, so we need to store it
                var dataToStore = valueBytes;

                // Apply compression if the value is large enough and compression is beneficial
                if (valueBytes.Length >= CompressionThreshold)
                {
                    var compressed = CompressData(valueBytes);
                    if (compressed.Length < valueBytes.Length * 0.9) // Only use if >10% reduction
                    {
                        dataToStore = compressed;
                        _metrics.IncrementCompressedValues();
                    }
                }

                _snapshot!.Put(valueKey, dataToStore);
                _bloomFilter.Add(hashString);
                _metrics.IncrementStoredValues();
                _metrics.AddStorageBytes(dataToStore.Length);
            }
            else
            {
                // Might be present, check actual storage
                if (!_snapshot!.Contains(valueKey))
                {
                    // False positive in bloom filter
                    var dataToStore = valueBytes;

                    if (valueBytes.Length >= CompressionThreshold)
                    {
                        var compressed = CompressData(valueBytes);
                        if (compressed.Length < valueBytes.Length * 0.9)
                        {
                            dataToStore = compressed;
                            _metrics.IncrementCompressedValues();
                        }
                    }

                    _snapshot.Put(valueKey, dataToStore);
                    _metrics.IncrementStoredValues();
                    _metrics.AddStorageBytes(dataToStore.Length);
                }
                else
                {
                    _metrics.IncrementDeduplicatedValues();
                }
            }

            return hash;
        }

        /// <summary>
        /// Retrieves value from storage with LRU caching and decompression.
        /// </summary>
        private byte[] RetrieveValue(byte[] keyOrValue)
        {
            _metrics.IncrementRetrieveAttempts();

            // If it's a small value, it's stored directly - return it as is
            if (keyOrValue.Length <= ValueHashThreshold)
            {
                _metrics.IncrementCacheHits(); // Small values are effectively cached
                return keyOrValue;
            }

            // For a hash reference, check LRU cache first
            var hashString = Convert.ToHexString(keyOrValue);
            if (_valueCache.TryGetValue(hashString, out var cachedValue))
            {
                _metrics.IncrementCacheHits();
                return cachedValue;
            }

            // Cache miss - retrieve from storage
            _metrics.IncrementCacheMisses();
            var valueKey = CreateValueKey(keyOrValue);
            if (_snapshot!.TryGet(valueKey, out var storedValue))
            {
                // Decompress if needed (detect compression by trying to decompress)
                var actualValue = TryDecompressData(storedValue);

                // Add to LRU cache (automatically handles eviction)
                _valueCache.Set(hashString, actualValue);

                return actualValue;
            }

            // Fallback: return the hash itself if value not found
            return keyOrValue;
        }

        /// <summary>
        /// Compresses data using GZip.
        /// </summary>
        private static byte[] CompressData(byte[] data)
        {
            using var output = new MemoryStream();
            output.WriteByte(1); // Compression marker
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(data);
            }
            return output.ToArray();
        }

        /// <summary>
        /// Attempts to decompress data.
        /// </summary>
        private static byte[] TryDecompressData(byte[] data)
        {
            if (data.Length == 0 || data[0] != 1) // Check compression marker
                return data;

            try
            {
                using var input = new MemoryStream(data, 1, data.Length - 1); // Skip marker
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
            catch
            {
                // If decompression fails, return original data
                return data;
            }
        }

        #endregion

        #region Key Construction and Serialization

        /// <summary>
        /// Creates prefix for block read set queries.
        /// </summary>
        private byte[] CreateBlockReadSetPrefix()
        {
            return new KeyBuilder(PrefixId, PrefixBlockReadSet).ToArray();
        }

        /// <summary>
        /// Creates key for specific block read set.
        /// </summary>
        private static byte[] CreateBlockReadSetKey(uint blockIndex)
        {
            return new KeyBuilder(PrefixId, PrefixBlockReadSet)
                .AddBigEndian(blockIndex)
                .ToArray();
        }

        /// <summary>
        /// Creates key for content-addressable storage value.
        /// </summary>
        private static byte[] CreateValueKey(byte[] valueHash)
        {
            return new KeyBuilder(PrefixId, PrefixValue)
                .Add(valueHash)
                .ToArray();
        }

        /// <summary>
        /// Serializes block read set to byte array.
        /// </summary>
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
        /// Deserializes block read set from byte array.
        /// </summary>
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

    /// <summary>
    /// LRU cache implementation for efficient value caching.
    /// </summary>
    internal class LruCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new();

        public LruCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
                value = default!;
                return false;
            }
        }

        public void Set(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    // Update existing item
                    existingNode.Value.Value = value;
                    _lruList.Remove(existingNode);
                    _lruList.AddFirst(existingNode);
                }
                else
                {
                    // Add new item
                    if (_cache.Count >= _capacity)
                    {
                        // Remove least recently used item
                        var lru = _lruList.Last!;
                        _lruList.RemoveLast();
                        _cache.Remove(lru.Value.Key);
                    }

                    var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                    _lruList.AddFirst(newNode);
                    _cache[key] = newNode;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        private class CacheItem
        {
            public TKey Key { get; }
            public TValue Value { get; set; }

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
    }

    /// <summary>
    /// Bloom filter for fast existence checks.
    /// </summary>
    internal class BloomFilter
    {
        private readonly BitArray _bits;
        private readonly int _hashFunctions;
        private readonly int _size;

        public BloomFilter(int expectedItems, double falsePositiveRate)
        {
            // Calculate optimal size and hash functions
            _size = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
            _hashFunctions = (int)Math.Ceiling(_size * Math.Log(2) / expectedItems);
            _bits = new BitArray(_size);
        }

        public void Add(string item)
        {
            var hashes = GetHashes(item);
            for (int i = 0; i < _hashFunctions; i++)
            {
                var index = Math.Abs((hashes[0] + i * hashes[1]) % _size);
                _bits[index] = true;
            }
        }

        public bool Contains(string item)
        {
            var hashes = GetHashes(item);
            for (int i = 0; i < _hashFunctions; i++)
            {
                var index = Math.Abs((hashes[0] + i * hashes[1]) % _size);
                if (!_bits[index])
                    return false;
            }
            return true;
        }

        public void Clear()
        {
            _bits.SetAll(false);
        }

        private static int[] GetHashes(string item)
        {
            var hash1 = item.GetHashCode();
            var hash2 = item.GetHashCode(StringComparison.Ordinal);
            return new[] { hash1, hash2 };
        }
    }

    /// <summary>
    /// Storage metrics for monitoring efficiency.
    /// </summary>
    public class StorageMetrics
    {
        private long _storeAttempts;
        private long _retrieveAttempts;
        private long _cacheHits;
        private long _cacheMisses;
        private long _smallValues;
        private long _storedValues;
        private long _compressedValues;
        private long _deduplicatedValues;
        private long _totalStorageBytes;

        public long StoreAttempts => _storeAttempts;
        public long RetrieveAttempts => _retrieveAttempts;
        public long CacheHits => _cacheHits;
        public long CacheMisses => _cacheMisses;
        public long SmallValues => _smallValues;
        public long StoredValues => _storedValues;
        public long CompressedValues => _compressedValues;
        public long DeduplicatedValues => _deduplicatedValues;
        public long TotalStorageBytes => _totalStorageBytes;

        public double CacheHitRate => _retrieveAttempts > 0 ? (double)_cacheHits / _retrieveAttempts : 0;
        public double CompressionRate => _storedValues > 0 ? (double)_compressedValues / _storedValues : 0;
        public double DeduplicationRate => _storeAttempts > 0 ? (double)_deduplicatedValues / _storeAttempts : 0;

        public void IncrementStoreAttempts() => Interlocked.Increment(ref _storeAttempts);
        public void IncrementRetrieveAttempts() => Interlocked.Increment(ref _retrieveAttempts);
        public void IncrementCacheHits() => Interlocked.Increment(ref _cacheHits);
        public void IncrementCacheMisses() => Interlocked.Increment(ref _cacheMisses);
        public void IncrementSmallValues() => Interlocked.Increment(ref _smallValues);
        public void IncrementStoredValues() => Interlocked.Increment(ref _storedValues);
        public void IncrementCompressedValues() => Interlocked.Increment(ref _compressedValues);
        public void IncrementDeduplicatedValues() => Interlocked.Increment(ref _deduplicatedValues);
        public void AddStorageBytes(long bytes) => Interlocked.Add(ref _totalStorageBytes, bytes);

        public void Reset()
        {
            Interlocked.Exchange(ref _storeAttempts, 0);
            Interlocked.Exchange(ref _retrieveAttempts, 0);
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
            Interlocked.Exchange(ref _smallValues, 0);
            Interlocked.Exchange(ref _storedValues, 0);
            Interlocked.Exchange(ref _compressedValues, 0);
            Interlocked.Exchange(ref _deduplicatedValues, 0);
            Interlocked.Exchange(ref _totalStorageBytes, 0);
        }
    }
}
