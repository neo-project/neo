// Copyright (C) 2015-2025 The Neo Project.
//
// Bls12.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.MillerLoopUtility;

namespace Neo.Cryptography.BLS12_381
{
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
}
