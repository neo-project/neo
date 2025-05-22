// Copyright (C) 2015-2025 The Neo Project.
//
// MockBlockReadSetStorage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.Plugins.LedgerDebugger.Tests
{
    /// <summary>
    /// A simplified mock implementation of BlockReadSetStorage for testing.
    /// This doesn't use content-addressable storage but provides compatible Add/TryGet methods.
    /// </summary>
    public class MockBlockReadSetStorage : IDisposable
    {
        private readonly Dictionary<uint, Dictionary<byte[], byte[]>> _storage = new();
        private readonly IStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockBlockReadSetStorage"/> class.
        /// </summary>
        /// <param name="store">The store implementation to use</param>
        public MockBlockReadSetStorage(IStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Adds or updates a block read set in the storage.
        /// </summary>
        /// <param name="blockIndex">The block index to associate with the read set</param>
        /// <param name="readSet">The read set to store</param>
        /// <returns>True if the operation was successful</returns>
        public virtual bool Add(uint blockIndex, Dictionary<StorageKey, StorageItem> readSet)
        {
            if (readSet == null) return false;

            try
            {
                var byteReadSet = new Dictionary<byte[], byte[]>(new ByteArrayComparer());

                // Convert StorageKey and StorageItem to byte arrays
                foreach (var entry in readSet)
                {
                    byte[] keyBytes = entry.Key.ToArray();
                    byte[] valueBytes = entry.Value.Value.Span.ToArray();
                    byteReadSet[keyBytes] = valueBytes;
                }

                // Store in our in-memory dictionary
                _storage[blockIndex] = byteReadSet;
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
        public bool TryGet(uint blockIndex, out Dictionary<byte[], byte[]> readSet)
        {
            if (_storage.TryGetValue(blockIndex, out var storedReadSet))
            {
                readSet = storedReadSet;
                return true;
            }

            readSet = null;
            return false;
        }

        /// <summary>
        /// Disposes resources used by this class.
        /// </summary>
        public void Dispose()
        {
            _store?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Custom comparer for byte arrays
        /// </summary>
        private class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (x == null || y == null)
                    return x == y;

                return x.SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                if (obj == null)
                    return 0;

                int hash = 17;
                foreach (byte b in obj)
                {
                    hash = hash * 31 + b;
                }
                return hash;
            }
        }
    }
}
