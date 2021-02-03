using System;
using System.Numerics;

namespace Neo
{
    public struct BigDecimal
    {
        private readonly BigInteger value;
        private readonly byte decimals;

        public BigInteger Value => value;
        public byte Decimals => decimals;
        public int Sign => value.Sign;

        /// <summary>
        /// Create BigDecimal from BigInteger
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="decimals">Decimals</param>
        public BigDecimal(BigInteger value, byte decimals)
        {
            this.value = value;
            this.decimals = decimals;
        }

        /// <summary>
        /// Create BigDecimal from decimal
        /// </summary>
        /// <param name="value">Value</param>
        public unsafe BigDecimal(decimal value)
        {
            ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(&value, sizeof(decimal));
            this.decimals = buffer[14];
            this.value = new BigInteger(decimal.Multiply((decimal)Math.Pow(10, decimals), value));
        }

        /// <summary>
        /// Create BigDecimal from decimal
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="decimals">Decimals</param>
        public unsafe BigDecimal(decimal value, byte decimals)
        {
            ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(&value, sizeof(decimal));
            if (buffer[14] > decimals) throw new ArgumentException(null, nameof(value));
            this.value = new BigInteger(decimal.Multiply((decimal)Math.Pow(10, decimals), value));
            this.decimals = decimals;
        }

        public BigDecimal ChangeDecimals(byte decimals)
        {
            if (this.decimals == decimals) return this;
            BigInteger value;
            if (this.decimals < decimals)
            {
                value = this.value * BigInteger.Pow(10, decimals - this.decimals);
            }
            else
            {
                BigInteger divisor = BigInteger.Pow(10, this.decimals - decimals);
                value = BigInteger.DivRem(this.value, divisor, out BigInteger remainder);
                if (remainder > BigInteger.Zero)
                    throw new ArgumentOutOfRangeException();
            }
            return new BigDecimal(value, decimals);
        }

        public static BigDecimal Parse(string s, byte decimals)
        {
            if (!TryParse(s, decimals, out BigDecimal result))
                throw new FormatException();
            return result;
        }

        public override string ToString()
        {
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger result = BigInteger.DivRem(value, divisor, out BigInteger remainder);
            if (remainder == 0) return result.ToString();
            return $"{result}.{remainder.ToString("d" + decimals)}".TrimEnd('0');
        }

        public static bool TryParse(string s, byte decimals, out BigDecimal result)
        {
            int e = 0;
            int index = s.IndexOfAny(new[] { 'e', 'E' });
            if (index >= 0)
            {
                if (!sbyte.TryParse(s.Substring(index + 1), out sbyte e_temp))
                {
                    result = default(BigDecimal);
                    return false;
                }
                e = e_temp;
                s = s.Substring(0, index);
            }
            index = s.IndexOf('.');
            if (index >= 0)
            {
                s = s.TrimEnd('0');
                e -= s.Length - index - 1;
                s = s.Remove(index, 1);
            }
            int ds = e + decimals;
            if (ds < 0)
            {
                result = default(BigDecimal);
                return false;
            }
            if (ds > 0)
                s += new string('0', ds);
            if (!BigInteger.TryParse(s, out BigInteger value))
            {
                result = default(BigDecimal);
                return false;
            }
            result = new BigDecimal(value, decimals);
            return true;
        }
    }
}
