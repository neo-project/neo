// Copyright (C) 2015-2025 The Neo Project.
//
// ScalarConstants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BN254
{
    static class ScalarConstants
    {
        // BN254 scalar field modulus
        // r = 21888242871839275222246405745257275088548364400416034343698204186575808495617
        public static readonly Scalar MODULUS = new(new ulong[]
        {
            0x43e1f593f0000001,
            0x2833e84879b97091,
            0xb85045b68181585d,
            0x30644e72e131a029
        });

        // Montgomery form R = 2^256 mod r
        public static readonly Scalar R = new(new ulong[]
        {
            0xac96341c4ffffffb,
            0x36fc76959f60cd29,
            0x666ea36f7879462e,
            0x0e0a77c19a07df2f
        });

        // R^2 mod r
        public static readonly Scalar R2 = new(new ulong[]
        {
            0x1bb8e645ae216da7,
            0x53fe3ab1e35c59e3,
            0x8c49833d53bb8085,
            0x0216d0b17f4e44a5
        });

        // R^3 mod r
        public static readonly Scalar R3 = new(new ulong[]
        {
            0x5e94d8e1b4bf0040,
            0x2a489cbe1cfbb6b8,
            0x893cc664a19fcfed,
            0x0cf8594b7fcc657c
        });

        // -r^(-1) mod 2^64
        public const ulong INV = 0x43e1f593efffffff;
    }
}