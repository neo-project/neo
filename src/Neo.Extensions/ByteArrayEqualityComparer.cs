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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Extensions
{
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>, IEqualityComparer
    {
        public static readonly ByteArrayEqualityComparer Default = new();

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.Length != y.Length) return false;

            return GetHashCode(x) == GetHashCode(y);
        }

        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return true;
            return Equals(x as byte[], y as byte[]);
        }

        public int GetHashCode([DisallowNull] byte[] obj) =>
            obj.XxHash3_32();

        public int GetHashCode([DisallowNull] object obj) =>
            obj is byte[] b ? GetHashCode(b) : 0;
    }
}
