// Copyright (C) 2015-2024 The Neo Project.
//
// ICollectionExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Neo.Extensions
{
    public static class ICollectionExtensions
    {
        /// <summary>
        /// Gets the size of the specified array encoded in variable-length encoding.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The specified array.</param>
        /// <returns>The size of the array.</returns>
        public static int GetVarSize<T>(this IReadOnlyCollection<T> value)
        {
            int value_size;
            var t = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(t))
            {
                value_size = value.OfType<ISerializable>().Sum(p => p.Size);
            }
            else if (t.GetTypeInfo().IsEnum)
            {
                int element_size;
                var u = t.GetTypeInfo().GetEnumUnderlyingType();
                if (u == typeof(sbyte) || u == typeof(byte))
                    element_size = 1;
                else if (u == typeof(short) || u == typeof(ushort))
                    element_size = 2;
                else if (u == typeof(int) || u == typeof(uint))
                    element_size = 4;
                else //if (u == typeof(long) || u == typeof(ulong))
                    element_size = 8;
                value_size = value.Count * element_size;
            }
            else
            {
                value_size = value.Count * Marshal.SizeOf<T>();
            }
            return UnsafeData.GetVarSize(value.Count) + value_size;
        }

        /// <summary>
        /// Converts an <see cref="ISerializable"/> array to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The <see cref="ISerializable"/> array to be converted.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] ToByteArray<T>(this IReadOnlyCollection<T> value)
            where T : ISerializable
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            writer.Write(value);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
