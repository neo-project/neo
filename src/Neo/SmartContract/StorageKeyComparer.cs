// Copyright (C) 2015-2026 The Neo Project.
//
// StorageKeyComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    internal class StorageKeyComparer : ByteArrayComparer, IComparer<StorageKey>
    {
        public static new readonly StorageKeyComparer Default = new(1);
        public static new readonly StorageKeyComparer Reverse = new(-1);

        private StorageKeyComparer(int direction) : base(direction) { }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(StorageKey? x, StorageKey? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            if (x is null) // y must not be null
                return -y!.Length * Direction;

            if (y is null) // x must not be null
                return x.Length * Direction;

            return x.Compare(this, y);
        }
    }
}
