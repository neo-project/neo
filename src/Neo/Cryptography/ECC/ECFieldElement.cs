// Copyright (C) 2015-2025 The Neo Project.
//
// ECFieldElement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using System;
using System.Numerics;

namespace Neo.Cryptography.ECC
{
    internal class ECFieldElement : IComparable<ECFieldElement>, IEquatable<ECFieldElement>
    {
        internal readonly BigInteger Value;
        private readonly ECCurve _curve;

        public ECFieldElement(BigInteger value, ECCurve curve)
        {
            if (value >= curve.Q)
                throw new ArgumentException("x value too large in field element");
            Value = value;
            _curve = curve;
        }

        public int CompareTo(ECFieldElement? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (!_curve.Equals(other._curve)) throw new InvalidOperationException("Invalid comparision for points with different curves");
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj == this)
                return true;

            if (obj is not ECFieldElement other)
                return false;

            return Equals(other);
        }

        public bool Equals(ECFieldElement? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            return Value.Equals(other.Value) && _curve.Equals(other._curve);
        }

        private static BigInteger[] FastLucasSequence(BigInteger p, BigInteger P, BigInteger Q, BigInteger k)
        {
            var n = (int)VM.Utility.GetBitLength(k);
            var s = k.GetLowestSetBit();

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

            for (var j = 1; j <= s; ++j)
            {
                Uh = Uh * Vl * p;
                Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                Ql = (Ql * Ql).Mod(p);
            }

            return [Uh, Vl];
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public ECFieldElement? Sqrt()
        {
            if (_curve.Q.TestBit(1))
            {
                ECFieldElement z = new(BigInteger.ModPow(Value, (_curve.Q >> 2) + 1, _curve.Q), _curve);
                return z.Square().Equals(this) ? z : null;
            }
            BigInteger qMinusOne = _curve.Q - 1;
            BigInteger legendreExponent = qMinusOne >> 1;
            if (BigInteger.ModPow(Value, legendreExponent, _curve.Q) != 1)
                return null;
            BigInteger u = qMinusOne >> 2;
            BigInteger k = (u << 1) + 1;
            BigInteger Q = Value;
            BigInteger fourQ = (Q << 2).Mod(_curve.Q);
            BigInteger U, V;
            do
            {
                Random rand = new();
                BigInteger P;
                do
                {
                    P = rand.NextBigInteger((int)VM.Utility.GetBitLength(_curve.Q));
                }
                while (P >= _curve.Q || BigInteger.ModPow(P * P - fourQ, legendreExponent, _curve.Q) != qMinusOne);
                BigInteger[] result = FastLucasSequence(_curve.Q, P, Q, k);
                U = result[0];
                V = result[1];
                if ((V * V).Mod(_curve.Q) == fourQ)
                {
                    if (V.TestBit(0))
                    {
                        V += _curve.Q;
                    }
                    V >>= 1;
                    return new ECFieldElement(V, _curve);
                }
            }
            while (U.Equals(BigInteger.One) || U.Equals(qMinusOne));
            return null;
        }

        public ECFieldElement Square()
        {
            return new ECFieldElement((Value * Value).Mod(_curve.Q), _curve);
        }

        public byte[] ToByteArray()
        {
            var data = Value.ToByteArray(isUnsigned: true, isBigEndian: true);
            if (data.Length == 32)
                return data;
            var buffer = new byte[32];
            Buffer.BlockCopy(data, 0, buffer, buffer.Length - data.Length, data.Length);
            return buffer;
        }

        public static ECFieldElement operator -(ECFieldElement x)
        {
            return new ECFieldElement((-x.Value).Mod(x._curve.Q), x._curve);
        }

        public static ECFieldElement operator *(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value * y.Value).Mod(x._curve.Q), x._curve);
        }

        public static ECFieldElement operator /(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value * y.Value.ModInverse(x._curve.Q)).Mod(x._curve.Q), x._curve);
        }

        public static ECFieldElement operator +(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value + y.Value).Mod(x._curve.Q), x._curve);
        }

        public static ECFieldElement operator -(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value - y.Value).Mod(x._curve.Q), x._curve);
        }
    }
}

#nullable disable
