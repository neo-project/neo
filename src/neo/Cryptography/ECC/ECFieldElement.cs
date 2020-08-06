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

            if (!(obj is ECFieldElement other))
                return false;

            return Equals(other);
        }

        public bool Equals(ECFieldElement other)
        {
            return Value.Equals(other.Value) && curve.Equals(other.curve);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public ECFieldElement Sqrt()
        {
            if (curve.Q.TestBit(1))
            {
                ECFieldElement z = new ECFieldElement(BigInteger.ModPow(Value, (curve.Q >> 2) + 1, curve.Q), curve);
                return z.Square().Equals(this) ? z : null;
            }

            throw new NotImplementedException();
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
