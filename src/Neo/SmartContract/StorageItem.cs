// Copyright (C) 2015-2025 The Neo Project.
//
// StorageItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Extensions;
using Neo.IO;
using Neo.VM;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract;

/// <summary>
/// Represents the values in contract storage.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public class StorageItem : ISerializable
{
    private class SealInteroperable(StorageItem item) : IDisposable
    {
        public readonly StorageItem Item = item;

        public void Dispose()
        {
            Item.Seal();
        }
    }

    private ReadOnlyMemory<byte> _value;
    private object? _cache;

    public int Size => Value.GetVarSize();

    /// <summary>
    /// The byte array value of the <see cref="StorageItem"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Value
    {
        get
        {
            return !_value.IsEmpty ? _value : _value = _cache switch
            {
                BigInteger bi => bi.ToByteArrayStandard(),
                IInteroperable interoperable => BinarySerializer.Serialize(interoperable.ToStackItem(null), ExecutionEngineLimits.Default),
                null => ReadOnlyMemory<byte>.Empty,
                _ => throw new InvalidCastException()
            };
        }
        set
        {
            _value = value;
            _cache = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem"/> class.
    /// </summary>
    public StorageItem() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem"/> class.
    /// </summary>
    /// <param name="value">The byte array value of the <see cref="StorageItem"/>.</param>
    public StorageItem(byte[] value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem"/> class.
    /// </summary>
    /// <param name="value">The integer value of the <see cref="StorageItem"/>.</param>
    public StorageItem(BigInteger value)
    {
        _cache = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem"/> class.
    /// </summary>
    /// <param name="interoperable">The <see cref="IInteroperable"/> value of the <see cref="StorageItem"/>.</param>
    public StorageItem(IInteroperable interoperable)
    {
        _cache = interoperable;
    }

    /// <summary>
    /// Create a new instance from an sealed <see cref="IInteroperable"/> class.
    /// </summary>
    /// <param name="interoperable">The <see cref="IInteroperable"/> value of the <see cref="StorageItem"/>.</param>
    /// <returns><see cref="StorageItem"/> class</returns>
    public static StorageItem CreateSealed(IInteroperable interoperable)
    {
        var item = new StorageItem(interoperable);
        item.Seal();
        return item;
    }

    /// <summary>
    /// Returns true if the <see cref="IInteroperable"/> class is serializable
    /// </summary>
    /// <param name="interoperable">The <see cref="IInteroperable"/> value of the <see cref="StorageItem"/>.</param>
    /// <returns>True if serializable</returns>
    public static bool IsSerializable(IInteroperable interoperable)
    {
        try
        {
            _ = CreateSealed(interoperable);
        }
        catch
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Ensure that is Serializable and cache the value
    /// </summary>
    public void Seal()
    {
        // Assert is Serializable and cached
        _ = Value;
    }

    /// <summary>
    /// Increases the integer value in the store by the specified value.
    /// </summary>
    /// <param name="integer">The integer to add.</param>
    public BigInteger Add(BigInteger integer)
    {
        return Set(this + integer);
    }

    /// <summary>
    /// Creates a new instance of <see cref="StorageItem"/> with the same value as this instance.
    /// </summary>
    /// <returns>The created <see cref="StorageItem"/>.</returns>
    public StorageItem Clone()
    {
        return new()
        {
            _value = _value,
            _cache = _cache is IInteroperable interoperable ? interoperable.Clone() : _cache
        };
    }

    public void Deserialize(ref MemoryReader reader)
    {
        Value = reader.ReadToEnd();
    }

    /// <summary>
    /// Copies the value of another <see cref="StorageItem"/> instance to this instance.
    /// </summary>
    /// <param name="replica">The instance to be copied.</param>
    public void FromReplica(StorageItem replica)
    {
        _value = replica._value;
        if (replica._cache is IInteroperable interoperable)
        {
            if (_cache?.GetType() == interoperable.GetType())
                ((IInteroperable)_cache).FromReplica(interoperable);
            else
                _cache = interoperable.Clone();
        }
        else
        {
            _cache = replica._cache;
        }
    }

    /// <summary>
    /// Gets an <see cref="IInteroperable"/> from the storage.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
    /// <returns>The <see cref="IInteroperable"/> in the storage.</returns>
    public T GetInteroperable<T>() where T : IInteroperable
    {
        _cache ??= GetInteroperableClone<T>();
        _value = null;
        return (T)_cache;
    }

    /// <summary>
    /// Gets an <see cref="IInteroperableVerifiable"/> from the storage.
    /// </summary>
    /// <param name="verify">Verify deserialization</param>
    /// <typeparam name="T">The type of the <see cref="IInteroperableVerifiable"/>.</typeparam>
    /// <returns>The <see cref="IInteroperableVerifiable"/> in the storage.</returns>
    public T GetInteroperable<T>(bool verify = true) where T : IInteroperableVerifiable
    {
        _cache ??= GetInteroperableClone<T>(verify);
        _value = null;
        return (T)_cache;
    }

    /// <summary>
    /// Gets an <see cref="IInteroperable"/> from the storage not related to this <see cref="StorageItem"/>.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
    /// <returns>The <see cref="IInteroperable"/> in the storage.</returns>
    public T GetInteroperableClone<T>() where T : IInteroperable
    {
        // If it's interoperable and not sealed
        if (_value.IsEmpty && _cache is T interoperable)
        {
            // Refresh data without change _value
            return (T)interoperable.Clone();
        }

        interoperable = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        interoperable.FromStackItem(BinarySerializer.Deserialize(_value, ExecutionEngineLimits.Default));
        return interoperable;
    }

    /// <summary>
    /// Gets an <see cref="IInteroperableVerifiable"/> from the storage not related to this <see cref="StorageItem"/>.
    /// </summary>
    /// <param name="verify">Verify deserialization</param>
    /// <typeparam name="T">The type of the <see cref="IInteroperableVerifiable"/>.</typeparam>
    /// <returns>The <see cref="IInteroperableVerifiable"/> in the storage.</returns>
    public T GetInteroperableClone<T>(bool verify = true) where T : IInteroperableVerifiable
    {
        // If it's interoperable and not sealed
        if (_value.IsEmpty && _cache is T interoperable)
        {
            return (T)interoperable.Clone();
        }

        interoperable = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        interoperable.FromStackItem(BinarySerializer.Deserialize(_value, ExecutionEngineLimits.Default), verify);
        return interoperable;
    }

    /// <summary>
    /// Gets an <see cref="IInteroperable"/> from the storage.
    /// </summary>
    /// <param name="interop">The <see cref="IInteroperable"/> in the storage.</param>
    /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
    /// <returns>The <see cref="IDisposable"/> that seal the item when disposed.</returns>
    public IDisposable GetInteroperable<T>(out T interop) where T : IInteroperable, new()
    {
        interop = GetInteroperable<T>();
        return new SealInteroperable(this);
    }

    /// <summary>
    /// Gets an <see cref="IInteroperable"/> from the storage.
    /// </summary>
    /// <param name="interop">The <see cref="IInteroperable"/> in the storage.</param>
    /// <param name="verify">Verify deserialization</param>
    /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
    /// <returns>The <see cref="IDisposable"/> that seal the item when disposed.</returns>
    public IDisposable GetInteroperable<T>(out T interop, bool verify = true) where T : IInteroperableVerifiable
    {
        interop = GetInteroperable<T>(verify);
        return new SealInteroperable(this);
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Value.Span);
    }

    /// <summary>
    /// Sets the integer value of the storage.
    /// </summary>
    /// <param name="integer">The integer value to set.</param>
    public BigInteger Set(BigInteger integer)
    {
        _cache = integer;
        _value = null;
        return integer;
    }

    public static implicit operator BigInteger(StorageItem item)
    {
        item._cache ??= new BigInteger(item._value.Span);
        return (BigInteger)item._cache;
    }

    public static implicit operator StorageItem(BigInteger value)
    {
        return new StorageItem(value);
    }

    public static implicit operator StorageItem(byte[] value)
    {
        return new StorageItem(value);
    }

    public override string ToString()
    {
        var valueArray = _value.ToArray();
        return $"Value = {{ {string.Join(", ", valueArray.Select(static s => $"0x{s:x02}"))} }}";
    }
}
