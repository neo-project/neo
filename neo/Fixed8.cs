using Neo.IO;
using System;
using System.Globalization;
using System.IO;

namespace Neo
{
    /// <summary>
    /// Accurate to 10^-8 64-bit fixed-point numbers minimize rounding errors.
    /// By controlling the accuracy of the multiplier, rounding errors can be completely eliminated.
    /// </summary>
    public struct Fixed8 : IComparable<Fixed8>, IEquatable<Fixed8>, IFormattable, ISerializable
    {
        private const long D = 100_000_000;
        internal long value;

        public static readonly Fixed8 MaxValue = new Fixed8 { value = long.MaxValue };

        public static readonly Fixed8 MinValue = new Fixed8 { value = long.MinValue };

        public static readonly Fixed8 One = new Fixed8 { value = D };

        public static readonly Fixed8 Satoshi = new Fixed8 { value = 1 };

        public static readonly Fixed8 Zero = default(Fixed8);

        public int Size => sizeof(long);

        public Fixed8(long data)
        {
            this.value = data;
        }

        public Fixed8 Abs()
        {
            if (value >= 0) return this;
            return new Fixed8
            {
                value = -value
            };
        }

        public Fixed8 Ceiling()
        {
            long remainder = value % D;
            if (remainder == 0) return this;
            if (remainder > 0)
                return new Fixed8
                {
                    value = value - remainder + D
                };
            else
                return new Fixed8
                {
                    value = value - remainder
                };
        }

        public int CompareTo(Fixed8 other)
        {
            return value.CompareTo(other.value);
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            value = reader.ReadInt64();
        }

        public bool Equals(Fixed8 other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fixed8)) return false;
            return Equals((Fixed8)obj);
        }

        public static Fixed8 FromDecimal(decimal value)
        {
            value *= D;
            if (value < long.MinValue || value > long.MaxValue)
                throw new OverflowException();
            return new Fixed8
            {
                value = (long)value
            };
        }

        public long GetData() => value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static Fixed8 Max(Fixed8 first, params Fixed8[] others)
        {
            foreach (Fixed8 other in others)
            {
                if (first.CompareTo(other) < 0)
                    first = other;
            }
            return first;
        }

        public static Fixed8 Min(Fixed8 first, params Fixed8[] others)
        {
            foreach (Fixed8 other in others)
            {
                if (first.CompareTo(other) > 0)
                    first = other;
            }
            return first;
        }

        public static Fixed8 Parse(string s)
        {
            return FromDecimal(decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public override string ToString()
        {
            return ((decimal)this).ToString(CultureInfo.InvariantCulture);
        }

        public string ToString(string format)
        {
            return ((decimal)this).ToString(format);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ((decimal)this).ToString(format, formatProvider);
        }

        public static bool TryParse(string s, out Fixed8 result)
        {
            decimal d;
            if (!decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                result = default(Fixed8);
                return false;
            }
            d *= D;
            if (d < long.MinValue || d > long.MaxValue)
            {
                result = default(Fixed8);
                return false;
            }
            result = new Fixed8
            {
                value = (long)d
            };
            return true;
        }

        public static explicit operator decimal(Fixed8 value)
        {
            return value.value / (decimal)D;
        }

        public static explicit operator long(Fixed8 value)
        {
            return value.value / D;
        }

        public static bool operator ==(Fixed8 x, Fixed8 y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Fixed8 x, Fixed8 y)
        {
            return !x.Equals(y);
        }

        public static bool operator >(Fixed8 x, Fixed8 y)
        {
            return x.CompareTo(y) > 0;
        }

        public static bool operator <(Fixed8 x, Fixed8 y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator >=(Fixed8 x, Fixed8 y)
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool operator <=(Fixed8 x, Fixed8 y)
        {
            return x.CompareTo(y) <= 0;
        }

        public static Fixed8 operator *(Fixed8 x, Fixed8 y)
        {
            const ulong QUO = (1ul << 63) / (D >> 1);
            const ulong REM = ((1ul << 63) % (D >> 1)) << 1;
            int sign = Math.Sign(x.value) * Math.Sign(y.value);
            ulong ux = (ulong)Math.Abs(x.value);
            ulong uy = (ulong)Math.Abs(y.value);
            ulong xh = ux >> 32;
            ulong xl = ux & 0x00000000fffffffful;
            ulong yh = uy >> 32;
            ulong yl = uy & 0x00000000fffffffful;
            ulong rh = xh * yh;
            ulong rm = xh * yl + xl * yh;
            ulong rl = xl * yl;
            ulong rmh = rm >> 32;
            ulong rml = rm << 32;
            rh += rmh;
            rl += rml;
            if (rl < rml)
                ++rh;
            if (rh >= D)
                throw new OverflowException();
            ulong rd = rh * REM + rl;
            if (rd < rl)
                ++rh;
            ulong r = rh * QUO + rd / D;
            x.value = (long)r * sign;
            return x;
        }

        public static Fixed8 operator *(Fixed8 x, long y)
        {
            x.value *= y;
            return x;
        }

        public static Fixed8 operator /(Fixed8 x, long y)
        {
            x.value /= y;
            return x;
        }

        public static Fixed8 operator +(Fixed8 x, Fixed8 y)
        {
            x.value = checked(x.value + y.value);
            return x;
        }

        public static Fixed8 operator -(Fixed8 x, Fixed8 y)
        {
            x.value = checked(x.value - y.value);
            return x;
        }

        public static Fixed8 operator -(Fixed8 value)
        {
            value.value = -value.value;
            return value;
        }
    }
}
