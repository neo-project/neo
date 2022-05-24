// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using System;
using System.Buffers.Binary;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the keys in contract storage.
    /// </summary>
    public class StorageKey : IEquatable<StorageKey>
    {
        /// <summary>
        /// The id of the contract.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// The key of the storage entry.
        /// </summary>
        public ReadOnlyMemory<byte> Key { get; init; }

        private byte[] cache = null;

        public StorageKey() { }

        public StorageKey(int id, ReadOnlyMemory<byte> key)
        {
            Id = id;
            Key = key;
        }

        internal StorageKey(byte[] cache)
        {
            this.cache = cache;
            Id = BinaryPrimitives.ReadInt32LittleEndian(cache);
            Key = cache.AsMemory(sizeof(int));
        }

        /// <summary>
        /// Creates a search prefix for a contract.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the keys to search.</param>
        /// <returns>The created search prefix.</returns>
        public static byte[] CreateSearchPrefix(int id, ReadOnlySpan<byte> prefix)
        {
            byte[] buffer = new byte[sizeof(int) + prefix.Length];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, id);
            prefix.CopyTo(buffer.AsSpan(sizeof(int)));
            return buffer;
        }

        public bool Equals(StorageKey other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id && Key.Span.SequenceEqual(other.Key.Span);
        }

        public override bool Equals(object obj)
        {
            if (obj is not StorageKey other) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Id + (int)Key.Span.Murmur32(0);
        }

        public byte[] ToArray()
        {
            if (cache is null)
            {
                cache = GC.AllocateUninitializedArray<byte>(sizeof(int) + Key.Length);
                BinaryPrimitives.WriteInt32LittleEndian(cache, Id);
                Key.CopyTo(cache.AsMemory(sizeof(int)));
            }
            return cache;
        }
    }
}
