// Copyright (C) 2015-2025 The Neo Project.
//
// ISerializableExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.IO;

namespace Neo.Extensions
{
    public static class ISerializableExtensions
    {
        /// <summary>
        /// Converts an <see cref="ISerializable"/> object to a byte array.
        /// </summary>
        /// <param name="value">The <see cref="ISerializable"/> object to be converted.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] ToArray(this ISerializable value)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            value.Serialize(writer);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
