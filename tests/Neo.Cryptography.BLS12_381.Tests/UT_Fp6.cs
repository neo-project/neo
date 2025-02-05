// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Fp6.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BLS12_381.Tests
{
    [TestClass]
    public class UT_Fp6
    {
        [TestMethod]
        public void TestArithmetic()
        {
            var a = new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x47f9_cb98_b1b8_2d58,
                0x5fe9_11eb_a3aa_1d9d,
                0x96bf_1b5f_4dd8_1db3,
                0x8100_d27c_c925_9f5b,
                0xafa2_0b96_7464_0eab,
                0x09bb_cea7_d8d9_497d
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0x0303_cb98_b166_2daa,
                0xd931_10aa_0a62_1d5a,
                0xbfa9_820c_5be4_a468,
                0x0ba3_643e_cb05_a348,
                0xdc35_34bb_1f1c_25a6,
                0x06c3_05bb_19c0_e1c1
            })), c1: new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x46f9_cb98_b162_d858,
                0x0be9_109c_f7aa_1d57,
                0xc791_bc55_fece_41d2,
                0xf84c_5770_4e38_5ec2,
                0xcb49_c1d9_c010_e60f,
                0x0acd_b8e1_58bf_e3c8
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0x8aef_cb98_b15f_8306,
                0x3ea1_108f_e4f2_1d54,
                0xcf79_f69f_a1b7_df3b,
                0xe4f5_4aa1_d16b_1a3c,
                0xba5e_4ef8_6105_a679,
                0x0ed8_6c07_97be_e5cf
            })), c2: new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xcee5_cb98_b15c_2db4,
                0x7159_1082_d23a_1d51,
                0xd762_30e9_44a1_7ca4,
                0xd19e_3dd3_549d_d5b6,
                0xa972_dc17_01fa_66e3,
                0x12e3_1f2d_d6bd_e7d6
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0xad2a_cb98_b173_2d9d,
                0x2cfd_10dd_0696_1d64,
                0x0739_6b86_c6ef_24e8,
                0xbd76_e2fd_b1bf_c820,
                0x6afe_a7f6_de94_d0d5,
                0x1099_4b0c_5744_c040
            })));

            var b = new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xf120_cb98_b16f_d84b,
                0x5fb5_10cf_f3de_1d61,
                0x0f21_a5d0_69d8_c251,
                0xaa1f_d62f_34f2_839a,
                0x5a13_3515_7f89_913f,
                0x14a3_fe32_9643_c247
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0x3516_cb98_b16c_82f9,
                0x926d_10c2_e126_1d5f,
                0x1709_e01a_0cc2_5fba,
                0x96c8_c960_b825_3f14,
                0x4927_c234_207e_51a9,
                0x18ae_b158_d542_c44e
            })), c1: new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xbf0d_cb98_b169_82fc,
                0xa679_10b7_1d1a_1d5c,
                0xb7c1_47c2_b8fb_06ff,
                0x1efa_710d_47d2_e7ce,
                0xed20_a79c_7e27_653c,
                0x02b8_5294_dac1_dfba
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0x9d52_cb98_b180_82e5,
                0x621d_1111_5176_1d6f,
                0xe798_8260_3b48_af43,
                0x0ad3_1637_a4f4_da37,
                0xaeac_737c_5ac1_cf2e,
                0x006e_7e73_5b48_b824
            })), c2: new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xe148_cb98_b17d_2d93,
                0x94d5_1104_3ebe_1d6c,
                0xef80_bca9_de32_4cac,
                0xf77c_0969_2827_95b1,
                0x9dc1_009a_fbb6_8f97,
                0x0479_3199_9a47_ba2b
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0x253e_cb98_b179_d841,
                0xc78d_10f7_2c06_1d6a,
                0xf768_f6f3_811b_ea15,
                0xe424_fc9a_ab5a_512b,
                0x8cd5_8db9_9cab_5001,
                0x0883_e4bf_d946_bc32
            })));

            var c = new Fp6(new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x6934_cb98_b176_82ef,
                0xfa45_10ea_194e_1d67,
                0xff51_313d_2405_877e,
                0xd0cd_efcc_2e8d_0ca5,
                0x7bea_1ad8_3da0_106b,
                0x0c8e_97e6_1845_be39
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0x4779_cb98_b18d_82d8,
                0xb5e9_1144_4daa_1d7a,
                0x2f28_6bda_a653_2fc2,
                0xbca6_94f6_8bae_ff0f,
                0x3d75_e6b8_1a3a_7a5d,
                0x0a44_c3c4_98cc_96a3
            })), c1: new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x8b6f_cb98_b18a_2d86,
                0xe8a1_1137_3af2_1d77,
                0x3710_a624_493c_cd2b,
                0xa94f_8828_0ee1_ba89,
                0x2c8a_73d6_bb2f_3ac7,
                0x0e4f_76ea_d7cb_98aa
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0xcf65_cb98_b186_d834,
                0x1b59_112a_283a_1d74,
                0x3ef8_e06d_ec26_6a95,
                0x95f8_7b59_9214_7603,
                0x1b9f_00f5_5c23_fb31,
                0x125a_2a11_16ca_9ab1
            })), c2: new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x135b_cb98_b183_82e2,
                0x4e11_111d_1582_1d72,
                0x46e1_1ab7_8f10_07fe,
                0x82a1_6e8b_1547_317d,
                0x0ab3_8e13_fd18_bb9b,
                0x1664_dd37_55c9_9cb8
            }), c1: Fp.FromRawUnchecked(new ulong[]
            {
                0xce65_cb98_b131_8334,
                0xc759_0fdb_7c3a_1d2e,
                0x6fcb_8164_9d1c_8eb3,
                0x0d44_004d_1727_356a,
                0x3746_b738_a7d0_d296,
                0x136c_144a_96b1_34fc
            })));

            Assert.AreEqual(a * a, a.Square());
            Assert.AreEqual(b * b, b.Square());
            Assert.AreEqual(c * c, c.Square());

            Assert.AreEqual((a + b) * c.Square(), (c * c * a) + (c * c * b));

            Assert.AreEqual(a.Invert() * b.Invert(), (a * b).Invert());
            Assert.AreEqual(Fp6.One, a.Invert() * a);
        }
    }
}
