// Copyright (C) 2015-2025 The Neo Project.
//
// ReversePoolItemComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System.Collections.Generic;

namespace Neo.Ledger
{
    internal class ReversePoolItemComparer : IComparer<PoolItem>
    {
        public static readonly ReversePoolItemComparer Instance = new();

        public int Compare(PoolItem? x, PoolItem? y)
        {
            if (x == null)
            {
                if (y == null) return 0;

                return y.CompareTo(x); // Already reversed
            }

            return x.CompareTo(y) * -1;
        }
    }
}

#nullable disable
