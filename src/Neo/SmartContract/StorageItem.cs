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

using Neo.Extensions;
using Neo.IO;
using Neo.VM;
using System;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the values in contract storage.
    /// </summary>
    public class StorageItem : ISerializable
    {
        private ReadOnlyMemory<byte> _value;
        private object _cache;

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
                _value = value.ToArray(); // create new memory region
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
            _value = value.AsMemory().ToArray(); // allocate new buffer
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
        /// Increases the integer value in the store by the specified value.
        /// </summary>
        /// <param name="integer">The integer to add.</param>
        public void Add(BigInteger integer)
        {
            Set(this + integer);
        }

        /// <summary>
        /// Creates a new instance of <see cref="StorageItem"/> with the same value as this instance.
        /// </summary>
        /// <returns>The created <see cref="StorageItem"/>.</returns>
        public StorageItem Clone()
        {
            var newItem = new StorageItem
            {
                _value = _value.ToArray(), // allocate new buffer
                _cache = _cache is IInteroperable interoperable ? interoperable.Clone() : _cache,
            };

            return newItem;
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
            _value = replica._value.ToArray(); // allocate new buffer. DONT USE INSTANCE
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
        public T GetInteroperable<T>() where T : IInteroperable, new()
        {
            if (_cache is null)
            {
                var interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(_value, ExecutionEngineLimits.Default));
                _cache = interoperable;
            }
            _value = ReadOnlyMemory<byte>.Empty; // garbage collect the previous allocated memory space
            return (T)_cache;
        }

        /// <summary>
        /// Gets an <see cref="IInteroperable"/> from the storage.
        /// </summary>
        /// <param name="verify">Verify deserialization</param>
        /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
        /// <returns>The <see cref="IInteroperable"/> in the storage.</returns>
        public T GetInteroperable<T>(bool verify = true) where T : IInteroperableVerifiable, new()
        {
            if (_cache is null)
            {
                var interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(_value, ExecutionEngineLimits.Default), verify);
                _cache = interoperable;
            }
            _value = ReadOnlyMemory<byte>.Empty; // garbage collect the previous allocated memory space
            return (T)_cache;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value.Span);
        }

        /// <summary>
        /// Sets the integer value of the storage.
        /// </summary>
        /// <param name="integer">The integer value to set.</param>
        public void Set(BigInteger integer)
        {
            _cache = integer;
            _value = ReadOnlyMemory<byte>.Empty; // garbage collect the previous allocated memory space
        }

        /// <summary>
        /// Sets the interoperable value of the storage.
        /// </summary>
        /// <param name="interoperable">The <see cref="IInteroperable"/> value of the <see cref="StorageItem"/>.</param>
        public void Set(IInteroperable interoperable)
        {
            _cache = interoperable;
            _value = ReadOnlyMemory<byte>.Empty; // garbage collect the previous allocated memory space
        }

        public static implicit operator BigInteger(StorageItem item)
        {
            item._cache ??= new BigInteger(item._value.Span);
            return (BigInteger)item._cache;
        }

        public static implicit operator StorageItem(BigInteger value)
        {
            return new(value);
        }

        public static implicit operator StorageItem(byte[] value)
        {
            return new(value);
        }
    }
}
