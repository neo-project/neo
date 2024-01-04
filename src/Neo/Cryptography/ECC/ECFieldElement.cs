// Copyright (C) 2015-2024 The Neo Project.
//
// ECFieldElement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using System;
using System.Numerics;

namespace Neo.Cryptography.ECC
{
    internal class ECFieldElement : IComparable<ECFieldElement>, IEquatable<ECFieldElement>
    {
        internal readonly BigInteger Value;
        private readonly ECCurve curve;

        public ECFieldElement(BigInteger value, ECCurve curve)
        {
            if (curve is null)
                throw new ArgumentNullException(nameof(curve));
            if (value >= curve.Q)
                throw new ArgumentException("x value too large in field element");
            this.Value = value;
            this.curve = curve;
        }

        public int CompareTo(ECFieldElement other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (!curve.Equals(other.curve)) throw new InvalidOperationException("Invalid comparision for points with different curves");
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            if (obj is not ECFieldElement other)
                return false;

            return Equals(other);
        }

        public bool Equals(ECFieldElement other)
        {
            return Value.Equals(other.Value) && curve.Equals(other.curve);
        }

        private static BigInteger[] FastLucasSequence(BigInteger p, BigInteger P, BigInteger Q, BigInteger k)
        {
            int n = (int)k.GetBitLength();
            int s = k.GetLowestSetBit();

            BigInteger Uh = 1;
            BigInteger Vl = 2;
            BigInteger Vh = P;
            BigInteger Ql = 1;
            BigInteger Qh = 1;

            for (int j = n - 1; j >= s + 1; --j)
            {
                Ql = (Ql * Qh).Mod(p);

                if (k.TestBit(j))
                {
                    Qh = (Ql * Q).Mod(p);
                    Uh = (Uh * Vh).Mod(p);
                    Vl = (Vh * Vl - P * Ql).Mod(p);
                    Vh = ((Vh * Vh) - (Qh << 1)).Mod(p);
                }
                else
                {
                    Qh = Ql;
                    Uh = (Uh * Vl - Ql).Mod(p);
                    Vh = (Vh * Vl - P * Ql).Mod(p);
                    Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                }
            }

            Ql = (Ql * Qh).Mod(p);
            Qh = (Ql * Q).Mod(p);
            Uh = (Uh * Vl - Ql).Mod(p);
            Vl = (Vh * Vl - P * Ql).Mod(p);
            Ql = (Ql * Qh).Mod(p);

            for (int j = 1; j <= s; ++j)
            {
                Uh = Uh * Vl * p;
                Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                Ql = (Ql * Ql).Mod(p);
            }

            return new BigInteger[] { Uh, Vl };
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public ECFieldElement Sqrt()
        {
            if (curve.Q.TestBit(1))
            {
                ECFieldElement z = new(BigInteger.ModPow(Value, (curve.Q >> 2) + 1, curve.Q), curve);
                return z.Square().Equals(this) ? z : null;
            }
            BigInteger qMinusOne = curve.Q - 1;
            BigInteger legendreExponent = qMinusOne >> 1;
            if (BigInteger.ModPow(Value, legendreExponent, curve.Q) != 1)
                return null;
            BigInteger u = qMinusOne >> 2;
            BigInteger k = (u << 1) + 1;
            BigInteger Q = this.Value;
            BigInteger fourQ = (Q << 2).Mod(curve.Q);
            BigInteger U, V;
            do
            {
                Random rand = new();
                BigInteger P;
                do
                {
                    P = rand.NextBigInteger((int)curve.Q.GetBitLength());
                }
                while (P >= curve.Q || BigInteger.ModPow(P * P - fourQ, legendreExponent, curve.Q) != qMinusOne);
                BigInteger[] result = FastLucasSequence(curve.Q, P, Q, k);
                U = result[0];
                V = result[1];
                if ((V * V).Mod(curve.Q) == fourQ)
                {
                    if (V.TestBit(0))
                    {
                        V += curve.Q;
                    }
                    V >>= 1;
                    return new ECFieldElement(V, curve);
                }
            }
            while (U.Equals(BigInteger.One) || U.Equals(qMinusOne));
            return null;
        }

        public ECFieldElement Square()
        {
            return new ECFieldElement((Value * Value).Mod(curve.Q), curve);
        }

        public byte[] ToByteArray()
        {
            byte[] data = Value.ToByteArray(isUnsigned: true, isBigEndian: true);
            if (data.Length == 32)
                return data;
            byte[] buffer = new byte[32];
            Buffer.BlockCopy(data, 0, buffer, buffer.Length - data.Length, data.Length);
            return buffer;
        }

        public static ECFieldElement operator -(ECFieldElement x)
        {
            return new ECFieldElement((-x.Value).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator *(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value * y.Value).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator /(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value * y.Value.ModInverse(x.curve.Q)).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator +(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value + y.Value).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator -(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value - y.Value).Mod(x.curve.Q), x.curve);
        }
    }
}
