// Copyright (C) 2015-2025 The Neo Project.
//
// KeyValueSerializableEqualityComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Extensions
{
    internal class KeyValueSerializableEqualityComparer<TKey> : IEqualityComparer<TKey>
        where TKey : IKeySerializable
    {
        public static readonly KeyValueSerializableEqualityComparer<TKey> Default = new();

        public bool Equals(TKey x, TKey y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.ToArray().AsSpan().SequenceEqual(y.ToArray().AsSpan());
        }

        public int GetHashCode(TKey obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            return obj.GetHashCode();
        }
    }
}
