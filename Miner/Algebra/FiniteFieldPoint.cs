using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace AntShares.Algebra
{
    /// <summary>
    /// Represents a single 2D point using finite field polynomials.
    /// </summary>
    public class FiniteFieldPoint
    {
        public FiniteFieldPoint(FiniteFieldPolynomial x, FiniteFieldPolynomial y)
        {
            X = x;
            Y = y;
        }

        public FiniteFieldPolynomial X { get; private set; }
        public FiniteFieldPolynomial Y { get; private set; }

        public string ToString(int totalPoints)
        {
            var sb = new StringBuilder();
            // How many decimal digits do the total points take up?

            for (int remainder = totalPoints; remainder > 0; remainder /= 10)
            {
                sb.Append('0');
            }

            string format = sb.ToString();

            string shareNumber = ((long)X.PolynomialValue).ToString(format);


            var expectedByteCount = Y.PrimePolynomial.SizeInBytes;
            var pointBytes = Y.PolynomialValue.ToUnsignedBigEndianBytes();

            // Occasionally, the value won't fill all bytes, so we need to prefix with 0's as needed
            var prefixedPointBytes = Enumerable.Range(0, expectedByteCount - pointBytes.Length).Select(ix => (byte)0).Concat(pointBytes);

            // To hex string on its own just wasn't working right
            string shareValue = String.Join("", prefixedPointBytes.Select(b => b.ToString("x2")));
            return shareNumber + "-" + shareValue;
        }

        public override string ToString()
        {
            return ToString((int)X.PolynomialValue);
        }

        internal const string RegexPattern = @"(?<x>[0-9]+)-(?<y>[0-9a-fA-F]+)";

        public static FiniteFieldPoint Parse(string s)
        {
            FiniteFieldPoint result;
            if (!TryParse(s, out result))
            {
                throw new ArgumentException();
            }

            return result;
        }

        public static bool TryParse(string s, out FiniteFieldPoint result)
        {
            var match = Regex.Match(s, RegexPattern);
            return TryParse(match, out result);
        }

        internal static bool TryParse(Match match, out FiniteFieldPoint result)
        {
            if (!match.Success)
            {
                result = null;
                return false;
            }

            try
            {
                var xString = match.Groups["x"].Value.ToLowerInvariant();
                var yString = match.Groups["y"].Value.ToLowerInvariant();

                // get rid of any initial 0's
                while (xString.StartsWith("0", StringComparison.Ordinal))
                {
                    xString = xString.Substring(1);
                }

                // Each hex letter makes up 4 bits, so to get the degree in bits
                // we multiply by 4

                int polynomialDegree = yString.Length * 4;

                var irp = new IrreduciblePolynomial(polynomialDegree);

                var x = new FiniteFieldPolynomial(irp, BigInteger.Parse(xString));

                // get bytes
                var bigEndianBytes = new byte[yString.Length / 2];
                for (int i = 0; i < yString.Length; i += 2)
                {
                    bigEndianBytes[i / 2] = Byte.Parse(yString.Substring(i, 2), NumberStyles.HexNumber);
                }
                var y = new FiniteFieldPolynomial(irp, bigEndianBytes.ToBigIntegerFromBigEndianUnsignedBytes());

                result = new FiniteFieldPoint(x, y);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
