namespace Neo.Cryptography.BLS12_381;

static class ScalarConstants
{
    // The modulus as u32 limbs.
    public static readonly uint[] MODULUS_LIMBS_32 =
    {
        0x0000_0001,
        0xffff_ffff,
        0xfffe_5bfe,
        0x53bd_a402,
        0x09a1_d805,
        0x3339_d808,
        0x299d_7d48,
        0x73ed_a753
    };

    // The modulus as u64 limbs.
    public static readonly ulong[] MODULUS_LIMBS_64 =
    {
        0xffff_ffff_0000_0001,
        0x53bd_a402_fffe_5bfe,
        0x3339_d808_09a1_d805,
        0x73ed_a753_299d_7d48
    };

    // q = 0x73eda753299d7d483339d80809a1d80553bda402fffe5bfeffffffff00000001
    public static readonly Scalar MODULUS = new(MODULUS_LIMBS_64);

    // The number of bits needed to represent the modulus.
    public const uint MODULUS_BITS = 255;

    // GENERATOR = 7 (multiplicative generator of r-1 order, that is also quadratic nonresidue)
    public static readonly Scalar GENERATOR = new(new ulong[]
    {
        0x0000_000e_ffff_fff1,
        0x17e3_63d3_0018_9c0f,
        0xff9c_5787_6f84_57b0,
        0x3513_3220_8fc5_a8c4
    });

    // INV = -(q^{-1} mod 2^64) mod 2^64
    public const ulong INV = 0xffff_fffe_ffff_ffff;

    // R = 2^256 mod q
    public static readonly Scalar R = new(new ulong[]
    {
        0x0000_0001_ffff_fffe,
        0x5884_b7fa_0003_4802,
        0x998c_4fef_ecbc_4ff5,
        0x1824_b159_acc5_056f
    });

    // R^2 = 2^512 mod q
    public static readonly Scalar R2 = new(new ulong[]
    {
        0xc999_e990_f3f2_9c6d,
        0x2b6c_edcb_8792_5c23,
        0x05d3_1496_7254_398f,
        0x0748_d9d9_9f59_ff11
    });

    // R^3 = 2^768 mod q
    public static readonly Scalar R3 = new(new ulong[]
    {
        0xc62c_1807_439b_73af,
        0x1b3e_0d18_8cf0_6990,
        0x73d1_3c71_c7b5_f418,
        0x6e2a_5bb9_c8db_33e9
    });

    // 2^S * t = MODULUS - 1 with t odd
    public const uint S = 32;

    // GENERATOR^t where t * 2^s + 1 = q with t odd.
    // In other words, this is a 2^s root of unity.
    // `GENERATOR = 7 mod q` is a generator of the q - 1 order multiplicative subgroup.
    public static readonly Scalar ROOT_OF_UNITY = new(new ulong[]
    {
        0xb9b5_8d8c_5f0e_466a,
        0x5b1b_4c80_1819_d7ec,
        0x0af5_3ae3_52a3_1e64,
        0x5bf3_adda_19e9_b27b
    });
}
