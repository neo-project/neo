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

using Neo.Extensions;
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

        private Memory<byte> _cache;

        // NOTE: StorageKey is readonly, so we can cache the hash code.
        private int _hashCode = 0;

        public StorageKey() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageKey"/> class.
        /// </summary>
        /// <param name="cache">The cached byte array. NOTE: It must be read-only and can be modified by the caller.</param>
        internal StorageKey(byte[] cache)
        {
            _cache = cache.AsMemory().ToArray();    // allocate new buffer
            Id = BinaryPrimitives.ReadInt32LittleEndian(_cache.Span);
            Key = _cache[sizeof(int)..].ToArray();  // allocate new buffer. NOTE: DONT USE POINTERS HERE
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
            return buffer.ToArray();
        }

        public bool Equals(StorageKey other)
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
                _cache = new byte[sizeof(int) + Key.Length];    // allocate new buffer
                BinaryPrimitives.WriteInt32LittleEndian(_cache.Span, Id);
                Key.CopyTo(_cache[sizeof(int)..]);
            }
            return _cache.ToArray();                            // allocate new buffer
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StorageKey(byte[] value) => new(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StorageKey(ReadOnlyMemory<byte> value) => new(value.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StorageKey(ReadOnlySpan<byte> value) => new(value.ToArray());
    }
}
