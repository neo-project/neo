// Copyright (C) 2015-2025 The Neo Project.
//
// FpConstants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BN254
{
    static class FpConstants
    {
        // BN254 prime field modulus
        // p = 21888242871839275222246405745257275088696311157297823662689037894645226208583

        // Montgomery form R = 2^256 mod p
        public static readonly Fp R = new(new ulong[]
        {
            0xd35d438dc58f0d9d,
            0x0a78eb28f5c70b3d,
            0x666ea36f7879462c,
            0x0e0a77c19a07df2f
        });

        // R^2 mod p
        public static readonly Fp R2 = new(new ulong[]
        {
            0xf32cfc5b538afa89,
            0xb5e71911d44501fb,
            0x47ab1eff0a417ff6,
            0x06d89f71cab8351f
        });

        // R^3 mod p
        public static readonly Fp R3 = new(new ulong[]
        {
            0xb1cd6dafda1530df,
            0x62f210e6a7283db6,
            0xef7f0b0c0ada0afb,
            0x20fd6e902d592544
        });

        // -p^(-1) mod 2^64 = 0x87d20782e4866389
        public const ulong INV = 0x87d20782e4866389;

        // Generator of the multiplicative group
        public static readonly Fp GENERATOR = new(new ulong[]
        {
            0xefffffff1,
            0x17e363d300189c0f,
            0xff9c57876f8457b0,
            0x351332208fc5a8c4
        });

        // 2^s * t = p - 1 with t odd
        public const uint S = 28;

        // Quadratic residue and non-residue
        public static readonly Fp QUADRATIC_NON_RESIDUE = new(new ulong[]
        {
            0x68c3488912edefaa,
            0x8d087f6872aabf4f,
            0x51e1a24709081231,
            0x2259d6b14729c0fa
        });
    }
}
