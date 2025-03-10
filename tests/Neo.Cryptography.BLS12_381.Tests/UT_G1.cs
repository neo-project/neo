// Copyright (C) 2015-2025 The Neo Project.
//
// UT_G1.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using static Neo.Cryptography.BLS12_381.Constants;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.G1Constants;

namespace Neo.Cryptography.BLS12_381.Tests
{
    [TestClass]
    public class UT_G1
    {
        [TestMethod]
        public void TestBeta()
        {
            Assert.AreEqual(Fp.FromBytes(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5f, 0x19, 0x67, 0x2f, 0xdf, 0x76,
                0xce, 0x51, 0xba, 0x69, 0xc6, 0x07, 0x6a, 0x0f, 0x77, 0xea, 0xdd, 0xb3, 0xa9, 0x3b,
                0xe6, 0xf8, 0x96, 0x88, 0xde, 0x17, 0xd8, 0x13, 0x62, 0x0a, 0x00, 0x02, 0x2e, 0x01,
                0xff, 0xff, 0xff, 0xfe, 0xff, 0xfe
            }), BETA);
            Assert.AreNotEqual(Fp.One, BETA);
            Assert.AreNotEqual(Fp.One, BETA * BETA);
            Assert.AreEqual(Fp.One, BETA * BETA * BETA);
        }

        [TestMethod]
        public void TestIsOnCurve()
        {
            Assert.IsTrue(G1Affine.Identity.IsOnCurve);
            Assert.IsTrue(G1Affine.Generator.IsOnCurve);
            Assert.IsTrue(G1Projective.Identity.IsOnCurve);
            Assert.IsTrue(G1Projective.Generator.IsOnCurve);

            Fp z = Fp.FromRawUnchecked(new ulong[]
            {
                0xba7a_fa1f_9a6f_e250,
                0xfa0f_5b59_5eaf_e731,
                0x3bdc_4776_94c3_06e7,
                0x2149_be4b_3949_fa24,
                0x64aa_6e06_49b2_078c,
                0x12b1_08ac_3364_3c3e
            });

            var gen = G1Affine.Generator;
            G1Projective test = new(gen.X * z, gen.Y * z, in z);

            Assert.IsTrue(test.IsOnCurve);

            test = new(in z, in test.Y, in test.Z);
            Assert.IsFalse(test.IsOnCurve);
        }

        [TestMethod]
        public void TestAffinePointEquality()
        {
            var a = G1Affine.Generator;
            var b = G1Affine.Identity;

            Assert.AreEqual(a, a);
            Assert.AreEqual(b, b);
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void TestProjectivePointEquality()
        {
            var a = G1Projective.Generator;
            var b = G1Projective.Identity;

            Assert.AreEqual(a, a);
            Assert.AreEqual(b, b);
            Assert.AreNotEqual(a, b);

            Fp z = Fp.FromRawUnchecked(new ulong[]
            {
                0xba7a_fa1f_9a6f_e250,
                0xfa0f_5b59_5eaf_e731,
                0x3bdc_4776_94c3_06e7,
                0x2149_be4b_3949_fa24,
                0x64aa_6e06_49b2_078c,
                0x12b1_08ac_3364_3c3e
            });

            G1Projective c = new(a.X * z, a.Y * z, in z);
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
            var a = G1Affine.Generator;
            var b = G1Affine.Identity;

            Assert.AreEqual(a, ConditionalSelect(in a, in b, false));
            Assert.AreEqual(b, ConditionalSelect(in a, in b, true));
        }

        [TestMethod]
        public void TestConditionallySelectProjective()
        {
            var a = G1Projective.Generator;
            var b = G1Projective.Identity;

            Assert.AreEqual(a, ConditionalSelect(in a, in b, false));
            Assert.AreEqual(b, ConditionalSelect(in a, in b, true));
        }

        [TestMethod]
        public void TestProjectiveToAffine()
        {
            var a = G1Projective.Generator;
            var b = G1Projective.Identity;

            Assert.IsTrue(new G1Affine(a).IsOnCurve);
            Assert.IsFalse(new G1Affine(a).IsIdentity);
            Assert.IsTrue(new G1Affine(b).IsOnCurve);
            Assert.IsTrue(new G1Affine(b).IsIdentity);

            Fp z = Fp.FromRawUnchecked(new ulong[]
            {
                0xba7a_fa1f_9a6f_e250,
                0xfa0f_5b59_5eaf_e731,
                0x3bdc_4776_94c3_06e7,
                0x2149_be4b_3949_fa24,
                0x64aa_6e06_49b2_078c,
                0x12b1_08ac_3364_3c3e
            });

            G1Projective c = new(a.X * z, a.Y * z, in z);

            Assert.AreEqual(G1Affine.Generator, new G1Affine(c));
        }

        [TestMethod]
        public void TestAffineToProjective()
        {
            var a = G1Affine.Generator;
            var b = G1Affine.Identity;

            Assert.IsTrue(new G1Projective(a).IsOnCurve);
            Assert.IsFalse(new G1Projective(a).IsIdentity);
            Assert.IsTrue(new G1Projective(b).IsOnCurve);
            Assert.IsTrue(new G1Projective(b).IsIdentity);
        }

        [TestMethod]
        public void TestDoubling()
        {
            {
                var tmp = G1Projective.Identity.Double();
                Assert.IsTrue(tmp.IsIdentity);
                Assert.IsTrue(tmp.IsOnCurve);
            }
            {
                var tmp = G1Projective.Generator.Double();
                Assert.IsFalse(tmp.IsIdentity);
                Assert.IsTrue(tmp.IsOnCurve);

                Assert.AreEqual(new G1Affine(Fp.FromRawUnchecked(new ulong[]
                {
                    0x53e9_78ce_58a9_ba3c,
                    0x3ea0_583c_4f3d_65f9,
                    0x4d20_bb47_f001_2960,
                    0xa54c_664a_e5b2_b5d9,
                    0x26b5_52a3_9d7e_b21f,
                    0x0008_895d_26e6_8785
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x7011_0b32_9829_3940,
                    0xda33_c539_3f1f_6afc,
                    0xb86e_dfd1_6a5a_a785,
                    0xaec6_d1c9_e7b1_c895,
                    0x25cf_c2b5_22d1_1720,
                    0x0636_1c83_f8d0_9b15
                })), new G1Affine(tmp));
            }
        }

        [TestMethod]
        public void TestProjectiveAddition()
        {
            {
                var a = G1Projective.Identity;
                var b = G1Projective.Identity;
                var c = a + b;
                Assert.IsTrue(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
            }
            {
                var a = G1Projective.Identity;
                var b = G1Projective.Generator;

                Fp z = Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                });

                b = new(b.X * z, b.Y * z, in z);
                var c = a + b;
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
                Assert.AreEqual(G1Projective.Generator, c);
            }
            {
                var a = G1Projective.Identity;
                var b = G1Projective.Generator;

                Fp z = Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                });

                b = new(b.X * z, b.Y * z, in z);
                var c = b + a;
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
                Assert.AreEqual(G1Projective.Generator, c);
            }
            {
                var a = G1Projective.Generator.Double().Double(); // 4P
                var b = G1Projective.Generator.Double(); // 2P
                var c = a + b;

                var d = G1Projective.Generator;
                for (int i = 0; i < 5; i++)
                {
                    d += G1Projective.Generator;
                }
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
                Assert.IsFalse(d.IsIdentity);
                Assert.IsTrue(d.IsOnCurve);
                Assert.AreEqual(c, d);
            }
            {
                Fp beta = Fp.FromRawUnchecked(new ulong[]
                {
                    0xcd03_c9e4_8671_f071,
                    0x5dab_2246_1fcd_a5d2,
                    0x5870_42af_d385_1b95,
                    0x8eb6_0ebe_01ba_cb9e,
                    0x03f9_7d6e_83d0_50d2,
                    0x18f0_2065_5463_8741
                });
                beta = beta.Square();
                var a = G1Projective.Generator.Double().Double();
                var b = new G1Projective(a.X * beta, -a.Y, in a.Z);
                Assert.IsTrue(a.IsOnCurve);
                Assert.IsTrue(b.IsOnCurve);

                var c = a + b;
                Assert.AreEqual(new G1Affine(new G1Projective(Fp.FromRawUnchecked(new ulong[]
                {
                    0x29e1_e987_ef68_f2d0,
                    0xc5f3_ec53_1db0_3233,
                    0xacd6_c4b6_ca19_730f,
                    0x18ad_9e82_7bc2_bab7,
                    0x46e3_b2c5_785c_c7a9,
                    0x07e5_71d4_2d22_ddd6
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x94d1_17a7_e5a5_39e7,
                    0x8e17_ef67_3d4b_5d22,
                    0x9d74_6aaf_508a_33ea,
                    0x8c6d_883d_2516_c9a2,
                    0x0bc3_b8d5_fb04_47f7,
                    0x07bf_a4c7_210f_4f44,
                }), in Fp.One)), new G1Affine(c));
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
            }
        }

        [TestMethod]
        public void TestMixedAddition()
        {
            {
                var a = G1Affine.Identity;
                var b = G1Projective.Identity;
                var c = a + b;
                Assert.IsTrue(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
            }
            {
                var a = G1Affine.Identity;
                var b = G1Projective.Generator;

                Fp z = Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                });

                b = new(b.X * z, b.Y * z, in z);
                var c = a + b;
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
                Assert.AreEqual(G1Projective.Generator, c);
            }
            {
                var a = G1Affine.Identity;
                var b = G1Projective.Generator;

                Fp z = Fp.FromRawUnchecked(new ulong[]
                {
                    0xba7a_fa1f_9a6f_e250,
                    0xfa0f_5b59_5eaf_e731,
                    0x3bdc_4776_94c3_06e7,
                    0x2149_be4b_3949_fa24,
                    0x64aa_6e06_49b2_078c,
                    0x12b1_08ac_3364_3c3e
                });

                b = new(b.X * z, b.Y * z, in z);
                var c = b + a;
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
                Assert.AreEqual(G1Projective.Generator, c);
            }
            {
                var a = G1Projective.Generator.Double().Double(); // 4P
                var b = G1Projective.Generator.Double(); // 2P
                var c = a + b;

                var d = G1Projective.Generator;
                for (int i = 0; i < 5; i++)
                {
                    d += G1Affine.Generator;
                }
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
                Assert.IsFalse(d.IsIdentity);
                Assert.IsTrue(d.IsOnCurve);
                Assert.AreEqual(c, d);
            }
            {
                Fp beta = Fp.FromRawUnchecked(new ulong[]
                {
                    0xcd03_c9e4_8671_f071,
                    0x5dab_2246_1fcd_a5d2,
                    0x5870_42af_d385_1b95,
                    0x8eb6_0ebe_01ba_cb9e,
                    0x03f9_7d6e_83d0_50d2,
                    0x18f0_2065_5463_8741
                });
                beta = beta.Square();
                var a = G1Projective.Generator.Double().Double();
                var b = new G1Projective(a.X * beta, -a.Y, in a.Z);
                var a2 = new G1Affine(a);
                Assert.IsTrue(a2.IsOnCurve);
                Assert.IsTrue(b.IsOnCurve);

                var c = a2 + b;
                Assert.AreEqual(new G1Affine(new G1Projective(Fp.FromRawUnchecked(new ulong[]
                {
                    0x29e1_e987_ef68_f2d0,
                    0xc5f3_ec53_1db0_3233,
                    0xacd6_c4b6_ca19_730f,
                    0x18ad_9e82_7bc2_bab7,
                    0x46e3_b2c5_785c_c7a9,
                    0x07e5_71d4_2d22_ddd6
                }), Fp.FromRawUnchecked(new ulong[]
                {
                    0x94d1_17a7_e5a5_39e7,
                    0x8e17_ef67_3d4b_5d22,
                    0x9d74_6aaf_508a_33ea,
                    0x8c6d_883d_2516_c9a2,
                    0x0bc3_b8d5_fb04_47f7,
                    0x07bf_a4c7_210f_4f44
                }), Fp.One)), new G1Affine(c));
                Assert.IsFalse(c.IsIdentity);
                Assert.IsTrue(c.IsOnCurve);
            }
        }

        [TestMethod]
        public void TestProjectiveNegationAndSubtraction()
        {
            var a = G1Projective.Generator.Double();
            Assert.AreEqual(a + (-a), G1Projective.Identity);
            Assert.AreEqual(a + (-a), a - a);
        }

        [TestMethod]
        public void TestAffineNegationAndSubtraction()
        {
            var a = G1Affine.Generator;
            Assert.AreEqual(G1Projective.Identity, new G1Projective(a) + (-a));
            Assert.AreEqual(new G1Projective(a) + (-a), new G1Projective(a) - a);
        }

        [TestMethod]
        public void TestProjectiveScalarMultiplication()
        {
            var g = G1Projective.Generator;
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

            Assert.AreEqual(g * a * b, g * c);
        }

        [TestMethod]
        public void TestAffineScalarMultiplication()
        {
            var g = G1Affine.Generator;
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

            Assert.AreEqual(new G1Affine(g * a) * b, g * c);
        }

        [TestMethod]
        public void TestIsTorsionFree()
        {
            var a = new G1Affine(Fp.FromRawUnchecked(new ulong[]
            {
                0x0aba_f895_b97e_43c8,
                0xba4c_6432_eb9b_61b0,
                0x1250_6f52_adfe_307f,
                0x7502_8c34_3933_6b72,
                0x8474_4f05_b8e9_bd71,
                0x113d_554f_b095_54f7
            }), Fp.FromRawUnchecked(new ulong[]
            {
                0x73e9_0e88_f5cf_01c0,
                0x3700_7b65_dd31_97e2,
                0x5cf9_a199_2f0d_7c78,
                0x4f83_c10b_9eb3_330d,
                0xf6a6_3f6f_07f6_0961,
                0x0c53_b5b9_7e63_4df3
            }));
            Assert.IsFalse(a.IsTorsionFree);

            Assert.IsTrue(G1Affine.Identity.IsTorsionFree);
            Assert.IsTrue(G1Affine.Generator.IsTorsionFree);
        }

        [TestMethod]
        public void TestMulByX()
        {
            // multiplying by `x` a point in G1 is the same as multiplying by
            // the equivalent scalar.
            var generator = G1Projective.Generator;
            var x = BLS_X_IS_NEGATIVE ? -new Scalar(BLS_X) : new Scalar(BLS_X);
            Assert.AreEqual(generator.MulByX(), generator * x);

            var point = G1Projective.Generator * new Scalar(42);
            Assert.AreEqual(point.MulByX(), point * x);
        }

        [TestMethod]
        public void TestClearCofactor()
        {
            // the generator (and the identity) are always on the curve,
            // even after clearing the cofactor
            var generator = G1Projective.Generator;
            Assert.IsTrue(generator.ClearCofactor().IsOnCurve);
            var id = G1Projective.Identity;
            Assert.IsTrue(id.ClearCofactor().IsOnCurve);

            var z = Fp.FromRawUnchecked(new ulong[]
            {
                0x3d2d1c670671394e,
                0x0ee3a800a2f7c1ca,
                0x270f4f21da2e5050,
                0xe02840a53f1be768,
                0x55debeb597512690,
                0x08bd25353dc8f791
            });

            var point = new G1Projective(Fp.FromRawUnchecked(new ulong[]
            {
                0x48af5ff540c817f0,
                0xd73893acaf379d5a,
                0xe6c43584e18e023c,
                0x1eda39c30f188b3e,
                0xf618c6d3ccc0f8d8,
                0x0073542cd671e16c
            }) * z, Fp.FromRawUnchecked(new ulong[]
            {
                0x57bf8be79461d0ba,
                0xfc61459cee3547c3,
                0x0d23567df1ef147b,
                0x0ee187bcce1d9b64,
                0xb0c8cfbe9dc8fdc1,
                0x1328661767ef368b
            }), z.Square() * z);

            Assert.IsTrue(point.IsOnCurve);
            Assert.IsFalse(new G1Affine(point).IsTorsionFree);
            var cleared_point = point.ClearCofactor();
            Assert.IsTrue(cleared_point.IsOnCurve);
            Assert.IsTrue(new G1Affine(cleared_point).IsTorsionFree);

            // in BLS12-381 the cofactor in G1 can be
            // cleared multiplying by (1-x)
            var h_eff = new Scalar(1) + new Scalar(BLS_X);
            Assert.AreEqual(point.ClearCofactor(), point * h_eff);
        }

        [TestMethod]
        public void TestBatchNormalize()
        {
            var a = G1Projective.Generator.Double();
            var b = a.Double();
            var c = b.Double();

            foreach (bool a_identity in new[] { false, true })
            {
                foreach (bool b_identity in new[] { false, true })
                {
                    foreach (bool c_identity in new[] { false, true })
                    {
                        var v = new[] { a, b, c };
                        if (a_identity)
                        {
                            v[0] = G1Projective.Identity;
                        }
                        if (b_identity)
                        {
                            v[1] = G1Projective.Identity;
                        }
                        if (c_identity)
                        {
                            v[2] = G1Projective.Identity;
                        }

                        var t = new G1Affine[3];
                        var expected = new[] { new G1Affine(v[0]), new G1Affine(v[1]), new G1Affine(v[2]) };

                        G1Projective.BatchNormalize(v, t);

                        CollectionAssert.AreEqual(expected, t);
                    }
                }
            }
        }
    }
}
