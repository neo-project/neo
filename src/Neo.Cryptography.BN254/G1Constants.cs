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
        // Generator point G1
        public static readonly Fp GENERATOR_X = new(new ulong[]
        {
            0x0000000000000001,
            0x0000000000000000,
            0x0000000000000000,
            0x0000000000000000
        });

        public static readonly Fp GENERATOR_Y = new(new ulong[]
        {
            0x0000000000000002,
            0x0000000000000000,
            0x0000000000000000,
            0x0000000000000000
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