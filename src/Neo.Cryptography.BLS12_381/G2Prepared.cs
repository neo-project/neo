using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.MillerLoopUtility;

namespace Neo.Cryptography.BLS12_381;

partial class G2Prepared
{
    public readonly bool Infinity;
    public readonly List<(Fp2, Fp2, Fp2)> Coeffs;

    public G2Prepared(in G2Affine q)
    {
        Infinity = q.IsIdentity;
        var q2 = ConditionalSelect(in q, in G2Affine.Generator, Infinity);
        var adder = new Adder(q2);
        MillerLoop<object?, Adder>(adder);
        Coeffs = adder.Coeffs;
        if (Coeffs.Count != 68) throw new InvalidOperationException();
    }
}
