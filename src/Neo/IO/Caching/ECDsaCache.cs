// Copyright (C) 2015-2024 The Neo Project.
//
// ECDsaCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Security.Cryptography;

namespace Neo.IO.Caching
{
    record ECDsaCacheItem(Cryptography.ECC.ECPoint key, ECDsa value);
    internal class ECDsaCache : FIFOCache<Cryptography.ECC.ECPoint, ECDsaCacheItem>
    {
        public ECDsaCache(int max_capacity = 20000) : base(max_capacity, EqualityComparer<Cryptography.ECC.ECPoint>.Default)
        {
        }

        protected override Cryptography.ECC.ECPoint GetKeyForItem(ECDsaCacheItem item)
        {
            return item.key;
        }
    }
}
