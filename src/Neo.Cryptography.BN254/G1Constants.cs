// Copyright (C) 2015-2025 The Neo Project.
//
// G1Constants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BN254
{
    static class G1Constants
    {
        // Generator point G1 (Montgomery form)
        public static readonly Fp GENERATOR_X = new(new ulong[]
        {
            0xd35d438dc58f0d9d,
            0x0a78eb28f5c70b3d,
            0x666ea36f7879462c,
            0x0e0a77c19a07df2f
        });

        public static readonly Fp GENERATOR_Y = new(new ulong[]
        {
            0xa6ba871b8b1e1b3a,
            0x14f1d651eb8e167b,
            0xccdd46def0f28c58,
            0x1c14ef83340fbe5e
        });

        // Curve parameter b = 3
        public static readonly Fp B = new(new ulong[]
        {
            0x0000000000000003,
            0x0000000000000000,
            0x0000000000000000,
            0x0000000000000000
        });
    }
}