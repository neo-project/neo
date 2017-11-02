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

        public BigDecimal(BigInteger value, byte decimals)
        {
            this.value = value;
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
            byte ds = decimals;
            int index = s.IndexOf('.');
            if (index >= 0)
            {
                int d = s.Length - index - 1;
                if (d > decimals) throw new FormatException();
                s = s.Remove(index, 1);
                ds -= (byte)d;
            }
            s += new string('0', ds);
            return new BigDecimal(BigInteger.Parse(s), decimals);
        }

        public Fixed8 ToFixed8()
        {
            try
            {
                return new Fixed8((long)ChangeDecimals(8).value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(ex.Message, ex);
            }
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
            byte ds = decimals;
            int index = s.IndexOf('.');
            if (index >= 0)
            {
                int d = s.Length - index - 1;
                if (d > decimals)
                {
                    result = default(BigDecimal);
                    return false;
                }
                s = s.Remove(index, 1);
                ds -= (byte)d;
            }
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
