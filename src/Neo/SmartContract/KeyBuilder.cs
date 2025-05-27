// Copyright (C) 2015-2025 The Neo Project.
//
// KeyBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.IO;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    /// <summary>
    /// Used to build storage keys for native contracts.
    /// </summary>
    public class KeyBuilder
    {
        public const int PrefixLength = sizeof(int) + sizeof(byte);

        private readonly Memory<byte> _cacheData;
        private int _keyLength = 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBuilder"/> class.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="maxLength">The hint of the storage key size(Not including the id and prefix).</param>
        public KeyBuilder(int id, byte prefix, int maxLength = ApplicationEngine.MaxStorageKeySize)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "Must be greater than or equal to zero.");

            _cacheData = new byte[maxLength + PrefixLength];
            BinaryPrimitives.WriteInt32LittleEndian(_cacheData.Span, id);

            _keyLength = sizeof(int);
            _cacheData.Span[_keyLength++] = prefix;
        }

        private void CheckLength(int length)
        {
            if ((length + _keyLength) > _cacheData.Length)
                throw new OverflowException("Input data too Large!");
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(byte key)
        {
            CheckLength(1);
            _cacheData.Span[_keyLength++] = key;
            return this;
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(ReadOnlySpan<byte> key)
        {
            CheckLength(key.Length);
            key.CopyTo(_cacheData.Span[_keyLength..]);
            _keyLength += key.Length;
            return this;
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key represented by a byte array.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(byte[] key) =>
            Add(key.AsSpan());

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder Add(ISerializableSpan key) =>
            Add(key.GetSpan());

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder AddBigEndian(int key)
        {
            Span<byte> data = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder AddBigEndian(uint key)
        {
            Span<byte> data = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder AddBigEndian(long key)
        {
            Span<byte> data = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Adds part of the key to the builder in BigEndian.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder AddBigEndian(ulong key)
        {
            Span<byte> data = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(data, key);

            return Add(data);
        }

        /// <summary>
        /// Gets the storage key generated by the builder.
        /// </summary>
        /// <returns>The storage key.</returns>
        public byte[] ToArray() =>
            _cacheData[.._keyLength].ToArray();

        public static implicit operator StorageKey(KeyBuilder builder) =>
            new(builder.ToArray());
    }
}

#nullable disable
