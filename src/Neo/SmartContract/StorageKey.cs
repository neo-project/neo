// Copyright (C) 2015-2026 The Neo Project.
//
// StorageKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract;

/// <summary>
/// Represents the keys in contract storage.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed record StorageKey
{
    /// <summary>
    /// The id of the contract.
    /// </summary>
    public int Id
    {
        get => _id;
        init
        {
            if (!_memory.IsEmpty)
                BinaryPrimitives.WriteInt32LittleEndian(_memory.Span, value);
            _id = value;
        }
    }

    /// <summary>
    /// The key of the storage entry.
    /// </summary>
    public ReadOnlyMemory<byte> Key
    {
        get => _memory.IsEmpty ? ReadOnlyMemory<byte>.Empty : _memory[sizeof(int)..];
        init
        {
            _memory = new byte[(sizeof(int) + value.Length)];
            BinaryPrimitives.WriteInt32LittleEndian(_memory.Span, Id);
            value.CopyTo(_memory[sizeof(int)..]);
        }
    }

    /// <summary>
    /// Get StorageKey length(sizeof(int) + key.Length)
    /// </summary>
    public int Length => _memory.Length;

    private readonly int _id;

    private readonly Memory<byte> _memory;

    // NOTE: StorageKey is readonly, so we can cache the hash code.
    private int _hashCode = 0;

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
        prefix.CopyTo(buffer.AsSpan(sizeof(int)..));
        return buffer;
    }

    public StorageKey() { }

    /// <summary>
    /// Initializes a StorageKey from not shared memory.
    /// The size must greater than sizeof(int).
    /// </summary>
    internal StorageKey(Memory<byte> memory)
    {
        _memory = memory;
        Id = BinaryPrimitives.ReadInt32LittleEndian(_memory.Span);
    }

    public bool Equals(StorageKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && _memory.Span.SequenceEqual(other._memory.Span);
    }

    public override int GetHashCode()
    {
        if (_hashCode == 0)
            _hashCode = HashCode.Combine(Id, Key.Span.XxHash3_32());
        return _hashCode;
    }

    public byte[] ToArray()
    {
        if (_memory.IsEmpty)
        {
            var buffer = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, Id);
            return buffer;
        }
        return _memory.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StorageKey(byte[] value) => new(value[..].AsMemory());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StorageKey(ReadOnlyMemory<byte> value) => new(value.ToArray().AsMemory());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StorageKey(ReadOnlySpan<byte> value) => new(value.ToArray().AsMemory());

    public override string ToString()
    {
        return _memory.IsEmpty ? $"StorageKey{{Id={Id}}}" : $"StorageKey{{Id={Id},Key={Key.ToHexString()}}}";
    }
}
