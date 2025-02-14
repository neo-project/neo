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

using Neo.Extensions;
using Neo.IO;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    /// <summary>
    /// Used to build storage keys for native contracts.
    /// </summary>
    public class KeyBuilder
    {
        private readonly MemoryStream _stream;

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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
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
            return new(data);
        }

        /// <summary>
        /// Create StorageKey
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="serializable">Serializable</param>
        /// <returns>The <see cref="StorageKey"/> class</returns>
        public static StorageKey Create(int id, byte prefix, ISerializable serializable)
        {
            return Create(id, prefix, serializable.ToArray().AsSpan());
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBuilder"/> class.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="keySizeHint">The hint of the storage key size(including the id and prefix).</param>
        public KeyBuilder(int id, byte prefix, int keySizeHint = ApplicationEngine.MaxStorageKeySize)
        {
            Span<byte> data = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(data, id);

            _stream = new(keySizeHint);
            _stream.Write(data);
            _stream.WriteByte(prefix);
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(byte key)
        {
            _stream.WriteByte(key);
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
            _stream.Write(key);
            return this;
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key represented by a byte array.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(byte[] key) => Add(key.AsSpan());

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder Add(ISerializableSpan key) => Add(key.GetSpan());

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
        public byte[] ToArray()
        {
            using (_stream)
            {
                return _stream.ToArray();
            }
        }

        public static implicit operator StorageKey(KeyBuilder builder)
        {
            return new StorageKey(builder.ToArray());
        }
    }
}

#nullable disable
