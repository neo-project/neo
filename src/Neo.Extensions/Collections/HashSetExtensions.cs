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
        public static void Remove<T>(this HashSet<T> set, ISet<T> other)
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other);
            }
            else
            {
                set.RemoveWhere(u => other.Contains(u));
            }
        }

        public static void Remove<T, V>(this HashSet<T> set, IReadOnlyDictionary<T, V> other)
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other.Keys);
            }
            else
            {
                set.RemoveWhere(u => other.ContainsKey(u));
            }
        }
    }
}
