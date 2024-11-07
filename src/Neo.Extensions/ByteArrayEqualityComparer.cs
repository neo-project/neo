// Copyright (C) 2015-2024 The Neo Project.
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
using System.Collections.Generic;
using System.Linq;

namespace Neo.Extensions
{
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Default = new();

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null || x.Length != y.Length) return false;

            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            return obj.XxHash3_32();
        }
    }
}
