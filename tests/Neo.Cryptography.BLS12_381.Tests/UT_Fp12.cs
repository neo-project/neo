// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Fp12.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BLS12_381.Tests;

[TestClass]
public class UT_Fp12
{
    [TestMethod]
    public void TestArithmetic()
    {
        var a = new Fp12(new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x47f9_cb98_b1b8_2d58,
            0x5fe9_11eb_a3aa_1d9d,
            0x96bf_1b5f_4dd8_1db3,
            0x8100_d27c_c925_9f5b,
            0xafa2_0b96_7464_0eab,
            0x09bb_cea7_d8d9_497d
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0303_cb98_b166_2daa,
            0xd931_10aa_0a62_1d5a,
            0xbfa9_820c_5be4_a468,
            0x0ba3_643e_cb05_a348,
            0xdc35_34bb_1f1c_25a6,
            0x06c3_05bb_19c0_e1c1
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x46f9_cb98_b162_d858,
            0x0be9_109c_f7aa_1d57,
            0xc791_bc55_fece_41d2,
            0xf84c_5770_4e38_5ec2,
            0xcb49_c1d9_c010_e60f,
            0x0acd_b8e1_58bf_e3c8
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x8aef_cb98_b15f_8306,
            0x3ea1_108f_e4f2_1d54,
            0xcf79_f69f_a1b7_df3b,
            0xe4f5_4aa1_d16b_1a3c,
            0xba5e_4ef8_6105_a679,
            0x0ed8_6c07_97be_e5cf
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xcee5_cb98_b15c_2db4,
            0x7159_1082_d23a_1d51,
            0xd762_30e9_44a1_7ca4,
            0xd19e_3dd3_549d_d5b6,
            0xa972_dc17_01fa_66e3,
            0x12e3_1f2d_d6bd_e7d6
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xad2a_cb98_b173_2d9d,
            0x2cfd_10dd_0696_1d64,
            0x0739_6b86_c6ef_24e8,
            0xbd76_e2fd_b1bf_c820,
            0x6afe_a7f6_de94_d0d5,
            0x1099_4b0c_5744_c040
        }))), new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x47f9_cb98_b1b8_2d58,
            0x5fe9_11eb_a3aa_1d9d,
            0x96bf_1b5f_4dd8_1db3,
            0x8100_d27c_c925_9f5b,
            0xafa2_0b96_7464_0eab,
            0x09bb_cea7_d8d9_497d
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0303_cb98_b166_2daa,
            0xd931_10aa_0a62_1d5a,
            0xbfa9_820c_5be4_a468,
            0x0ba3_643e_cb05_a348,
            0xdc35_34bb_1f1c_25a6,
            0x06c3_05bb_19c0_e1c1
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x46f9_cb98_b162_d858,
            0x0be9_109c_f7aa_1d57,
            0xc791_bc55_fece_41d2,
            0xf84c_5770_4e38_5ec2,
            0xcb49_c1d9_c010_e60f,
            0x0acd_b8e1_58bf_e3c8
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x8aef_cb98_b15f_8306,
            0x3ea1_108f_e4f2_1d54,
            0xcf79_f69f_a1b7_df3b,
            0xe4f5_4aa1_d16b_1a3c,
            0xba5e_4ef8_6105_a679,
            0x0ed8_6c07_97be_e5cf
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xcee5_cb98_b15c_2db4,
            0x7159_1082_d23a_1d51,
            0xd762_30e9_44a1_7ca4,
            0xd19e_3dd3_549d_d5b6,
            0xa972_dc17_01fa_66e3,
            0x12e3_1f2d_d6bd_e7d6
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xad2a_cb98_b173_2d9d,
            0x2cfd_10dd_0696_1d64,
            0x0739_6b86_c6ef_24e8,
            0xbd76_e2fd_b1bf_c820,
            0x6afe_a7f6_de94_d0d5,
            0x1099_4b0c_5744_c040
        }))));

        var b = new Fp12(new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x47f9_cb98_b1b8_2d58,
            0x5fe9_11eb_a3aa_1d9d,
            0x96bf_1b5f_4dd8_1db3,
            0x8100_d272_c925_9f5b,
            0xafa2_0b96_7464_0eab,
            0x09bb_cea7_d8d9_497d
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0303_cb98_b166_2daa,
            0xd931_10aa_0a62_1d5a,
            0xbfa9_820c_5be4_a468,
            0x0ba3_643e_cb05_a348,
            0xdc35_34bb_1f1c_25a6,
            0x06c3_05bb_19c0_e1c1
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x46f9_cb98_b162_d858,
            0x0be9_109c_f7aa_1d57,
            0xc791_bc55_fece_41d2,
            0xf84c_5770_4e38_5ec2,
            0xcb49_c1d9_c010_e60f,
            0x0acd_b8e1_58bf_e348
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x8aef_cb98_b15f_8306,
            0x3ea1_108f_e4f2_1d54,
            0xcf79_f69f_a1b7_df3b,
            0xe4f5_4aa1_d16b_1a3c,
            0xba5e_4ef8_6105_a679,
            0x0ed8_6c07_97be_e5cf
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xcee5_cb98_b15c_2db4,
            0x7159_1082_d23a_1d51,
            0xd762_30e9_44a1_7ca4,
            0xd19e_3dd3_549d_d5b6,
            0xa972_dc17_01fa_66e3,
            0x12e3_1f2d_d6bd_e7d6
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xad2a_cb98_b173_2d9d,
            0x2cfd_10dd_0696_1d64,
            0x0739_6b86_c6ef_24e8,
            0xbd76_e2fd_b1bf_c820,
            0x6afe_a7f6_de94_d0d5,
            0x1099_4b0c_5744_c040
        }))), new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x47f9_cb98_b1b8_2d58,
            0x5fe9_11eb_a3aa_1d9d,
            0x96bf_1b5f_4dd2_1db3,
            0x8100_d27c_c925_9f5b,
            0xafa2_0b96_7464_0eab,
            0x09bb_cea7_d8d9_497d
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0303_cb98_b166_2daa,
            0xd931_10aa_0a62_1d5a,
            0xbfa9_820c_5be4_a468,
            0x0ba3_643e_cb05_a348,
            0xdc35_34bb_1f1c_25a6,
            0x06c3_05bb_19c0_e1c1
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x46f9_cb98_b162_d858,
            0x0be9_109c_f7aa_1d57,
            0xc791_bc55_fece_41d2,
            0xf84c_5770_4e38_5ec2,
            0xcb49_c1d9_c010_e60f,
            0x0acd_b8e1_58bf_e3c8
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x8aef_cb98_b15f_8306,
            0x3ea1_108f_e4f2_1d54,
            0xcf79_f69f_a117_df3b,
            0xe4f5_4aa1_d16b_1a3c,
            0xba5e_4ef8_6105_a679,
            0x0ed8_6c07_97be_e5cf
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xcee5_cb98_b15c_2db4,
            0x7159_1082_d23a_1d51,
            0xd762_30e9_44a1_7ca4,
            0xd19e_3dd3_549d_d5b6,
            0xa972_dc17_01fa_66e3,
            0x12e3_1f2d_d6bd_e7d6
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xad2a_cb98_b173_2d9d,
            0x2cfd_10dd_0696_1d64,
            0x0739_6b86_c6ef_24e8,
            0xbd76_e2fd_b1bf_c820,
            0x6afe_a7f6_de94_d0d5,
            0x1099_4b0c_5744_c040
        }))));

        var c = new Fp12(new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x47f9_cb98_71b8_2d58,
            0x5fe9_11eb_a3aa_1d9d,
            0x96bf_1b5f_4dd8_1db3,
            0x8100_d27c_c925_9f5b,
            0xafa2_0b96_7464_0eab,
            0x09bb_cea7_d8d9_497d
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0303_cb98_b166_2daa,
            0xd931_10aa_0a62_1d5a,
            0xbfa9_820c_5be4_a468,
            0x0ba3_643e_cb05_a348,
            0xdc35_34bb_1f1c_25a6,
            0x06c3_05bb_19c0_e1c1
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x46f9_cb98_b162_d858,
            0x0be9_109c_f7aa_1d57,
            0x7791_bc55_fece_41d2,
            0xf84c_5770_4e38_5ec2,
            0xcb49_c1d9_c010_e60f,
            0x0acd_b8e1_58bf_e3c8
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x8aef_cb98_b15f_8306,
            0x3ea1_108f_e4f2_1d54,
            0xcf79_f69f_a1b7_df3b,
            0xe4f5_4aa1_d16b_133c,
            0xba5e_4ef8_6105_a679,
            0x0ed8_6c07_97be_e5cf
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xcee5_cb98_b15c_2db4,
            0x7159_1082_d23a_1d51,
            0xd762_40e9_44a1_7ca4,
            0xd19e_3dd3_549d_d5b6,
            0xa972_dc17_01fa_66e3,
            0x12e3_1f2d_d6bd_e7d6
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xad2a_cb98_b173_2d9d,
            0x2cfd_10dd_0696_1d64,
            0x0739_6b86_c6ef_24e8,
            0xbd76_e2fd_b1bf_c820,
            0x6afe_a7f6_de94_d0d5,
            0x1099_4b0c_1744_c040
        }))), new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x47f9_cb98_b1b8_2d58,
            0x5fe9_11eb_a3aa_1d9d,
            0x96bf_1b5f_4dd8_1db3,
            0x8100_d27c_c925_9f5b,
            0xafa2_0b96_7464_0eab,
            0x09bb_cea7_d8d9_497d
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0303_cb98_b166_2daa,
            0xd931_10aa_0a62_1d5a,
            0xbfa9_820c_5be4_a468,
            0x0ba3_643e_cb05_a348,
            0xdc35_34bb_1f1c_25a6,
            0x06c3_05bb_19c0_e1c1
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x46f9_cb98_b162_d858,
            0x0be9_109c_f7aa_1d57,
            0xc791_bc55_fece_41d2,
            0xf84c_5770_4e38_5ec2,
            0xcb49_c1d3_c010_e60f,
            0x0acd_b8e1_58bf_e3c8
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x8aef_cb98_b15f_8306,
            0x3ea1_108f_e4f2_1d54,
            0xcf79_f69f_a1b7_df3b,
            0xe4f5_4aa1_d16b_1a3c,
            0xba5e_4ef8_6105_a679,
            0x0ed8_6c07_97be_e5cf
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xcee5_cb98_b15c_2db4,
            0x7159_1082_d23a_1d51,
            0xd762_30e9_44a1_7ca4,
            0xd19e_3dd3_549d_d5b6,
            0xa972_dc17_01fa_66e3,
            0x12e3_1f2d_d6bd_e7d6
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xad2a_cb98_b173_2d9d,
            0x2cfd_10dd_0696_1d64,
            0x0739_6b86_c6ef_24e8,
            0xbd76_e2fd_b1bf_c820,
            0x6afe_a7f6_de94_d0d5,
            0x1099_4b0c_5744_1040
        }))));

        // because a and b and c are similar to each other and
        // I was lazy, this is just some arbitrary way to make
        // them a little more different
        a = a.Square().Invert().Square() + c;
        b = b.Square().Invert().Square() + a;
        c = c.Square().Invert().Square() + b;

        Assert.AreEqual(a * a, a.Square());
        Assert.AreEqual(b * b, b.Square());
        Assert.AreEqual(c * c, c.Square());

        Assert.AreEqual((a + b) * c.Square(), (c * c * a) + (c * c * b));

        Assert.AreEqual(a.Invert() * b.Invert(), (a * b).Invert());
        Assert.AreEqual(Fp12.One, a.Invert() * a);

        Assert.AreNotEqual(a, a.FrobeniusMap());
        Assert.AreEqual(
            a,
            a.FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
                .FrobeniusMap()
        );
    }
}
