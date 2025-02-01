// Copyright (C) 2015-2025 The Neo Project.
//
// ByteArrayEqualityComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Extensions
{
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>, IEqualityComparer
    {
        public static readonly ByteArrayEqualityComparer Instance = new();

        /// <inheritdoc />
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null || x.Length != y.Length) return false;

            return x.AsSpan().SequenceEqual(y.AsSpan());
        }

        /// <inheritdoc />
        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return true;     // Check is `null` or same object instance

            if (x is not null and not byte[])
                throw new ArgumentException($"Unable to cast '{x.GetType().FullName}' to '{typeof(byte[]).FullName}'.");

            if (y is not null and not byte[])
                throw new ArgumentException($"Unable to cast '{y.GetType().FullName}' to '{typeof(byte[]).FullName}'.");

            return Equals(x as byte[], y as byte[]);    // if x or y isn't byte array they will be `null`
        }

        /// <inheritdoc />
        public int GetHashCode([DisallowNull] byte[] obj) =>
            obj.XxHash3_32();

        /// <inheritdoc />
        public int GetHashCode([DisallowNull] object obj) =>
            obj is byte[] b ? GetHashCode(b) : 0;
    }
}
