// Copyright (C) 2015-2025 The Neo Project.
//
// MemoryReaderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;

namespace Neo.Extensions
{
    /// <summary>
    /// A helper class for serialization of NEO objects.
    /// </summary>
    public static class MemoryReaderExtensions
    {
        /// <summary>
        /// Reads an <see cref="ISerializable"/> array from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="max">The maximum number of elements in the array.</param>
        /// <returns>The array read from the <see cref="MemoryReader"/>.</returns>
        public static T[] ReadNullableArray<T>(this ref MemoryReader reader, int max = 0x1000000)
            where T : class, ISerializable, new()
        {
            var array = new T[reader.ReadVarInt((ulong)max)];
            for (var i = 0; i < array.Length; i++)
                array[i] = reader.ReadBoolean() ? reader.ReadSerializable<T>() : null;
            return array;
        }

        /// <summary>
        /// Reads an <see cref="ISerializable"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ISerializable"/> object.</typeparam>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <returns>The object read from the <see cref="MemoryReader"/>.</returns>
        public static T ReadSerializable<T>(this ref MemoryReader reader)
            where T : ISerializable, new()
        {
            T obj = new();
            obj.Deserialize(ref reader);
            return obj;
        }

        /// <summary>
        /// Reads an <see cref="ISerializable"/> array from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="max">The maximum number of elements in the array.</param>
        /// <returns>The array read from the <see cref="MemoryReader"/>.</returns>
        public static T[] ReadSerializableArray<T>(this ref MemoryReader reader, int max = 0x1000000)
            where T : ISerializable, new()
        {
            var array = new T[reader.ReadVarInt((ulong)max)];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new T();
                array[i].Deserialize(ref reader);
            }
            return array;
        }
    }
}
