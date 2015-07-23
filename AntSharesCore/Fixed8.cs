using System;

namespace AntShares
{
    public struct Fixed8 : IComparable<Fixed8>, IEquatable<Fixed8>, IFormattable
    {
        private const long D = 100000000;
        internal long value;

        public static readonly Fixed8 One = new Fixed8 { value = D };

        public static readonly Fixed8 Satoshi = new Fixed8 { value = 1 };

        public static readonly Fixed8 Zero = new Fixed8();

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

        public int CompareTo(Fixed8 other)
        {
            return value.CompareTo(other.value);
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

        public static Fixed8 Multiply(Fixed8 amount, Fixed8 price)
        {
            long remainder;
            amount.value = Math.DivRem(amount.value, 1000, out remainder);
            if (remainder != 0) throw new ArgumentException();
            price.value = Math.DivRem(price.value, 100000, out remainder);
            if (remainder != 0) throw new ArgumentException();
            amount.value *= price.value;
            return amount;
        }

        public static Fixed8 Parse(string s)
        {
            return Fixed8.FromDecimal(decimal.Parse(s));
        }

        public override string ToString()
        {
            return (value / (decimal)D).ToString();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return (value / (decimal)D).ToString(format, formatProvider);
        }

        public static bool TryParse(string s, out Fixed8 result)
        {
            decimal d;
            if (!decimal.TryParse(s, out d))
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

        public static Fixed8 operator +(Fixed8 x, Fixed8 y)
        {
            x.value += y.value;
            return x;
        }

        public static Fixed8 operator -(Fixed8 x, Fixed8 y)
        {
            x.value -= y.value;
            return x;
        }

        public static Fixed8 operator -(Fixed8 value)
        {
            value.value = -value.value;
            return value;
        }
    }
}
