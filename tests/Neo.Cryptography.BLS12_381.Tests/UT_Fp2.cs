using System.Collections;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;

namespace Neo.Cryptography.BLS12_381.Tests;

[TestClass]
public class UT_Fp2
{
    [TestMethod]
    public void TestConditionalSelection()
    {
        var a = new Fp2(
            Fp.FromRawUnchecked(new ulong[] { 1, 2, 3, 4, 5, 6 }),
            Fp.FromRawUnchecked(new ulong[] { 7, 8, 9, 10, 11, 12 })
        );
        var b = new Fp2(
            Fp.FromRawUnchecked(new ulong[] { 13, 14, 15, 16, 17, 18 }),
            Fp.FromRawUnchecked(new ulong[] { 19, 20, 21, 22, 23, 24 })
        );

        Assert.AreEqual(a, ConditionalSelect(in a, in b, false));
        Assert.AreEqual(b, ConditionalSelect(in a, in b, true));
    }

    [TestMethod]
    public void TestEquality()
    {
        static bool IsEqual(in Fp2 a, in Fp2 b)
        {
            var eq = StructuralComparisons.StructuralEqualityComparer.Equals(a, b);
            var ct_eq = a == b;
            Assert.AreEqual(eq, ct_eq);
            return eq;
        }

        Assert.IsTrue(IsEqual(
            new Fp2(Fp.FromRawUnchecked(new ulong[] { 1, 2, 3, 4, 5, 6 }), Fp.FromRawUnchecked(new ulong[] { 7, 8, 9, 10, 11, 12 })),
            new Fp2(Fp.FromRawUnchecked(new ulong[] { 1, 2, 3, 4, 5, 6 }), Fp.FromRawUnchecked(new ulong[] { 7, 8, 9, 10, 11, 12 }))
        ));

        Assert.IsFalse(IsEqual(
            new Fp2(Fp.FromRawUnchecked(new ulong[] { 2, 2, 3, 4, 5, 6 }), Fp.FromRawUnchecked(new ulong[] { 7, 8, 9, 10, 11, 12 })),
            new Fp2(Fp.FromRawUnchecked(new ulong[] { 1, 2, 3, 4, 5, 6 }), Fp.FromRawUnchecked(new ulong[] { 7, 8, 9, 10, 11, 12 }))
        ));

        Assert.IsFalse(IsEqual(
            new Fp2(Fp.FromRawUnchecked(new ulong[] { 1, 2, 3, 4, 5, 6 }), Fp.FromRawUnchecked(new ulong[] { 2, 8, 9, 10, 11, 12 })),
            new Fp2(Fp.FromRawUnchecked(new ulong[] { 1, 2, 3, 4, 5, 6 }), Fp.FromRawUnchecked(new ulong[] { 7, 8, 9, 10, 11, 12 }))
        ));
    }

    [TestMethod]
    public void TestSquaring()
    {
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xc9a2_1831_63ee_70d4,
            0xbc37_70a7_196b_5c91,
            0xa247_f8c1_304c_5f44,
            0xb01f_c2a3_726c_80b5,
            0xe1d2_93e5_bbd9_19c9,
            0x04b7_8e80_020e_f2ca,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x952e_a446_0462_618f,
            0x238d_5edd_f025_c62f,
            0xf6c9_4b01_2ea9_2e72,
            0x03ce_24ea_c1c9_3808,
            0x0559_50f9_45da_483c,
            0x010a_768d_0df4_eabc,
        }));
        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xa1e0_9175_a4d2_c1fe,
            0x8b33_acfc_204e_ff12,
            0xe244_15a1_1b45_6e42,
            0x61d9_96b1_b6ee_1936,
            0x1164_dbe8_667c_853c,
            0x0788_557a_cc7d_9c79,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xda6a_87cc_6f48_fa36,
            0x0fc7_b488_277c_1903,
            0x9445_ac4a_dc44_8187,
            0x0261_6d5b_c909_9209,
            0xdbed_4677_2db5_8d48,
            0x11b9_4d50_76c7_b7b1,
        }));

        Assert.AreEqual(a.Square(), b);
    }

    [TestMethod]
    public void TestMultiplication()
    {
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xc9a2_1831_63ee_70d4,
            0xbc37_70a7_196b_5c91,
            0xa247_f8c1_304c_5f44,
            0xb01f_c2a3_726c_80b5,
            0xe1d2_93e5_bbd9_19c9,
            0x04b7_8e80_020e_f2ca,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x952e_a446_0462_618f,
            0x238d_5edd_f025_c62f,
            0xf6c9_4b01_2ea9_2e72,
            0x03ce_24ea_c1c9_3808,
            0x0559_50f9_45da_483c,
            0x010a_768d_0df4_eabc,
        }));
        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xa1e0_9175_a4d2_c1fe,
            0x8b33_acfc_204e_ff12,
            0xe244_15a1_1b45_6e42,
            0x61d9_96b1_b6ee_1936,
            0x1164_dbe8_667c_853c,
            0x0788_557a_cc7d_9c79,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xda6a_87cc_6f48_fa36,
            0x0fc7_b488_277c_1903,
            0x9445_ac4a_dc44_8187,
            0x0261_6d5b_c909_9209,
            0xdbed_4677_2db5_8d48,
            0x11b9_4d50_76c7_b7b1,
        }));
        var c = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xf597_483e_27b4_e0f7,
            0x610f_badf_811d_ae5f,
            0x8432_af91_7714_327a,
            0x6a9a_9603_cf88_f09e,
            0xf05a_7bf8_bad0_eb01,
            0x0954_9131_c003_ffae,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x963b_02d0_f93d_37cd,
            0xc95c_e1cd_b30a_73d4,
            0x3087_25fa_3126_f9b8,
            0x56da_3c16_7fab_0d50,
            0x6b50_86b5_f4b6_d6af,
            0x09c3_9f06_2f18_e9f2,
        }));

        Assert.AreEqual(c, a * b);
    }

    [TestMethod]
    public void TestAddition()
    {
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xc9a2_1831_63ee_70d4,
            0xbc37_70a7_196b_5c91,
            0xa247_f8c1_304c_5f44,
            0xb01f_c2a3_726c_80b5,
            0xe1d2_93e5_bbd9_19c9,
            0x04b7_8e80_020e_f2ca,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x952e_a446_0462_618f,
            0x238d_5edd_f025_c62f,
            0xf6c9_4b01_2ea9_2e72,
            0x03ce_24ea_c1c9_3808,
            0x0559_50f9_45da_483c,
            0x010a_768d_0df4_eabc,
        }));
        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xa1e0_9175_a4d2_c1fe,
            0x8b33_acfc_204e_ff12,
            0xe244_15a1_1b45_6e42,
            0x61d9_96b1_b6ee_1936,
            0x1164_dbe8_667c_853c,
            0x0788_557a_cc7d_9c79,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xda6a_87cc_6f48_fa36,
            0x0fc7_b488_277c_1903,
            0x9445_ac4a_dc44_8187,
            0x0261_6d5b_c909_9209,
            0xdbed_4677_2db5_8d48,
            0x11b9_4d50_76c7_b7b1,
        }));
        var c = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x6b82_a9a7_08c1_32d2,
            0x476b_1da3_39ba_5ba4,
            0x848c_0e62_4b91_cd87,
            0x11f9_5955_295a_99ec,
            0xf337_6fce_2255_9f06,
            0x0c3f_e3fa_ce8c_8f43,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x6f99_2c12_73ab_5bc5,
            0x3355_1366_17a1_df33,
            0x8b0e_f74c_0aed_aff9,
            0x062f_9246_8ad2_ca12,
            0xe146_9770_738f_d584,
            0x12c3_c3dd_84bc_a26d,
        }));

        Assert.AreEqual(a + b, c);
    }

    [TestMethod]
    public void TestSubtraction()
    {
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xc9a2_1831_63ee_70d4,
            0xbc37_70a7_196b_5c91,
            0xa247_f8c1_304c_5f44,
            0xb01f_c2a3_726c_80b5,
            0xe1d2_93e5_bbd9_19c9,
            0x04b7_8e80_020e_f2ca,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x952e_a446_0462_618f,
            0x238d_5edd_f025_c62f,
            0xf6c9_4b01_2ea9_2e72,
            0x03ce_24ea_c1c9_3808,
            0x0559_50f9_45da_483c,
            0x010a_768d_0df4_eabc,
        }));
        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xa1e0_9175_a4d2_c1fe,
            0x8b33_acfc_204e_ff12,
            0xe244_15a1_1b45_6e42,
            0x61d9_96b1_b6ee_1936,
            0x1164_dbe8_667c_853c,
            0x0788_557a_cc7d_9c79,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xda6a_87cc_6f48_fa36,
            0x0fc7_b488_277c_1903,
            0x9445_ac4a_dc44_8187,
            0x0261_6d5b_c909_9209,
            0xdbed_4677_2db5_8d48,
            0x11b9_4d50_76c7_b7b1,
        }));
        var c = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xe1c0_86bb_bf1b_5981,
            0x4faf_c3a9_aa70_5d7e,
            0x2734_b5c1_0bb7_e726,
            0xb2bd_7776_af03_7a3e,
            0x1b89_5fb3_98a8_4164,
            0x1730_4aef_6f11_3cec,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x74c3_1c79_9519_1204,
            0x3271_aa54_79fd_ad2b,
            0xc9b4_7157_4915_a30f,
            0x65e4_0313_ec44_b8be,
            0x7487_b238_5b70_67cb,
            0x0952_3b26_d0ad_19a4,
        }));

        Assert.AreEqual(c, a - b);
    }

    [TestMethod]
    public void TestNegation()
    {
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xc9a2_1831_63ee_70d4,
            0xbc37_70a7_196b_5c91,
            0xa247_f8c1_304c_5f44,
            0xb01f_c2a3_726c_80b5,
            0xe1d2_93e5_bbd9_19c9,
            0x04b7_8e80_020e_f2ca,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x952e_a446_0462_618f,
            0x238d_5edd_f025_c62f,
            0xf6c9_4b01_2ea9_2e72,
            0x03ce_24ea_c1c9_3808,
            0x0559_50f9_45da_483c,
            0x010a_768d_0df4_eabc,
        }));
        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0xf05c_e7ce_9c11_39d7,
            0x6274_8f57_97e8_a36d,
            0xc4e8_d9df_c664_96df,
            0xb457_88e1_8118_9209,
            0x6949_13d0_8772_930d,
            0x1549_836a_3770_f3cf,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x24d0_5bb9_fb9d_491c,
            0xfb1e_a120_c12e_39d0,
            0x7067_879f_c807_c7b1,
            0x60a9_269a_31bb_dab6,
            0x45c2_56bc_fd71_649b,
            0x18f6_9b5d_2b8a_fbde,
        }));

        Assert.AreEqual(b, -a);
    }

    [TestMethod]
    public void TestSqrt()
    {
        // a = 1488924004771393321054797166853618474668089414631333405711627789629391903630694737978065425271543178763948256226639*u + 784063022264861764559335808165825052288770346101304131934508881646553551234697082295473567906267937225174620141295
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x2bee_d146_27d7_f9e9,
            0xb661_4e06_660e_5dce,
            0x06c4_cc7c_2f91_d42c,
            0x996d_7847_4b7a_63cc,
            0xebae_bc4c_820d_574e,
            0x1886_5e12_d93f_d845,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x7d82_8664_baf4_f566,
            0xd17e_6639_96ec_7339,
            0x679e_ad55_cb40_78d0,
            0xfe3b_2260_e001_ec28,
            0x3059_93d0_43d9_1b68,
            0x0626_f03c_0489_b72d,
        }));

        Assert.AreEqual(a, a.Sqrt().Square());

        // b = 5, which is a generator of the p - 1 order
        // multiplicative subgroup
        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x6631_0000_0010_5545,
            0x2114_0040_0eec_000d,
            0x3fa7_af30_c820_e316,
            0xc52a_8b8d_6387_695d,
            0x9fb4_e61d_1e83_eac5,
            0x005c_b922_afe8_4dc7,
        }), Fp.Zero);

        Assert.AreEqual(b, b.Sqrt().Square());

        // c = 25, which is a generator of the (p - 1) / 2 order
        // multiplicative subgroup
        var c = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x44f6_0000_0051_ffae,
            0x86b8_0141_9948_0043,
            0xd715_9952_f1f3_794a,
            0x755d_6e3d_fe1f_fc12,
            0xd36c_d6db_5547_e905,
            0x02f8_c8ec_bf18_67bb,
        }), Fp.Zero);

        Assert.AreEqual(c, c.Sqrt().Square());

        // 2155129644831861015726826462986972654175647013268275306775721078997042729172900466542651176384766902407257452753362*u + 2796889544896299244102912275102369318775038861758288697415827248356648685135290329705805931514906495247464901062529
        // is nonsquare.
        Assert.ThrowsException<ArithmeticException>(() =>
            new Fp2(Fp.FromRawUnchecked(new ulong[]
            {
                0xc5fa_1bc8_fd00_d7f6,
                0x3830_ca45_4606_003b,
                0x2b28_7f11_04b1_02da,
                0xa7fb_30f2_8230_f23e,
                0x339c_db9e_e953_dbf0,
                0x0d78_ec51_d989_fc57,
            }), Fp.FromRawUnchecked(new ulong[]{
                0x27ec_4898_cf87_f613,
                0x9de1_394e_1abb_05a5,
                0x0947_f85d_c170_fc14,
                0x586f_bc69_6b61_14b7,
                0x2b34_75a4_077d_7169,
                0x13e1_c895_cc4b_6c22,
            })).Sqrt());
    }

    [TestMethod]
    public void TestInversion()
    {
        var a = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x1128_ecad_6754_9455,
            0x9e7a_1cff_3a4e_a1a8,
            0xeb20_8d51_e08b_cf27,
            0xe98a_d408_11f5_fc2b,
            0x736c_3a59_232d_511d,
            0x10ac_d42d_29cf_cbb6,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xd328_e37c_c2f5_8d41,
            0x948d_f085_8a60_5869,
            0x6032_f9d5_6f93_a573,
            0x2be4_83ef_3fff_dc87,
            0x30ef_61f8_8f48_3c2a,
            0x1333_f55a_3572_5be0,
        }));

        var b = new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x0581_a133_3d4f_48a6,
            0x5824_2f6e_f074_8500,
            0x0292_c955_349e_6da5,
            0xba37_721d_dd95_fcd0,
            0x70d1_6790_3aa5_dfc5,
            0x1189_5e11_8b58_a9d5,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0x0eda_09d2_d7a8_5d17,
            0x8808_e137_a7d1_a2cf,
            0x43ae_2625_c1ff_21db,
            0xf85a_c9fd_f7a7_4c64,
            0x8fcc_dda5_b8da_9738,
            0x08e8_4f0c_b32c_d17d,
        }));
        Assert.AreEqual(b, a.Invert());

        Assert.ThrowsException<DivideByZeroException>(() => Fp2.Zero.Invert());
    }

    [TestMethod]
    public void TestLexicographicLargest()
    {
        Assert.IsFalse(Fp2.Zero.LexicographicallyLargest());
        Assert.IsFalse(Fp2.One.LexicographicallyLargest());
        Assert.IsTrue(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x1128_ecad_6754_9455,
            0x9e7a_1cff_3a4e_a1a8,
            0xeb20_8d51_e08b_cf27,
            0xe98a_d408_11f5_fc2b,
            0x736c_3a59_232d_511d,
            0x10ac_d42d_29cf_cbb6,
        }), Fp.FromRawUnchecked(new ulong[]
        {
            0xd328_e37c_c2f5_8d41,
            0x948d_f085_8a60_5869,
            0x6032_f9d5_6f93_a573,
            0x2be4_83ef_3fff_dc87,
            0x30ef_61f8_8f48_3c2a,
            0x1333_f55a_3572_5be0,
        })).LexicographicallyLargest());
        Assert.IsFalse(new Fp2(-Fp.FromRawUnchecked(new ulong[]
        {
            0x1128_ecad_6754_9455,
            0x9e7a_1cff_3a4e_a1a8,
            0xeb20_8d51_e08b_cf27,
            0xe98a_d408_11f5_fc2b,
            0x736c_3a59_232d_511d,
            0x10ac_d42d_29cf_cbb6,
        }), -Fp.FromRawUnchecked(new ulong[]
        {
            0xd328_e37c_c2f5_8d41,
            0x948d_f085_8a60_5869,
            0x6032_f9d5_6f93_a573,
            0x2be4_83ef_3fff_dc87,
            0x30ef_61f8_8f48_3c2a,
            0x1333_f55a_3572_5be0,
        })).LexicographicallyLargest());
        Assert.IsFalse(new Fp2(Fp.FromRawUnchecked(new ulong[]
        {
            0x1128_ecad_6754_9455,
            0x9e7a_1cff_3a4e_a1a8,
            0xeb20_8d51_e08b_cf27,
            0xe98a_d408_11f5_fc2b,
            0x736c_3a59_232d_511d,
            0x10ac_d42d_29cf_cbb6,
        }), Fp.Zero).LexicographicallyLargest());
        Assert.IsTrue(new Fp2(-Fp.FromRawUnchecked(new ulong[]
        {
            0x1128_ecad_6754_9455,
            0x9e7a_1cff_3a4e_a1a8,
            0xeb20_8d51_e08b_cf27,
            0xe98a_d408_11f5_fc2b,
            0x736c_3a59_232d_511d,
            0x10ac_d42d_29cf_cbb6,
        }), Fp.Zero).LexicographicallyLargest());
    }
}
