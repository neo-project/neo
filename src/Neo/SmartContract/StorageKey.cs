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
        public int Id
        {
            get => _id;
            init => _id = value;
        }

        /// <summary>
        /// The key of the storage entry.
        /// </summary>
        public ReadOnlyMemory<byte> Key
        {
            get => _key;
            // The example below shows how you would of been
            // able to overwrite keys in the pass
            // Example:
            //      byte[] keyData = [0x00, 0x00, 0x00, 0x00, 0x12];
            //      var keyMemory = new ReadOnlyMemory<byte>(keyData);
            //      var storageKey1 = new StorageKey { Id = 0, Key = keyMemory };
            //      // Below will overwrite the key in "storageKey1.Key"
            //      keyData[0] = 0xff;
            init => _key = value.ToArray(); // make new region of memory (a copy).
        }

        private Memory<byte> _cache;
        private readonly int _id;
        private readonly ReadOnlyMemory<byte> _key;

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
            Span<byte> data = stackalloc byte[PrefixLength];
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
            Span<byte> data = stackalloc byte[ByteLength];
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
            Span<byte> data = stackalloc byte[UInt160Length];
            FillHeader(data, id, prefix);
            hash.GetSpan().CopyTo(data[PrefixLength..]);
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
            Span<byte> data = stackalloc byte[UInt256Length];
            FillHeader(data, id, prefix);
            hash.GetSpan().CopyTo(data[PrefixLength..]);
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
            Span<byte> data = stackalloc byte[UInt256UInt160Length];
            FillHeader(data, id, prefix);
            hash.GetSpan().CopyTo(data[PrefixLength..]);
            signer.GetSpan().CopyTo(data[UInt256Length..]);
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
            Span<byte> data = stackalloc byte[Int32Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteInt32BigEndian(data[PrefixLength..], bigEndian);
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
            Span<byte> data = stackalloc byte[Int32Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteUInt32BigEndian(data[PrefixLength..], bigEndian);
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
            Span<byte> data = stackalloc byte[Int64Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteInt64BigEndian(data[PrefixLength..], bigEndian);
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
            Span<byte> data = stackalloc byte[Int64Length];
            FillHeader(data, id, prefix);
            BinaryPrimitives.WriteUInt64BigEndian(data[PrefixLength..], bigEndian);
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
            Span<byte> data = stackalloc byte[PrefixLength + content.Length];
            FillHeader(data, id, prefix);
            content.CopyTo(data[PrefixLength..]);
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
        private StorageKey(int id, ReadOnlySpan<byte> cache)
        {
            // DO NOT CHANGE OR ELSE "Create" WILL HAVE PROBLEMS
            _cache = cache.ToArray(); // Make a copy
            Id = id;
            Key = _cache[sizeof(int)..]; // "Key" init makes a copy already.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageKey"/> class.
        /// </summary>
        /// <param name="cache">The cached byte array. NOTE: It must be read-only and can be modified by the caller.</param>
        internal StorageKey(ReadOnlySpan<byte> cache)
        {
            // DO NOT CHANGE OR ELSE "Create" WILL HAVE PROBLEMS
            _cache = cache.ToArray(); // Make a copy
            Id = BinaryPrimitives.ReadInt32LittleEndian(cache);
            Key = _cache[sizeof(int)..]; // "Key" init makes a copy already.
        }

        /// <summary>
        /// Creates a search prefix for a contract.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the keys to search.</param>
        /// <returns>The created search prefix.</returns>
        public static byte[] CreateSearchPrefix(int id, ReadOnlySpan<byte> prefix)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int) + prefix.Length];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, id);
            prefix.CopyTo(buffer[sizeof(int)..]);
            return [.. buffer]; // Copy off stack
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
            if (_cache is { IsEmpty: true })
            {
                _cache = new byte[sizeof(int) + Key.Length];
                BinaryPrimitives.WriteInt32LittleEndian(_cache.Span, Id);
                Key.CopyTo(_cache[sizeof(int)..]);
            }
            return _cache.ToArray(); // Make a copy
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
