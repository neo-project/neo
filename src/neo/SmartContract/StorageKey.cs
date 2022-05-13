// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.IO;
using System;
using System.Buffers.Binary;
using System.IO;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the keys in contract storage.
    /// </summary>
    public class StorageKey : IEquatable<StorageKey>, ISerializable
    {
        /// <summary>
        /// The id of the contract.
        /// </summary>
        public int Id;

        /// <summary>
        /// The key of the storage entry.
        /// </summary>
        public ReadOnlyMemory<byte> Key;

        private byte[] cache = null;

        int ISerializable.Size => sizeof(int) + Key.Length;

        public StorageKey() { }

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

        //If the base stream of the reader doesn't support seeking, a NotSupportedException is thrown.
        //But StorageKey never works with NetworkStream, so it doesn't matter.
        void ISerializable.Deserialize(BinaryReader reader)
        {
            cache = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            Id = BinaryPrimitives.ReadInt32LittleEndian(cache);
            Key = cache.AsMemory(sizeof(int));
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
            return Id.GetHashCode() + (int)Key.Span.Murmur32(0);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            if (cache != null)
            {
                writer.Write(cache);
            }
            else
            {
                writer.Write(Id);
                writer.Write(Key.Span);
            }
        }

        public byte[] ToArray()
        {
            if (cache is null)
            {
                using MemoryStream ms = new(sizeof(int) + Key.Length);
                using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
                writer.Write(Id);
                writer.Write(Key.Span);
                writer.Flush();
                cache = ms.ToArray();
            }
            return cache;
        }
    }
}
