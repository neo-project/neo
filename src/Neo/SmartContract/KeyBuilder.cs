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

using Neo.IO;
using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo.SmartContract;

/// <summary>
/// Used to build storage keys for native contracts.
/// </summary>
public class KeyBuilder : IEnumerable
{
    private readonly byte[] _cacheData;
    private int _keyLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyBuilder"/> class.
    /// </summary>
    /// <param name="id">The id of the contract.</param>
    /// <param name="prefix">The prefix of the key.</param>
    public KeyBuilder(int id, byte prefix)
    {
        _cacheData = new byte[sizeof(int) + ApplicationEngine.MaxStorageKeySize];
        BinaryPrimitives.WriteInt32LittleEndian(_cacheData, id);
        _keyLength = sizeof(int);
        _cacheData[_keyLength++] = prefix;
    }

    IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

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
        _cacheData[_keyLength++] = key;
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
        key.CopyTo(_cacheData.AsSpan(_keyLength..));
        _keyLength += key.Length;
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
    public KeyBuilder Add<T>(T key) where T : unmanaged
    {
        if (!typeof(T).IsPrimitive)
            throw new InvalidOperationException("The argument must be a primitive.");
        Span<byte> data = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        if (BitConverter.IsLittleEndian) data.Reverse();
        return Add(data);
    }

    internal static bool TryParse(byte[] data, Type[] types, out int id, out byte prefix, out object[] values)
    {
        values = Array.Empty<object>();

        foreach (var t in types)
        {
            if (!t.IsValueType || t.IsGenericType)
            {
                id = default;
                prefix = default;
                return false;
            }
        }

        if (data.Length < sizeof(int) + 1)
        {
            id = default;
            prefix = default;
            return false;
        }

        id = BinaryPrimitives.ReadInt32LittleEndian(data);
        prefix = data[sizeof(int)];

        values = new object[types.Length];
        int offset = sizeof(int) + 1;

        for (int i = 0; i < types.Length; i++)
        {
            int size = Marshal.SizeOf(types[i]);
            if (data.Length < offset + size)
                return false;

            Span<byte> span = data.AsSpan(offset, size);

            if (BitConverter.IsLittleEndian)
                span.Reverse();

            values[i] = ReadStruct(span, types[i]);
            offset += size;
        }

        return true;
    }

    static object ReadStruct(Span<byte> span, Type t)
    {
        byte[] buffer = span.ToArray();
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t)!;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Gets the storage key generated by the builder.
    /// </summary>
    /// <returns>The storage key.</returns>
    public byte[] ToArray() => _cacheData[.._keyLength];

    public static implicit operator StorageKey(KeyBuilder builder) => new(builder._cacheData.AsMemory(0, builder._keyLength), false);
}
