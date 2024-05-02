// Copyright (C) 2015-2024 The Neo Project.
//
// G1Constants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BLS12_381;

static class G1Constants
{
    public static readonly Fp GeneratorX = Fp.FromRawUnchecked(
    [
        0x5cb3_8790_fd53_0c16,
        0x7817_fc67_9976_fff5,
        0x154f_95c7_143b_a1c1,
        0xf0ae_6acd_f3d0_e747,
        0xedce_6ecc_21db_f440,
        0x1201_7741_9e0b_fb75
    ]);

    public static readonly Fp GeneratorY = Fp.FromRawUnchecked(
    [
        0xbaac_93d5_0ce7_2271,
        0x8c22_631a_7918_fd8e,
        0xdd59_5f13_5707_25ce,
        0x51ac_5829_5040_5194,
        0x0e1c_8c3f_ad00_59c0,
        0x0bbc_3efc_5008_a26a
    ]);

    public static readonly Fp B = Fp.FromRawUnchecked(
    [
        0xaa27_0000_000c_fff3,
        0x53cc_0032_fc34_000a,
        0x478f_e97a_6b0a_807f,
        0xb1d3_7ebe_e6ba_24d7,
        0x8ec9_733b_bf78_ab2f,
        0x09d6_4551_3d83_de7e
    ]);

    public static readonly Fp BETA = Fp.FromRawUnchecked(
    [
        0x30f1_361b_798a_64e8,
        0xf3b8_ddab_7ece_5a2a,
        0x16a8_ca3a_c615_77f7,
        0xc26a_2ff8_74fd_029b,
        0x3636_b766_6070_1c6e,
        0x051b_a4ab_241b_6160
    ]);
}
