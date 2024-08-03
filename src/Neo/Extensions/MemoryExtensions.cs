// Copyright (C) 2015-2024 The Neo Project.
//
// MemoryExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Reflection;

namespace Neo.Extensions
{
    public static class MemoryExtensions
    {
        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The byte array to be converted.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static T AsSerializable<T>(this ReadOnlyMemory<byte> value)
            where T : ISerializable, new()
        {
            if (value.IsEmpty) throw new FormatException();
            MemoryReader reader = new(value);
            return reader.ReadSerializable<T>();
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="type">The type to convert to.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static ISerializable AsSerializable(this ReadOnlyMemory<byte> value, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidCastException();
            var serializable = (ISerializable)Activator.CreateInstance(type);
            MemoryReader reader = new(value);
            serializable.Deserialize(ref reader);
            return serializable;
        }

        /// <summary>
        /// Gets the size of the specified array encoded in variable-length encoding.
        /// </summary>
        /// <param name="value">The specified array.</param>
        /// <returns>The size of the array.</returns>
        public static int GetVarSize(this ReadOnlyMemory<byte> value)
        {
            return UnsafeData.GetVarSize(value.Length) + value.Length;
        }
    }
}
