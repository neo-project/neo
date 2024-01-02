// Copyright (C) 2015-2024 The Neo Project.
//
// ECPointCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;

namespace Neo.IO.Caching
{
    internal class ECPointCache : FIFOCache<byte[], ECPoint>
    {
        public ECPointCache(int max_capacity)
            : base(max_capacity, ByteArrayEqualityComparer.Default)
        {
        }

        protected override byte[] GetKeyForItem(ECPoint item)
        {
            return item.EncodePoint(true);
        }
    }
}
