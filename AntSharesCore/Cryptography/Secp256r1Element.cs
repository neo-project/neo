using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace AntShares.Cryptography
{
    internal class Secp256r1Element : IComparable<Secp256r1Element>, IEquatable<Secp256r1Element>
    {
        internal readonly BigInteger Value;

        public Secp256r1Element(BigInteger value)
        {
            if (value >= Secp256r1Curve.Q)
                throw new ArgumentException("x value too large in field element");
            this.Value = value;
        }

        public int CompareTo(Secp256r1Element other)
        {
            if (ReferenceEquals(this, other)) return 0;
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            Secp256r1Element other = obj as Secp256r1Element;

            if (other == null)
                return false;

            return Equals(other);
        }

        public bool Equals(Secp256r1Element other)
        {
            return Value.Equals(other.Value);
        }

        private static BigInteger[] FastLucasSequence(BigInteger p, BigInteger P, BigInteger Q, BigInteger k)
        {
            int n = k.GetBitLength();
            int s = k.GetLowestSetBit();

            Debug.Assert(k.TestBit(s));

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

        public Secp256r1Element Sqrt()
        {
            if (Secp256r1Curve.Q.TestBit(1))
            {
                Secp256r1Element z = new Secp256r1Element(BigInteger.ModPow(Value, (Secp256r1Curve.Q >> 2) + 1, Secp256r1Curve.Q));
                return z.Square().Equals(this) ? z : null;
            }
            BigInteger qMinusOne = Secp256r1Curve.Q - 1;
            BigInteger legendreExponent = qMinusOne >> 1;
            if (BigInteger.ModPow(Value, legendreExponent, Secp256r1Curve.Q) != 1)
                return null;
            BigInteger u = qMinusOne >> 2;
            BigInteger k = (u << 1) + 1;
            BigInteger Q = this.Value;
            BigInteger fourQ = (Q << 2).Mod(Secp256r1Curve.Q);
            BigInteger U, V;
            do
            {
                Random rand = new Random();
                BigInteger P;
                do
                {
                    P = rand.NextBigInteger(Secp256r1Curve.Q.GetBitLength());
                }
                while (P >= Secp256r1Curve.Q || BigInteger.ModPow(P * P - fourQ, legendreExponent, Secp256r1Curve.Q) != qMinusOne);
                BigInteger[] result = FastLucasSequence(Secp256r1Curve.Q, P, Q, k);
                U = result[0];
                V = result[1];
                if ((V * V).Mod(Secp256r1Curve.Q) == fourQ)
                {
                    if (V.TestBit(0))
                    {
                        V += Secp256r1Curve.Q;
                    }
                    V >>= 1;
                    Debug.Assert((V * V).Mod(Secp256r1Curve.Q) == Value);
                    return new Secp256r1Element(V);
                }
            }
            while (U.Equals(BigInteger.One) || U.Equals(qMinusOne));
            return null;
        }

        public Secp256r1Element Square()
        {
            return new Secp256r1Element((Value * Value).Mod(Secp256r1Curve.Q));
        }

        public byte[] ToByteArray()
        {
            byte[] data = Value.ToByteArray();
            if (data.Length == 32)
                return data.Reverse().ToArray();
            if (data.Length > 32)
                return data.Take(32).Reverse().ToArray();
            return Enumerable.Repeat<byte>(0, 32 - data.Length).Concat(data.Reverse()).ToArray();
        }

        public static Secp256r1Element operator -(Secp256r1Element x)
        {
            return new Secp256r1Element((-x.Value).Mod(Secp256r1Curve.Q));
        }

        public static Secp256r1Element operator *(Secp256r1Element x, Secp256r1Element y)
        {
            return new Secp256r1Element((x.Value * y.Value).Mod(Secp256r1Curve.Q));
        }

        public static Secp256r1Element operator /(Secp256r1Element x, Secp256r1Element y)
        {
            return new Secp256r1Element((x.Value * BigInteger.ModPow(y.Value, Secp256r1Curve.Q - 2, Secp256r1Curve.Q)).Mod(Secp256r1Curve.Q));
        }

        public static Secp256r1Element operator +(Secp256r1Element x, Secp256r1Element y)
        {
            return new Secp256r1Element((x.Value + y.Value).Mod(Secp256r1Curve.Q));
        }

        public static Secp256r1Element operator -(Secp256r1Element x, Secp256r1Element y)
        {
            return new Secp256r1Element((x.Value - y.Value).Mod(Secp256r1Curve.Q));
        }
    }
}
