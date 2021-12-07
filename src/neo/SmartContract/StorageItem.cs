// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the values in contract storage.
    /// </summary>
    public class StorageItem : ISerializable
    {
        private byte[] value;
        private object cache;

        public int Size => Value.GetVarSize();

        /// <summary>
        /// The byte array value of the <see cref="StorageItem"/>.
        /// </summary>
        public byte[] Value
        {
            get
            {
                return value ??= cache switch
                {
                    BigInteger bi => bi.ToByteArrayStandard(),
                    IInteroperable interoperable => BinarySerializer.Serialize(interoperable.ToStackItem(null), 1024 * 1024),
                    IReadOnlyCollection<ISerializable> list => list.ToByteArray(),
                    null => null,
                    _ => throw new InvalidCastException()
                };
            }
            set
            {
                this.value = value;
                cache = null;
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
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageItem"/> class.
        /// </summary>
        /// <param name="value">The integer value of the <see cref="StorageItem"/>.</param>
        public StorageItem(BigInteger value)
        {
            this.cache = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageItem"/> class.
        /// </summary>
        /// <param name="interoperable">The <see cref="IInteroperable"/> value of the <see cref="StorageItem"/>.</param>
        public StorageItem(IInteroperable interoperable)
        {
            this.cache = interoperable;
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
            return new StorageItem
            {
                Value = Value
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadBytes((int)(reader.BaseStream.Length));
        }

        /// <summary>
        /// Copies the value of another <see cref="StorageItem"/> instance to this instance.
        /// </summary>
        /// <param name="replica">The instance to be copied.</param>
        public void FromReplica(StorageItem replica)
        {
            Value = replica.Value;
        }

        /// <summary>
        /// Gets an <see cref="IInteroperable"/> from the storage.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IInteroperable"/>.</typeparam>
        /// <returns>The <see cref="IInteroperable"/> in the storage.</returns>
        public T GetInteroperable<T>() where T : IInteroperable, new()
        {
            if (cache is null)
            {
                var interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(value, ExecutionEngineLimits.Default));
                cache = interoperable;
            }
            value = null;
            return (T)cache;
        }

        /// <summary>
        /// Gets a list of <see cref="ISerializable"/> from the storage.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ISerializable"/>.</typeparam>
        /// <returns>The list of the <see cref="ISerializable"/>.</returns>
        public List<T> GetSerializableList<T>() where T : ISerializable, new()
        {
            cache ??= new List<T>(value.AsSerializableArray<T>());
            value = null;
            return (List<T>)cache;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        /// <summary>
        /// Sets the integer value of the storage.
        /// </summary>
        /// <param name="integer">The integer value to set.</param>
        public void Set(BigInteger integer)
        {
            cache = integer;
            value = null;
        }

        public static implicit operator BigInteger(StorageItem item)
        {
            item.cache ??= new BigInteger(item.value);
            return (BigInteger)item.cache;
        }
    }
}
