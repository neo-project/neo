// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Scalar.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using static Neo.Cryptography.BLS12_381.ScalarConstants;

namespace Neo.Cryptography.BLS12_381.Tests;

[TestClass]
public class UT_Scalar
{
    private static readonly Scalar LARGEST = new(new ulong[]
    {
        0xffff_ffff_0000_0000,
        0x53bd_a402_fffe_5bfe,
        0x3339_d808_09a1_d805,
        0x73ed_a753_299d_7d48
    });

    [TestMethod]
    public void TestInv()
    {
        // Compute -(q^{-1} mod 2^64) mod 2^64 by exponentiating
        // by totient(2**64) - 1

        var inv = 1ul;
        for (var i = 0; i < 63; i++)
        {
            inv = unchecked(inv * inv);
            inv = unchecked(inv * MODULUS_LIMBS_64[0]);
        }
        inv = unchecked(~inv + 1);

        Assert.AreEqual(INV, inv);
    }

    [TestMethod]
    public void TestToString()
    {
        Assert.AreEqual("0x0000000000000000000000000000000000000000000000000000000000000000", Scalar.Zero.ToString());
        Assert.AreEqual("0x0000000000000000000000000000000000000000000000000000000000000001", Scalar.One.ToString());
        Assert.AreEqual("0x1824b159acc5056f998c4fefecbc4ff55884b7fa0003480200000001fffffffe", R2.ToString());
    }

    [TestMethod]
    public void TestEquality()
    {
        Assert.AreEqual(Scalar.Zero, Scalar.Zero);
        Assert.AreEqual(Scalar.One, Scalar.One);
        Assert.AreEqual(R2, R2);

        Assert.AreNotEqual(Scalar.Zero, Scalar.One);
        Assert.AreNotEqual(Scalar.One, R2);
    }

    [TestMethod]
    public void TestToBytes()
    {
        CollectionAssert.AreEqual(new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0
        }, Scalar.Zero.ToArray());

        CollectionAssert.AreEqual(new byte[]
        {
            1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0
        }, Scalar.One.ToArray());

        CollectionAssert.AreEqual(new byte[]
        {
            254, 255, 255, 255, 1, 0, 0, 0, 2, 72, 3, 0, 250, 183, 132, 88, 245, 79, 188, 236, 239,
            79, 140, 153, 111, 5, 197, 172, 89, 177, 36, 24
        }, R2.ToArray());

        CollectionAssert.AreEqual(new byte[]
        {
            0, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 57, 51, 72, 125, 157, 41, 83, 167, 237, 115
        }, (-Scalar.One).ToArray());
    }

    [TestMethod]
    public void TestFromBytes()
    {
        Assert.AreEqual(Scalar.Zero, Scalar.FromBytes(new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0
        }));

        Assert.AreEqual(Scalar.One, Scalar.FromBytes(new byte[]
        {
            1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0
        }));

        Assert.AreEqual(R2, Scalar.FromBytes(new byte[]
        {
            254, 255, 255, 255, 1, 0, 0, 0, 2, 72, 3, 0, 250, 183, 132, 88, 245, 79, 188, 236, 239,
            79, 140, 153, 111, 5, 197, 172, 89, 177, 36, 24
        }));

        // -1 should work
        Scalar.FromBytes(new byte[]
        {
            0, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 57, 51, 72, 125, 157, 41, 83, 167, 237, 115
        });

        // modulus is invalid
        Assert.ThrowsException<FormatException>(() => Scalar.FromBytes(new byte[]
        {
            1, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 57, 51, 72, 125, 157, 41, 83, 167, 237, 115
        }));

        // Anything larger than the modulus is invalid
        Assert.ThrowsException<FormatException>(() => Scalar.FromBytes(new byte[]
        {
            2, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 57, 51, 72, 125, 157, 41, 83, 167, 237, 115
        }));
        Assert.ThrowsException<FormatException>(() => Scalar.FromBytes(new byte[]
        {
            1, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 58, 51, 72, 125, 157, 41, 83, 167, 237, 115
        }));
        Assert.ThrowsException<FormatException>(() => Scalar.FromBytes(new byte[]
        {
            1, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 57, 51, 72, 125, 157, 41, 83, 167, 237, 116
        }));
    }

    [TestMethod]
    public void TestFromBytesWideR2()
    {
        Assert.AreEqual(R2, Scalar.FromBytesWide(new byte[]
        {
            254, 255, 255, 255, 1, 0, 0, 0, 2, 72, 3, 0, 250, 183, 132, 88, 245, 79, 188, 236, 239,
            79, 140, 153, 111, 5, 197, 172, 89, 177, 36, 24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        }));
    }

    [TestMethod]
    public void TestFromBytesWideNegativeOne()
    {
        Assert.AreEqual(-Scalar.One, Scalar.FromBytesWide(new byte[]
        {
            0, 0, 0, 0, 255, 255, 255, 255, 254, 91, 254, 255, 2, 164, 189, 83, 5, 216, 161, 9, 8,
            216, 57, 51, 72, 125, 157, 41, 83, 167, 237, 115, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        }));
    }

    [TestMethod]
    public void TestFromBytesWideMaximum()
    {
        Assert.AreEqual(new Scalar(new ulong[]
        {
            0xc62c_1805_439b_73b1,
            0xc2b9_551e_8ced_218e,
            0xda44_ec81_daf9_a422,
            0x5605_aa60_1c16_2e79
        }), Scalar.FromBytesWide(Enumerable.Repeat<byte>(0xff, 64).ToArray()));
    }

    [TestMethod]
    public void TestZero()
    {
        Assert.AreEqual(Scalar.Zero, -Scalar.Zero);
        Assert.AreEqual(Scalar.Zero, Scalar.Zero + Scalar.Zero);
        Assert.AreEqual(Scalar.Zero, Scalar.Zero - Scalar.Zero);
        Assert.AreEqual(Scalar.Zero, Scalar.Zero * Scalar.Zero);
    }

    [TestMethod]
    public void TestAddition()
    {
        var tmp = LARGEST;
        tmp += LARGEST;

        Assert.AreEqual(new Scalar(new ulong[]
        {
            0xffff_fffe_ffff_ffff,
            0x53bd_a402_fffe_5bfe,
            0x3339_d808_09a1_d805,
            0x73ed_a753_299d_7d48
        }), tmp);

        tmp = LARGEST;
        tmp += new Scalar(new ulong[] { 1, 0, 0, 0 });

        Assert.AreEqual(Scalar.Zero, tmp);
    }

    [TestMethod]
    public void TestNegation()
    {
        var tmp = -LARGEST;

        Assert.AreEqual(new Scalar(new ulong[] { 1, 0, 0, 0 }), tmp);

        tmp = -Scalar.Zero;
        Assert.AreEqual(Scalar.Zero, tmp);
        tmp = -new Scalar(new ulong[] { 1, 0, 0, 0 });
        Assert.AreEqual(LARGEST, tmp);
    }

    [TestMethod]
    public void TestSubtraction()
    {
        var tmp = LARGEST;
        tmp -= LARGEST;

        Assert.AreEqual(Scalar.Zero, tmp);

        tmp = Scalar.Zero;
        tmp -= LARGEST;

        var tmp2 = MODULUS;
        tmp2 -= LARGEST;

        Assert.AreEqual(tmp, tmp2);
    }

    [TestMethod]
    public void TestMultiplication()
    {
        var cur = LARGEST;

        for (var i = 0; i < 100; i++)
        {
            var tmp = cur;
            tmp *= cur;

            var tmp2 = Scalar.Zero;
            foreach (var b in cur
                .ToArray()
                .SelectMany(p => Enumerable.Range(0, 8).Select(q => ((p >> q) & 1) == 1))
                .Reverse())
            {
                var tmp3 = tmp2;
                tmp2 += tmp3;

                if (b)
                {
                    tmp2 += cur;
                }
            }

            Assert.AreEqual(tmp, tmp2);

            cur += LARGEST;
        }
    }

    [TestMethod]
    public void TestSquaring()
    {
        var cur = LARGEST;

        for (var i = 0; i < 100; i++)
        {
            var tmp = cur;
            tmp = tmp.Square();

            var tmp2 = Scalar.Zero;
            foreach (var b in cur
                .ToArray()
                .SelectMany(p => Enumerable.Range(0, 8).Select(q => ((p >> q) & 1) == 1))
                .Reverse())
            {
                var tmp3 = tmp2;
                tmp2 += tmp3;

                if (b)
                {
                    tmp2 += cur;
                }
            }

            Assert.AreEqual(tmp, tmp2);

            cur += LARGEST;
        }
    }

    [TestMethod]
    public void TestInversion()
    {
        Assert.ThrowsException<DivideByZeroException>(() => Scalar.Zero.Invert());
        Assert.AreEqual(Scalar.One, Scalar.One.Invert());
        Assert.AreEqual(-Scalar.One, (-Scalar.One).Invert());

        var tmp = R2;

        for (var i = 0; i < 100; i++)
        {
            var tmp2 = tmp.Invert();
            tmp2 *= tmp;

            Assert.AreEqual(Scalar.One, tmp2);

            tmp += R2;
        }
    }

    [TestMethod]
    public void TestInvertIsPow()
    {
        ulong[] q_minus_2 =
        {
            0xffff_fffe_ffff_ffff,
            0x53bd_a402_fffe_5bfe,
            0x3339_d808_09a1_d805,
            0x73ed_a753_299d_7d48
        };

        var r1 = R;
        var r2 = R;
        var r3 = R;

        for (var i = 0; i < 100; i++)
        {
            r1 = r1.Invert();
            r2 = r2.PowVartime(q_minus_2);
            r3 = r3.Pow(q_minus_2);

            Assert.AreEqual(r1, r2);
            Assert.AreEqual(r2, r3);
            // Add R so we check something different next time around
            r1 += R;
            r2 = r1;
            r3 = r1;
        }
    }

    [TestMethod]
    public void TestSqrt()
    {
        Assert.AreEqual(Scalar.Zero.Sqrt(), Scalar.Zero);

        var square = new Scalar(new ulong[]
        {
            0x46cd_85a5_f273_077e,
            0x1d30_c47d_d68f_c735,
            0x77f6_56f6_0bec_a0eb,
            0x494a_a01b_df32_468d
        });

        var none_count = 0;

        for (var i = 0; i < 100; i++)
        {
            Scalar square_root;
            try
            {
                square_root = square.Sqrt();
                Assert.AreEqual(square, square_root * square_root);
            }
            catch (ArithmeticException)
            {
                none_count++;
            }
            square -= Scalar.One;
        }

        Assert.AreEqual(49, none_count);
    }

    [TestMethod]
    public void TestFromRaw()
    {
        Assert.AreEqual(Scalar.FromRaw(new ulong[]
        {
            0x0001_ffff_fffd,
            0x5884_b7fa_0003_4802,
            0x998c_4fef_ecbc_4ff5,
            0x1824_b159_acc5_056f
        }), Scalar.FromRaw(Enumerable.Repeat(0xffff_ffff_ffff_ffff, 4).ToArray()));

        Assert.AreEqual(Scalar.Zero, Scalar.FromRaw(MODULUS_LIMBS_64));

        Assert.AreEqual(R, Scalar.FromRaw(new ulong[] { 1, 0, 0, 0 }));
    }

    [TestMethod]
    public void TestDouble()
    {
        var a = Scalar.FromRaw(new ulong[]
        {
            0x1fff_3231_233f_fffd,
            0x4884_b7fa_0003_4802,
            0x998c_4fef_ecbc_4ff3,
            0x1824_b159_acc5_0562
        });

        Assert.AreEqual(a + a, a.Double());
    }
}
