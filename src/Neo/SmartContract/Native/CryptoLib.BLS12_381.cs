using Neo.Cryptography.BLS12_381;
using Neo.VM.Types;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Neo.SmartContract.Native;

partial class CryptoLib
{
    private const int G1 = 104;
    private const int G2 = 200;
    private const int Gt = 576;

    /// <summary>
    /// The implementation of System.Crypto.GetPoint.
    /// Convert data to InteropInterface type.
    /// </summary>
    /// <param name="g">G point as byteArray</param>
    /// <returns></returns>
    [ContractMethod(CpuFee = 1 << 19)]
    [RequiresPreviewFeatures]
    public static InteropInterface Bls12381GetPoint(byte[] g)
    {
        return g.Length switch
        {
            G1 => new InteropInterface(g),
            G2 => new InteropInterface(g),
            Gt => new InteropInterface(g),
            _ => throw new ArgumentException($"Bls12381 operation fault, type:format, error:valid point length"),
        };
    }

    /// <summary>
    /// The implementation of System.Crypto.PointAdd.
    /// Add operation of two gt points.
    /// </summary>
    /// <param name="g1">Gt1 point as byteArray</param>
    /// <param name="g2">Gt1 point as byteArray</param>
    /// <returns></returns>
    [ContractMethod(CpuFee = 1 << 19)]
    [RequiresPreviewFeatures]
    public static InteropInterface Bls12381Add(InteropInterface g1, InteropInterface g2)
    {
        byte[] t1 = g1.GetInterface<byte[]>();
        byte[] t2 = g2.GetInterface<byte[]>();
        if (t1.Length != t2.Length)
            throw new ArgumentException($"Bls12381 operation fault, type:format, error:type mismatch");
        switch (t1.Length)
        {
            case G1:
                var r1 = new G1Affine(new G1Projective(MemoryMarshal.AsRef<G1Affine>(t1)) + new G1Projective(MemoryMarshal.AsRef<G1Affine>(t2)));
                return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r1, 1)).ToArray());
            case G2:
                var r2 = new G2Affine(new G2Projective(MemoryMarshal.AsRef<G2Affine>(t1)) + new G2Projective(MemoryMarshal.AsRef<G2Affine>(t2)));
                return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r2, 1)).ToArray());
            case Gt:
                var r = MemoryMarshal.AsRef<Gt>(t1) + MemoryMarshal.AsRef<Gt>(t2);
                return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r, 1)).ToArray());
            default:
                throw new ArgumentException($"Bls12381 operation fault, type:format, error:valid point length");
        }
    }

    /// <summary>
    /// The implementation of System.Crypto.PointMul.
    /// Mul operation of gt point and multiplier
    /// </summary>
    /// <param name="g">Gt point as byteArray</param>
    /// <param name="mul">Multiplier</param>
    /// <param name="neg">negative number</param>
    /// <returns></returns>
    [ContractMethod(CpuFee = 1 << 21)]
    [RequiresPreviewFeatures]
    public static InteropInterface Bls12381Mul(InteropInterface g, ulong mul, bool neg)
    {
        Scalar X = neg ? -new Scalar(mul) : new Scalar(mul);
        byte[] t = g.GetInterface<byte[]>();
        switch (t.Length)
        {
            case G1:
                var r1 = MemoryMarshal.AsRef<G1Affine>(t) * X;
                return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r1, 1)).ToArray());
            case G2:
                var r2 = MemoryMarshal.AsRef<G2Affine>(t) * X;
                return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r2, 1)).ToArray());
            case Gt:
                var r = MemoryMarshal.AsRef<Gt>(t) * X;
                return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r, 1)).ToArray());
            default:
                throw new ArgumentException($"Bls12381 operation fault, type:format, error:valid point length");
        }
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
    public static InteropInterface Bls12381Pairing(InteropInterface g1, InteropInterface g2)
    {
        byte[] t1 = g1.GetInterface<byte[]>();
        byte[] t2 = g2.GetInterface<byte[]>();
        if (t1.Length != G1 || t2.Length != G2)
            throw new ArgumentException($"Bls12381 operation fault, type:format, error:type mismatch");
        var r = Bls12.Pairing(MemoryMarshal.AsRef<G1Affine>(t1), MemoryMarshal.AsRef<G2Affine>(t2));
        return new InteropInterface(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r, 1)).ToArray());
    }
}
