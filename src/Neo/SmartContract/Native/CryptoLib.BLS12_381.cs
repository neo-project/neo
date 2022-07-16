using Neo.Cryptography.BLS12_381;
using System;
using System.Runtime.Versioning;

namespace Neo.SmartContract.Native;

partial class CryptoLib
{
    private const int G1 = 48;
    private const int G2 = 96;
    private const int Gt = 576;

    /// <summary>
    /// The implementation of System.Crypto.PointAdd.
    /// Add operation of two gt points.
    /// </summary>
    /// <param name="g1">Gt1 point as byteArray</param>
    /// <param name="g2">Gt1 point as byteArray</param>
    /// <returns></returns>
    [ContractMethod(CpuFee = 1 << 19)]
    [RequiresPreviewFeatures]
    public static byte[] Bls12381Add(byte[] g1, byte[] g2)
    {
        if (g1.Length != g2.Length)
            throw new ArgumentException($"Bls12381 operation fault, type:format, error:type mismatch");
        return g1.Length switch
        {
            G1 => new G1Affine(new G1Projective(G1Affine.FromCompressed(g1)) + new G1Projective(G1Affine.FromCompressed(g2))).ToCompressed(),
            G2 => new G2Affine(new G2Projective(G2Affine.FromCompressed(g1)) + new G2Projective(G2Affine.FromCompressed(g2))).ToCompressed(),
            Gt => (Cryptography.BLS12_381.Gt.FromBytes(g1) + Cryptography.BLS12_381.Gt.FromBytes(g2)).ToArray(),
            _ => throw new ArgumentException($"Bls12381 operation fault, type:format, error:valid point length"),
        };
    }

    /// <summary>
    /// The implementation of System.Crypto.PointMul.
    /// Mul operation of gt point and mulitiplier
    /// </summary>
    /// <param name="g">Gt point as byteArray</param>
    /// <param name="mul">Mulitiplier</param>
    /// <returns></returns>
    [ContractMethod(CpuFee = 1 << 21)]
    [RequiresPreviewFeatures]
    public static byte[] Bls12381Mul(byte[] g, long mul)
    {
        Scalar X = mul < 0 ? -new Scalar((ulong)-mul) : new Scalar((ulong)mul);
        return g.Length switch
        {
            G1 => new G1Affine(G1Affine.FromCompressed(g) * X).ToCompressed(),
            G2 => new G2Affine(G2Affine.FromCompressed(g) * X).ToCompressed(),
            Gt => (Cryptography.BLS12_381.Gt.FromBytes(g) * X).ToArray(),
            _ => throw new ArgumentException($"Bls12381 operation fault, type:format, error:valid point length"),
        };
    }

    /// <summary>
    /// The implementation of System.Crypto.PointPairing.
    /// Pairing operation of g1 and g2
    /// </summary>
    /// <param name="g1">Gt point1 as byteArray</param>
    /// <param name="g2">Gt point2 as byteArray</param>
    /// <returns></returns>
    [ContractMethod(CpuFee = 1 << 23)]
    [RequiresPreviewFeatures]
    public static byte[] Bls12381Pairing(byte[] g1, byte[] g2)
    {
        if (g1.Length != G1 || g2.Length != G2)
            throw new ArgumentException($"Bls12381 operation fault, type:format, error:type mismatch");
        return Bls12.Pairing(G1Affine.FromCompressed(g1), G2Affine.FromCompressed(g2)).ToArray();
    }
}
