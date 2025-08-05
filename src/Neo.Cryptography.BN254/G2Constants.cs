// Copyright (C) 2015-2025 The Neo Project.
//
// G2Constants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BN254
{
    public static class G2Constants
    {
        // G2 curve constant b' = 3/ξ in Montgomery form
        // For BN254, ξ = 9+u where u is the BN parameter
        // These values represent b' in Fp2 Montgomery form
        public static readonly Fp2 B = new(
            new Fp(new ulong[] { 0x2b149d40ceb8aaae, 0x3a18e4a61c076267, 0x45c2ac2962a12902, 0x09192585375e4d42 }),
            new Fp(new ulong[] { 0x0c54bba1d6f46fef, 0x5d784e17b8c00409, 0x21f828ff3dc8ca4d, 0x009075b4ee4d3ff4 })
        );
    }
}
