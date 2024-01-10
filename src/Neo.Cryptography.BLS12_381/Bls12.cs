using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.MillerLoopUtility;

namespace Neo.Cryptography.BLS12_381;

public static partial class Bls12
{
    public static Gt Pairing(in G1Affine p, in G2Affine q)
    {
        var either_identity = p.IsIdentity | q.IsIdentity;
        var p2 = ConditionalSelect(in p, in G1Affine.Generator, either_identity);
        var q2 = ConditionalSelect(in q, in G2Affine.Generator, either_identity);

        var adder = new Adder(p2, q2);

        var tmp = MillerLoop<Fp12, Adder>(adder);
        var tmp2 = new MillerLoopResult(ConditionalSelect(in tmp, in Fp12.One, either_identity));
        return tmp2.FinalExponentiation();
    }
}
