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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockReadSetStorage"/> class.
        /// </summary>
        /// <param name="path">The path where the storage will be created</param>
        /// <exception cref="ArgumentException">Thrown if the path is invalid</exception>
        /// <exception cref="IOException">Thrown if there is an error accessing the storage location</exception>
        public BlockReadSetStorage(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Storage path cannot be null or empty", nameof(path));

            var fullPath = Path.GetFullPath(path);
            _store = StoreFactory.GetStore(Settings.Default?.StoreProvider ?? "LevelDBStore", fullPath);
            _snapshot = _store.GetSnapshot();
            _maxReadSetsToKeep = Settings.Default?.MaxReadSetsToKeep ?? 10000;
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
        /// <returns>True if the operation was successful</returns>
        public virtual bool Add(uint blockIndex, Dictionary<StorageKey, StorageItem> readSet)
        {
            ArgumentNullException.ThrowIfNull(readSet);

            try
            {
                // Refresh the snapshot to ensure we have a clean state
                _snapshot?.Dispose();
                _snapshot = _store.GetSnapshot();

                var key = CreateBlockReadSetKey(blockIndex);
                _snapshot.Put(key, SerializeBlockReadSet(readSet));

                // Apply the read set limit if needed
                if (_maxReadSetsToKeep > 0)
                {
                    // Simply delete the oldest block(s) if we exceed the limit
                    if (blockIndex >= _maxReadSetsToKeep)
                    {
                        var oldKey = CreateBlockReadSetKey((uint)(blockIndex - _maxReadSetsToKeep));
                        _snapshot.Delete(oldKey);
                    }
                }

                _snapshot.Commit();
                return true;
            }
            catch
            {
                return false;
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

        /// <summary>
        /// Disposes all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            _sha256?.Dispose();
            _snapshot?.Dispose();
            _store?.Dispose();
            GC.SuppressFinalize(this);
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
        /// </summary>
        /// <param name="keyOrValue">Either a direct value (≤ ValueHashThreshold) or a hash reference</param>
        /// <returns>The actual value bytes</returns>
        private byte[] RetrieveValue(byte[] keyOrValue)
        {
            // If it's a small value, it's stored directly - return it as is
            if (keyOrValue.Length <= ValueHashThreshold)
                return keyOrValue;

            // For a hash reference, retrieve the actual value from content-addressable storage
            var valueKey = CreateValueKey(keyOrValue);
            return _snapshot!.TryGet(valueKey, out var storedValue) ? storedValue : keyOrValue;
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

