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
    public int Id { get; init; }

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

    /// <summary>
    /// Get key length
    /// </summary>
    public int Length
    {
        get
        {
            if (_cache is { IsEmpty: true })
            {
                _cache = Build();
            }
            return _cache.Length;
        }
    }

    private ReadOnlyMemory<byte> _cache;
    private readonly ReadOnlyMemory<byte> _key;

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
    /// Initializes a new instance of the <see cref="StorageKey"/> class.
    /// </summary>
    /// <param name="cache">The cached byte array.</param>
    internal StorageKey(ReadOnlySpan<byte> cache) : this(cache.ToArray(), false) { }

    internal StorageKey(ReadOnlyMemory<byte> cache, bool copy)
    {
        if (copy) cache = cache.ToArray();
        _cache = cache;
        Id = BinaryPrimitives.ReadInt32LittleEndian(_cache.Span);
        Key = _cache[sizeof(int)..]; // "Key" init makes a copy already.
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
            _cache = Build();
        }
        return _cache.ToArray(); // Make a copy
    }

    private byte[] Build()
    {
        var buffer = new byte[sizeof(int) + Key.Length];
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), Id);
        Key.CopyTo(buffer.AsMemory(sizeof(int)..));
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StorageKey(byte[] value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StorageKey(ReadOnlyMemory<byte> value) => new(value, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StorageKey(ReadOnlySpan<byte> value) => new(value);

    public override string ToString()
    {
        var keyArray = Key.ToArray();
        return $"Id = {Id}, Prefix = 0x{keyArray[0]:x02}, Key = {{ {string.Join(", ", keyArray[1..].Select(static s => $"0x{s:x02}"))} }}";
    }
}
