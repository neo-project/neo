// Copyright (C) 2015-2024 The Neo Project.
//
// G2Prepared.cs file belongs to the neo project and is free
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
}
