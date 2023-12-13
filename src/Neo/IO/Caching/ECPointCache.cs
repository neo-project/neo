// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
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
