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
        // Generator point G1: (1, 2) converted to Montgomery form
        // x = 1 * R mod p = R mod p
        public static readonly Fp GENERATOR_X = FpConstants.R;

        // y = 2 * R mod p = (2R) mod p = R + R mod p
        public static readonly Fp GENERATOR_Y = FpConstants.R + FpConstants.R;

        // Curve parameter b = 3 in Montgomery form = 3*R mod p
        public static readonly Fp B = FpConstants.R + FpConstants.R + FpConstants.R;
    }
}
