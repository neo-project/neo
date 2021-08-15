// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
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
        public byte[] Key;

        int ISerializable.Size => sizeof(int) + Key.Length;

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
            Id = reader.ReadInt32();
            Key = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public bool Equals(StorageKey other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id && MemoryExtensions.SequenceEqual<byte>(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (obj is not StorageKey other) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() + (int)Key.Murmur32(0);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Key);
        }
    }
}
