// Copyright (C) 2015-2024 The Neo Project.
//
// UT_G2.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using static Neo.Cryptography.BLS12_381.Constants;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;

namespace Neo.Cryptography.BLS12_381.Tests;

[TestClass]
public class UT_G2
{
    [TestMethod]
    public void TestIsOnCurve()
    {
        Assert.IsTrue(G2Affine.Identity.IsOnCurve);
        Assert.IsTrue(G2Affine.Generator.IsOnCurve);
        Assert.IsTrue(G2Projective.Identity.IsOnCurve);
        Assert.IsTrue(G2Projective.Generator.IsOnCurve);

        var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xba7a_fa1f_9a6f_e250,
            0xfa0f_5b59_5eaf_e731,
            0x3bdc_4776_94c3_06e7,
            0x2149_be4b_3949_fa24,
            0x64aa_6e06_49b2_078c,
            0x12b1_08ac_3364_3c3e
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x1253_25df_3d35_b5a8,
            0xdc46_9ef5_555d_7fe3,
            0x02d7_16d2_4431_06a9,
            0x05a1_db59_a6ff_37d0,
            0x7cf7_784e_5300_bb8f,
            0x16a8_8922_c7a5_e844
        }));

        var gen = G2Affine.Generator;
        var test = new G2Projective(gen.X * z, gen.Y * z, z);

        Assert.IsTrue(test.IsOnCurve);

        test = new(in z, in test.Y, in test.Z);
        Assert.IsFalse(test.IsOnCurve);
    }

    [TestMethod]
    public void TestAffinePointEquality()
    {
        var a = G2Affine.Generator;
        var b = G2Affine.Identity;

        Assert.AreEqual(a, a);
        Assert.AreEqual(b, b);
        Assert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void TestProjectivePointEquality()
    {
        var a = G2Projective.Generator;
        var b = G2Projective.Identity;

        Assert.AreEqual(a, a);
        Assert.AreEqual(b, b);
        Assert.AreNotEqual(a, b);

        var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xba7a_fa1f_9a6f_e250,
            0xfa0f_5b59_5eaf_e731,
            0x3bdc_4776_94c3_06e7,
            0x2149_be4b_3949_fa24,
            0x64aa_6e06_49b2_078c,
            0x12b1_08ac_3364_3c3e
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x1253_25df_3d35_b5a8,
            0xdc46_9ef5_555d_7fe3,
            0x02d7_16d2_4431_06a9,
            0x05a1_db59_a6ff_37d0,
            0x7cf7_784e_5300_bb8f,
            0x16a8_8922_c7a5_e844
        }));

        var c = new G2Projective(a.X * z, a.Y * z, in z);
        Assert.IsTrue(c.IsOnCurve);

        Assert.AreEqual(a, c);
        Assert.AreNotEqual(b, c);

        c = new(in c.X, -c.Y, in c.Z);
        Assert.IsTrue(c.IsOnCurve);

        Assert.AreNotEqual(a, c);
        Assert.AreNotEqual(b, c);

        c = new(in z, -c.Y, in c.Z);
        Assert.IsFalse(c.IsOnCurve);
        Assert.AreNotEqual(a, b);
        Assert.AreNotEqual(a, c);
        Assert.AreNotEqual(b, c);
    }

    [TestMethod]
    public void TestConditionallySelectAffine()
    {
        var a = G2Affine.Generator;
        var b = G2Affine.Identity;

        Assert.AreEqual(a, ConditionalSelect(in a, in b, false));
        Assert.AreEqual(b, ConditionalSelect(in a, in b, true));
    }

    [TestMethod]
    public void TestConditionallySelectProjective()
    {
        var a = G2Projective.Generator;
        var b = G2Projective.Identity;

        Assert.AreEqual(a, ConditionalSelect(in a, in b, false));
        Assert.AreEqual(b, ConditionalSelect(in a, in b, true));
    }

    [TestMethod]
    public void TestProjectiveToAffine()
    {
        var a = G2Projective.Generator;
        var b = G2Projective.Identity;

        Assert.IsTrue(new G2Affine(a).IsOnCurve);
        Assert.IsFalse(new G2Affine(a).IsIdentity);
        Assert.IsTrue(new G2Affine(b).IsOnCurve);
        Assert.IsTrue(new G2Affine(b).IsIdentity);

        var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xba7a_fa1f_9a6f_e250,
            0xfa0f_5b59_5eaf_e731,
            0x3bdc_4776_94c3_06e7,
            0x2149_be4b_3949_fa24,
            0x64aa_6e06_49b2_078c,
            0x12b1_08ac_3364_3c3e
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x1253_25df_3d35_b5a8,
            0xdc46_9ef5_555d_7fe3,
            0x02d7_16d2_4431_06a9,
            0x05a1_db59_a6ff_37d0,
            0x7cf7_784e_5300_bb8f,
            0x16a8_8922_c7a5_e844
        }));

        var c = new G2Projective(a.X * z, a.Y * z, in z);

        Assert.AreEqual(G2Affine.Generator, new G2Affine(c));
    }

    [TestMethod]
    public void TestAffineToProjective()
    {
        var a = G2Affine.Generator;
        var b = G2Affine.Identity;

        Assert.IsTrue(new G2Projective(a).IsOnCurve);
        Assert.IsFalse(new G2Projective(a).IsIdentity);
        Assert.IsTrue(new G2Projective(b).IsOnCurve);
        Assert.IsTrue(new G2Projective(b).IsIdentity);
    }

    [TestMethod]
    public void TestDoubling()
    {
        {
            var tmp = G2Projective.Identity.Double();
            Assert.IsTrue(tmp.IsIdentity);
            Assert.IsTrue(tmp.IsOnCurve);
        }
        {
            var tmp = G2Projective.Generator.Double();
            Assert.IsFalse(tmp.IsIdentity);
            Assert.IsTrue(tmp.IsOnCurve);

            Assert.AreEqual(new G2Affine(tmp), new G2Affine(new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xe9d9_e2da_9620_f98b,
                0x54f1_1993_46b9_7f36,
                0x3db3_b820_376b_ed27,
                0xcfdb_31c9_b0b6_4f4c,
                0x41d7_c127_8635_4493,
                0x0571_0794_c255_c064
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0xd6c1_d3ca_6ea0_d06e,
                0xda0c_bd90_5595_489f,
                0x4f53_52d4_3479_221d,
                0x8ade_5d73_6f8c_97e0,
                0x48cc_8433_925e_f70e,
                0x08d7_ea71_ea91_ef81
            })), new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x15ba_26eb_4b0d_186f,
                0x0d08_6d64_b7e9_e01e,
                0xc8b8_48dd_652f_4c78,
                0xeecf_46a6_123b_ae4f,
                0x255e_8dd8_b6dc_812a,
                0x1641_42af_21dc_f93f
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0xf9b4_a1a8_9598_4db4,
                0xd417_b114_cccf_f748,
                0x6856_301f_c89f_086e,
                0x41c7_7787_8931_e3da,
                0x3556_b155_066a_2105,
                0x00ac_f7d3_25cb_89cf
            }))));
        }
    }

    [TestMethod]
    public void TestProjectiveAddition()
    {
        {
            var a = G2Projective.Identity;
            var b = G2Projective.Identity;
            var c = a + b;
            Assert.IsTrue(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
        }
        {
            var a = G2Projective.Identity;
            var b = G2Projective.Generator;
            {
                var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x1253_25df_3d35_b5a8,
                    0xdc46_9ef5_555d_7fe3,
                    0x02d7_16d2_4431_06a9,
                    0x05a1_db59_a6ff_37d0,
                    0x7cf7_784e_5300_bb8f,
                    0x16a8_8922_c7a5_e844
                }));

                b = new G2Projective(b.X * z, b.Y * z, in z);
            }
            var c = a + b;
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
            Assert.AreEqual(G2Projective.Generator, c);
        }
        {
            var a = G2Projective.Identity;
            var b = G2Projective.Generator;
            {
                var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x1253_25df_3d35_b5a8,
                    0xdc46_9ef5_555d_7fe3,
                    0x02d7_16d2_4431_06a9,
                    0x05a1_db59_a6ff_37d0,
                    0x7cf7_784e_5300_bb8f,
                    0x16a8_8922_c7a5_e844
                }));

                b = new G2Projective(b.X * z, b.Y * z, in z);
            }
            var c = b + a;
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
            Assert.AreEqual(G2Projective.Generator, c);
        }
        {
            var a = G2Projective.Generator.Double().Double(); // 4P
            var b = G2Projective.Generator.Double(); // 2P
            var c = a + b;

            var d = G2Projective.Generator;
            for (var i = 0; i < 5; i++)
            {
                d += G2Projective.Generator;
            }
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
            Assert.IsFalse(d.IsIdentity);
            Assert.IsTrue(d.IsOnCurve);
            Assert.AreEqual(c, d);
        }

        // Degenerate case
        {
            var beta = new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xcd03_c9e4_8671_f071,
                0x5dab_2246_1fcd_a5d2,
                0x5870_42af_d385_1b95,
                0x8eb6_0ebe_01ba_cb9e,
                0x03f9_7d6e_83d0_50d2,
                0x18f0_2065_5463_8741
            }), Fp.Zero);
            beta = beta.Square();
            var a = G2Projective.Generator.Double().Double();
            var b = new G2Projective(a.X * beta, -a.Y, in a.Z);
            Assert.IsTrue(a.IsOnCurve);
            Assert.IsTrue(b.IsOnCurve);

            var c = a + b;
            Assert.AreEqual(
            new G2Affine(c),
            new G2Affine(new G2Projective(new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x705a_bc79_9ca7_73d3,
                0xfe13_2292_c1d4_bf08,
                0xf37e_ce3e_07b2_b466,
                0x887e_1c43_f447_e301,
                0x1e09_70d0_33bc_77e8,
                0x1985_c81e_20a6_93f2
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0x1d79_b25d_b36a_b924,
                0x2394_8e4d_5296_39d3,
                0x471b_a7fb_0d00_6297,
                0x2c36_d4b4_465d_c4c0,
                0x82bb_c3cf_ec67_f538,
                0x051d_2728_b67b_f952
            })), new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x41b1_bbf6_576c_0abf,
                0xb6cc_9371_3f7a_0f9a,
                0x6b65_b43e_48f3_f01f,
                0xfb7a_4cfc_af81_be4f,
                0x3e32_dadc_6ec2_2cb6,
                0x0bb0_fc49_d798_07e3
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0x7d13_9778_8f5f_2ddf,
                0xab29_0714_4ff0_d8e8,
                0x5b75_73e0_cdb9_1f92,
                0x4cb8_932d_d31d_af28,
                0x62bb_fac6_db05_2a54,
                0x11f9_5c16_d14c_3bbe
            })), Fp2.One)));
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
        }
    }

    [TestMethod]
    public void TestMixedAddition()
    {
        {
            var a = G2Affine.Identity;
            var b = G2Projective.Identity;
            var c = a + b;
            Assert.IsTrue(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
        }
        {
            var a = G2Affine.Identity;
            var b = G2Projective.Generator;
            {
                var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x1253_25df_3d35_b5a8,
                    0xdc46_9ef5_555d_7fe3,
                    0x02d7_16d2_4431_06a9,
                    0x05a1_db59_a6ff_37d0,
                    0x7cf7_784e_5300_bb8f,
                    0x16a8_8922_c7a5_e844
                }));

                b = new G2Projective(b.X * z, b.Y * z, in z);
            }
            var c = a + b;
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
            Assert.AreEqual(G2Projective.Generator, c);
        }
        {
            var a = G2Affine.Identity;
            var b = G2Projective.Generator;
            {
                var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x1253_25df_3d35_b5a8,
                    0xdc46_9ef5_555d_7fe3,
                    0x02d7_16d2_4431_06a9,
                    0x05a1_db59_a6ff_37d0,
                    0x7cf7_784e_5300_bb8f,
                    0x16a8_8922_c7a5_e844
                }));

                b = new G2Projective(b.X * z, b.Y * z, in z);
            }
            var c = b + a;
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
            Assert.AreEqual(G2Projective.Generator, c);
        }
        {
            var a = G2Projective.Generator.Double().Double(); // 4P
            var b = G2Projective.Generator.Double(); // 2P
            var c = a + b;

            var d = G2Projective.Generator;
            for (var i = 0; i < 5; i++)
            {
                d += G2Affine.Generator;
            }
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
            Assert.IsFalse(d.IsIdentity);
            Assert.IsTrue(d.IsOnCurve);
            Assert.AreEqual(c, d);
        }

        // Degenerate case
        {
            var beta = new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xcd03_c9e4_8671_f071,
                0x5dab_2246_1fcd_a5d2,
                0x5870_42af_d385_1b95,
                0x8eb6_0ebe_01ba_cb9e,
                0x03f9_7d6e_83d0_50d2,
                0x18f0_2065_5463_8741
            }), Fp.Zero);
            beta = beta.Square();
            var _a = G2Projective.Generator.Double().Double();
            var b = new G2Projective(_a.X * beta, -_a.Y, in _a.Z);
            var a = new G2Affine(_a);
            Assert.IsTrue((a.IsOnCurve));
            Assert.IsTrue((b.IsOnCurve));

            var c = a + b;
            Assert.AreEqual(new G2Affine(new G2Projective(new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x705a_bc79_9ca7_73d3,
                0xfe13_2292_c1d4_bf08,
                0xf37e_ce3e_07b2_b466,
                0x887e_1c43_f447_e301,
                0x1e09_70d0_33bc_77e8,
                0x1985_c81e_20a6_93f2
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0x1d79_b25d_b36a_b924,
                0x2394_8e4d_5296_39d3,
                0x471b_a7fb_0d00_6297,
                0x2c36_d4b4_465d_c4c0,
                0x82bb_c3cf_ec67_f538,
                0x051d_2728_b67b_f952
            })), new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0x41b1_bbf6_576c_0abf,
                0xb6cc_9371_3f7a_0f9a,
                0x6b65_b43e_48f3_f01f,
                0xfb7a_4cfc_af81_be4f,
                0x3e32_dadc_6ec2_2cb6,
                0x0bb0_fc49_d798_07e3
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0x7d13_9778_8f5f_2ddf,
                0xab29_0714_4ff0_d8e8,
                0x5b75_73e0_cdb9_1f92,
                0x4cb8_932d_d31d_af28,
                0x62bb_fac6_db05_2a54,
                0x11f9_5c16_d14c_3bbe
            })), Fp2.One)), new G2Affine(c));
            Assert.IsFalse(c.IsIdentity);
            Assert.IsTrue(c.IsOnCurve);
        }
    }

    [TestMethod]
    public void TestProjectiveNegationAndSubtraction()
    {
        var a = G2Projective.Generator.Double();
        Assert.AreEqual(G2Projective.Identity, a + (-a));
        Assert.AreEqual(a - a, a + (-a));
    }

    [TestMethod]
    public void TestAffineNegationAndSubtraction()
    {
        var a = G2Affine.Generator;
        Assert.AreEqual(G2Projective.Identity, new G2Projective(a) + (-a));
        Assert.AreEqual(new G2Projective(a) - a, new G2Projective(a) + (-a));
    }

    [TestMethod]
    public void TestProjectiveScalarMultiplication()
    {
        var g = G2Projective.Generator;
        var a = Scalar.FromRaw(new ulong[]
        {
            0x2b56_8297_a56d_a71c,
            0xd8c3_9ecb_0ef3_75d1,
            0x435c_38da_67bf_bf96,
            0x8088_a050_26b6_59b2
        });
        var b = Scalar.FromRaw(new ulong[]
        {
            0x785f_dd9b_26ef_8b85,
            0xc997_f258_3769_5c18,
            0x4c8d_bc39_e7b7_56c1,
            0x70d9_b6cc_6d87_df20
        });
        var c = a * b;

        Assert.AreEqual(g * c, g * a * b);
    }

    [TestMethod]
    public void TestAffineScalarMultiplication()
    {
        var g = G2Affine.Generator;
        var a = Scalar.FromRaw(new ulong[]
        {
            0x2b56_8297_a56d_a71c,
            0xd8c3_9ecb_0ef3_75d1,
            0x435c_38da_67bf_bf96,
            0x8088_a050_26b6_59b2
        });
        var b = Scalar.FromRaw(new ulong[]
        {
            0x785f_dd9b_26ef_8b85,
            0xc997_f258_3769_5c18,
            0x4c8d_bc39_e7b7_56c1,
            0x70d9_b6cc_6d87_df20
        });
        var c = a * b;

        Assert.AreEqual(g * c, new G2Affine(g * a) * b);
    }

    [TestMethod]
    public void TestIsTorsionFree()
    {
        var a = new G2Affine(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x89f5_50c8_13db_6431,
            0xa50b_e8c4_56cd_8a1a,
            0xa45b_3741_14ca_e851,
            0xbb61_90f5_bf7f_ff63,
            0x970c_a02c_3ba8_0bc7,
            0x02b8_5d24_e840_fbac
        }),
        Fp.FromRawUnchecked(new ulong[]
        {
            0x6888_bc53_d707_16dc,
            0x3dea_6b41_1768_2d70,
            0xd8f5_f930_500c_a354,
            0x6b5e_cb65_56f5_c155,
            0xc96b_ef04_3477_8ab0,
            0x0508_1505_5150_06ad
        })), new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x3cf1_ea0d_434b_0f40,
            0x1a0d_c610_e603_e333,
            0x7f89_9561_60c7_2fa0,
            0x25ee_03de_cf64_31c5,
            0xeee8_e206_ec0f_e137,
            0x0975_92b2_26df_ef28
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x71e8_bb5f_2924_7367,
            0xa5fe_049e_2118_31ce,
            0x0ce6_b354_502a_3896,
            0x93b0_1200_0997_314e,
            0x6759_f3b6_aa5b_42ac,
            0x1569_44c4_dfe9_2bbb
        })));
        Assert.IsFalse(a.IsTorsionFree);

        Assert.IsTrue(G2Affine.Identity.IsTorsionFree);
        Assert.IsTrue(G2Affine.Generator.IsTorsionFree);
    }

    [TestMethod]
    public void TestMulByX()
    {
        // multiplying by `x` a point in G2 is the same as multiplying by
        // the equivalent scalar.
        var generator = G2Projective.Generator;
        var x = BLS_X_IS_NEGATIVE ? -new Scalar(BLS_X) : new Scalar(BLS_X);
        Assert.AreEqual(generator * x, generator.MulByX());

        var point = G2Projective.Generator * new Scalar(42);
        Assert.AreEqual(point * x, point.MulByX());
    }

    [TestMethod]
    public void TestPsi()
    {
        var generator = G2Projective.Generator;

        var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x0ef2ddffab187c0a,
            0x2424522b7d5ecbfc,
            0xc6f341a3398054f4,
            0x5523ddf409502df0,
            0xd55c0b5a88e0dd97,
            0x066428d704923e52
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x538bbe0c95b4878d,
            0xad04a50379522881,
            0x6d5c05bf5c12fb64,
            0x4ce4a069a2d34787,
            0x59ea6c8d0dffaeaf,
            0x0d42a083a75bd6f3
        }));

        // `point` is a random point in the curve
        var point = new G2Projective(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xee4c8cb7c047eaf2,
            0x44ca22eee036b604,
            0x33b3affb2aefe101,
            0x15d3e45bbafaeb02,
            0x7bfc2154cd7419a4,
            0x0a2d0c2b756e5edc
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xfc224361029a8777,
            0x4cbf2baab8740924,
            0xc5008c6ec6592c89,
            0xecc2c57b472a9c2d,
            0x8613eafd9d81ffb1,
            0x10fe54daa2d3d495
        })) * z, new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x7de7edc43953b75c,
            0x58be1d2de35e87dc,
            0x5731d30b0e337b40,
            0xbe93b60cfeaae4c9,
            0x8b22c203764bedca,
            0x01616c8d1033b771
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xea126fe476b5733b,
            0x85cee68b5dae1652,
            0x98247779f7272b04,
            0xa649c8b468c6e808,
            0xb5b9a62dff0c4e45,
            0x1555b67fc7bbe73d
        })), z.Square() * z);
        Assert.IsTrue(point.IsOnCurve);

        // psi2(P) = psi(psi(P))
        Assert.AreEqual(generator.Psi2(), generator.Psi().Psi());
        Assert.AreEqual(point.Psi2(), point.Psi().Psi());
        // psi(P) is a morphism
        Assert.AreEqual(generator.Double().Psi(), generator.Psi().Double());
        Assert.AreEqual(point.Psi() + generator.Psi(), (point + generator).Psi());
        // psi(P) behaves in the same way on the same projective point
        var normalized_points = new G2Affine[1];
        G2Projective.BatchNormalize(new[] { point }, normalized_points);
        var normalized_point = new G2Projective(normalized_points[0]);
        Assert.AreEqual(point.Psi(), normalized_point.Psi());
        Assert.AreEqual(point.Psi2(), normalized_point.Psi2());
    }

    [TestMethod]
    public void TestClearCofactor()
    {
        var z = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x0ef2ddffab187c0a,
            0x2424522b7d5ecbfc,
            0xc6f341a3398054f4,
            0x5523ddf409502df0,
            0xd55c0b5a88e0dd97,
            0x066428d704923e52
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x538bbe0c95b4878d,
            0xad04a50379522881,
            0x6d5c05bf5c12fb64,
            0x4ce4a069a2d34787,
            0x59ea6c8d0dffaeaf,
            0x0d42a083a75bd6f3
        }));

        // `point` is a random point in the curve
        var point = new G2Projective(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xee4c8cb7c047eaf2,
            0x44ca22eee036b604,
            0x33b3affb2aefe101,
            0x15d3e45bbafaeb02,
            0x7bfc2154cd7419a4,
            0x0a2d0c2b756e5edc
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xfc224361029a8777,
            0x4cbf2baab8740924,
            0xc5008c6ec6592c89,
            0xecc2c57b472a9c2d,
            0x8613eafd9d81ffb1,
            0x10fe54daa2d3d495
        })) * z, new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x7de7edc43953b75c,
            0x58be1d2de35e87dc,
            0x5731d30b0e337b40,
            0xbe93b60cfeaae4c9,
            0x8b22c203764bedca,
            0x01616c8d1033b771
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xea126fe476b5733b,
            0x85cee68b5dae1652,
            0x98247779f7272b04,
            0xa649c8b468c6e808,
            0xb5b9a62dff0c4e45,
            0x1555b67fc7bbe73d
        })), z.Square() * z);

        Assert.IsTrue(point.IsOnCurve);
        Assert.IsFalse(new G2Affine(point).IsTorsionFree);
        var cleared_point = point.ClearCofactor();

        Assert.IsTrue(cleared_point.IsOnCurve);
        Assert.IsTrue(new G2Affine(cleared_point).IsTorsionFree);

        // the generator (and the identity) are always on the curve,
        // even after clearing the cofactor
        var generator = G2Projective.Generator;
        Assert.IsTrue(generator.ClearCofactor().IsOnCurve);
        var id = G2Projective.Identity;
        Assert.IsTrue(id.ClearCofactor().IsOnCurve);

        // test the effect on q-torsion points multiplying by h_eff modulo |Scalar|
        // h_eff % q = 0x2b116900400069009a40200040001ffff
        byte[] h_eff_modq =
        {
            0xff, 0xff, 0x01, 0x00, 0x04, 0x00, 0x02, 0xa4, 0x09, 0x90, 0x06, 0x00, 0x04, 0x90, 0x16,
            0xb1, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
        Assert.AreEqual(generator * h_eff_modq, generator.ClearCofactor());
        Assert.AreEqual(cleared_point * h_eff_modq, cleared_point.ClearCofactor());
    }

    [TestMethod]
    public void TestBatchNormalize()
    {
        var a = G2Projective.Generator.Double();
        var b = a.Double();
        var c = b.Double();

        foreach (var a_identity in new[] { false, true })
        {
            foreach (var b_identity in new[] { false, true })
            {
                foreach (var c_identity in new[] { false, true })
                {
                    var v = new[] { a, b, c };
                    if (a_identity)
                    {
                        v[0] = G2Projective.Identity;
                    }
                    if (b_identity)
                    {
                        v[1] = G2Projective.Identity;
                    }
                    if (c_identity)
                    {
                        v[2] = G2Projective.Identity;
                    }

                    var t = new G2Affine[3];
                    var expected = new[] { new G2Affine(v[0]), new G2Affine(v[1]), new G2Affine(v[2]) };

                    G2Projective.BatchNormalize(v, t);

                    CollectionAssert.AreEqual(t, expected);
                }
            }
        }
    }
}
