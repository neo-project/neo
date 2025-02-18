// Copyright (C) 2015-2025 The Neo Project.
//
// StorageKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the keys in contract storage.
    /// </summary>
    public sealed record StorageKey
    {
        /// <summary>
        /// The id of the contract.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// The key of the storage entry.
        /// </summary>
        public ReadOnlyMemory<byte> Key { get; init; }

        private byte[]? _cache;

        // NOTE: StorageKey is readonly, so we can cache the hash code.
        private int _hashCode = 0;

        public const int PrefixLength = sizeof(int) + sizeof(byte);

        private const int ByteLength = PrefixLength + sizeof(byte);
        private const int Int32Length = PrefixLength + sizeof(int);
        private const int Int64Length = PrefixLength + sizeof(long);
        private const int UInt160Length = PrefixLength + UInt160.Length;
        private const int UInt256Length = PrefixLength + UInt256.Length;
        private const int UInt256UInt160Length = PrefixLength + UInt256.Length + UInt160.Length;

        #region Static methods

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FillHeader(Span<byte> data, int id, byte prefix)
        {
            BinaryPrimitives.WriteInt32LittleEndian(data, id);
            data[sizeof(int)] = prefix;
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix)
        {
            var data = new byte[PrefixLength];
            FillHeader(data, id, prefix);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="content">Content</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, byte content)
        {
            var data = new byte[ByteLength];
            FillHeader(data, id, prefix);
            data[PrefixLength] = content;
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="hash">Hash</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, UInt160 hash)
        {
            var data = new byte[UInt160Length];
            FillHeader(data, id, prefix);
            hash.GetSpan().CopyTo(data.AsSpan()[PrefixLength..]);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="hash">Hash</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, UInt256 hash)
        {
            var data = new byte[UInt256Length];
            FillHeader(data, id, prefix);
            hash.GetSpan().CopyTo(data.AsSpan()[PrefixLength..]);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="publicKey">Public key</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, ECPoint publicKey)
        {
            return Create(id, prefix, publicKey.GetSpan());
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="hash">Hash</param>
        /// <param name="signer">Signer</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, UInt256 hash, UInt160 signer)
        {
            var data = new byte[UInt256UInt160Length];
            FillHeader(data, id, prefix);
            hash.GetSpan().CopyTo(data.AsSpan()[PrefixLength..]);
            signer.GetSpan().CopyTo(data.AsSpan()[UInt256Length..]);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="bigEndian">Big Endian key.</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StorageKey Create(int id, byte prefix, int bigEndian)
        {
            var data = new byte[Int32Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteInt32BigEndian(data.AsSpan()[PrefixLength..], bigEndian);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="bigEndian">Big Endian key.</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StorageKey Create(int id, byte prefix, uint bigEndian)
        {
            var data = new byte[Int32Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan()[PrefixLength..], bigEndian);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="bigEndian">Big Endian key.</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StorageKey Create(int id, byte prefix, long bigEndian)
        {
            var data = new byte[Int64Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteInt64BigEndian(data.AsSpan()[PrefixLength..], bigEndian);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="bigEndian">Big Endian key.</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StorageKey Create(int id, byte prefix, ulong bigEndian)
        {
            var data = new byte[Int64Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan()[PrefixLength..], bigEndian);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="content">Content</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, ReadOnlySpan<byte> content)
        {
            var data = new byte[PrefixLength + content.Length];
            FillHeader(data, id, prefix);
            content.CopyTo(data.AsSpan()[PrefixLength..]);
            return new(id, data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="content">Content</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, ISerializableSpan content)
        {
            return Create(id, prefix, content.GetSpan());
        }

        #endregion

        public StorageKey()
        {
            _cache = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageKey"/> class.
        /// </summary>
        /// <param name="id">Contract Id</param>
        /// <param name="cache">The cached byte array. NOTE: It must be read-only and can be modified by the caller.</param>
        private StorageKey(int id, byte[] cache)
        {
            _cache = cache;
            Id = id;
            Key = cache.AsMemory(sizeof(int));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageKey"/> class.
        /// </summary>
        /// <param name="cache">The cached byte array. NOTE: It must be read-only and can be modified by the caller.</param>
        internal StorageKey(byte[] cache)
        {
            _cache = cache;
            Id = BinaryPrimitives.ReadInt32LittleEndian(cache);
            Key = cache.AsMemory(sizeof(int));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageKey"/> class.
        /// </summary>
        /// <param name="cache">The cached byte array. NOTE: It must be read-only and can be modified by the caller.</param>
        internal StorageKey(ReadOnlySpan<byte> cache) : this(cache.ToArray()) { }

        /// <summary>
        /// Creates a search prefix for a contract.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the keys to search.</param>
        /// <returns>The created search prefix.</returns>
        public static byte[] CreateSearchPrefix(int id, ReadOnlySpan<byte> prefix)
        {
            var buffer = new byte[sizeof(int) + prefix.Length];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, id);
            prefix.CopyTo(buffer.AsSpan(sizeof(int)));
            return buffer;
        }

        public bool Equals(StorageKey? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id && Key.Span.SequenceEqual(other.Key.Span);
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
                _hashCode = HashCode.Combine(Id, Key.Span.XxHash3_32());
            return _hashCode;
        }

        public byte[] ToArray()
        {
            if (_cache is null)
            {
                _cache = new byte[sizeof(int) + Key.Length];
                BinaryPrimitives.WriteInt32LittleEndian(_cache, Id);
                Key.CopyTo(_cache.AsMemory(sizeof(int)));
            }
            return _cache;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StorageKey(byte[] value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StorageKey(ReadOnlyMemory<byte> value) => new(value.Span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StorageKey(ReadOnlySpan<byte> value) => new(value);
    }
}

#nullable disable
