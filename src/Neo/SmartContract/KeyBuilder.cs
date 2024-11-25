// Copyright (C) 2015-2024 The Neo Project.
//
// KeyBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
        private readonly MemoryStream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBuilder"/> class.
        /// </summary>
        /// <param name="id">The id of the contract.</param>
        /// <param name="prefix">The prefix of the key.</param>
        /// <param name="keySizeHint">The hint of the storage key size.</param>
        public KeyBuilder(int id, byte prefix, int keySizeHint = ApplicationEngine.MaxStorageKeySize)
        {
            Span<byte> data = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(data, id);

            stream = new(keySizeHint);
            stream.Write(data);
            stream.WriteByte(prefix);
        }

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(byte key)
        {
            stream.WriteByte(key);
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
            stream.Write(key);
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
        /// <param name="key">Part of the key represented by a <see cref="UInt160"/>.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(UInt160 key) => Add(key.GetSpan());

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key represented by a <see cref="UInt256"/>.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyBuilder Add(UInt256 key) => Add(key.GetSpan());

        /// <summary>
        /// Adds part of the key to the builder.
        /// </summary>
        /// <param name="key">Part of the key.</param>
        /// <returns>A reference to this instance after the add operation has completed.</returns>
        public KeyBuilder Add(ISerializable key)
        {
            using (BinaryWriter writer = new(stream, Utility.StrictUTF8, true))
            {
                key.Serialize(writer);
                writer.Flush();
            }
            return this;
        }

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
            using (stream)
            {
                return stream.ToArray();
            }
        }

        public static implicit operator StorageKey(KeyBuilder builder)
        {
            using (builder.stream)
            {
                return new StorageKey(builder.stream.ToArray());
            }
        }
    }
}
