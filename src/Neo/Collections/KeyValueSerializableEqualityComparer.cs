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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Extensions
{
    internal class KeyValueSerializableEqualityComparer<TKey> : IEqualityComparer<TKey>, IEqualityComparer
        where TKey : class, IKeySerializable
    {
        public static readonly KeyValueSerializableEqualityComparer<TKey> Instance = new();

        public bool Equals(TKey x, TKey y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.ToArray().AsSpan().SequenceEqual(y.ToArray().AsSpan());
        }

        public new bool Equals(object x, object y)
        {
            return Equals(x as TKey, y as TKey);
        }

        public int GetHashCode(TKey obj)
        {
            return obj is null ? 0 : obj.GetHashCode();
        }

        public int GetHashCode(object obj)
        {
            return obj is TKey t ? GetHashCode(t) : 0;
        }
    }
}
