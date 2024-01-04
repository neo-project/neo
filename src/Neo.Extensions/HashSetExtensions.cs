// Copyright (C) 2015-2024 The Neo Project.
//
// HashSetExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.Extensions
{
    public static class HashSetExtensions
    {
        internal static void Remove<T>(this HashSet<T> set, ISet<T> other)
        {
            if (set.Count > other.Count)
                set.ExceptWith(other);
            else
                set.RemoveWhere(u => other.Contains(u));
        }

        internal static void Remove<TKey, TValue>(this HashSet<TKey> set, IReadOnlyDictionary<TKey, TValue> other)
        {
            if (set.Count > other.Count)
                set.ExceptWith(other.Keys);
            else
                set.RemoveWhere(u => other.ContainsKey(u));
        }
    }
}
