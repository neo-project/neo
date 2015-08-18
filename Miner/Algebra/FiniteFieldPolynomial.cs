using System.Numerics;

namespace AntShares.Algebra
{
    /// <summary>
    /// Represents a polynomial modulo an irreducible polynomial (in a finite field).
    /// </summary>
    public class FiniteFieldPolynomial
    {
        private readonly IrreduciblePolynomial _PrimePolynomial;
        private readonly BigInteger _PolynomialSetCoefficients;

        public FiniteFieldPolynomial(IrreduciblePolynomial primePolynomial, BigInteger polynomial)
        {
            _PrimePolynomial = primePolynomial;
            _PolynomialSetCoefficients = polynomial;
        }

        public FiniteFieldPolynomial(IrreduciblePolynomial primePolynomial, params int[] setCoefficients)
        {
            _PrimePolynomial = primePolynomial;

            _PolynomialSetCoefficients = BigInteger.Zero;
            for (int i = 0; i < setCoefficients.Length; i++)
            {
                _PolynomialSetCoefficients = _PolynomialSetCoefficients.SetBit(setCoefficients[i]);
            }
        }

        public FiniteFieldPolynomial Clone()
        {
            return new FiniteFieldPolynomial(_PrimePolynomial, _PolynomialSetCoefficients);
        }

        public BigInteger PolynomialValue
        {
            get { return _PolynomialSetCoefficients; }
        }


        public IrreduciblePolynomial PrimePolynomial
        {
            get { return _PrimePolynomial; }
        }

        public static FiniteFieldPolynomial EvaluateAt(long x, FiniteFieldPolynomial[] coefficients)
        {
            // Use Horner's Scheme: http://en.wikipedia.org/wiki/Horner_scheme

            FiniteFieldPolynomial xAsPoly = coefficients[0].GetValueInField(x);

            // assume the coefficient for highest monomial is 1            
            FiniteFieldPolynomial result = xAsPoly.Clone();

            for (int i = coefficients.Length - 1; i > 0; i--)
            {
                result = result + coefficients[i];
                result = result * xAsPoly;
            }

            result = result + coefficients[0];

            return result;
        }

        public static FiniteFieldPolynomial operator +(FiniteFieldPolynomial left, FiniteFieldPolynomial right)
        {
            BigInteger result = left._PolynomialSetCoefficients ^ right._PolynomialSetCoefficients;
            return new FiniteFieldPolynomial(left._PrimePolynomial, result);
        }

        public static FiniteFieldPolynomial operator *(FiniteFieldPolynomial left, FiniteFieldPolynomial right)
        {
            // Use a modified version of the "peasant's algorithm":
            // http://en.wikipedia.org/wiki/Ancient_Egyptian_multiplication
            // The invariant is that a * b + p must always equal the product. We keep
            // doubling "a" and halving "b". If "b" is odd, then we add "a" to "p"

            BigInteger p = BigInteger.Zero;
            BigInteger a = left._PolynomialSetCoefficients;
            BigInteger b = right._PolynomialSetCoefficients;

            int degree = left._PrimePolynomial.Degree;

            BigInteger mask = (BigInteger.One << degree) - BigInteger.One;

            for (int i = 0; i < degree; i++)
            {
                if ((a == BigInteger.Zero) || (b == BigInteger.Zero))
                {
                    break;
                }

                if (b.TestBit(0))
                {
                    // It's odd, add it
                    p = p ^ a;
                }

                bool highBitSet = a.TestBit(degree - 1);

                // multiply a by "x"

                a = a << 1;
                a = a & mask;

                if (highBitSet)
                {
                    a = a ^ left._PrimePolynomial.PolynomialValue;
                    a = a & mask;
                }

                b = b >> 1;
            }

            p = p & mask;

            return new FiniteFieldPolynomial(left._PrimePolynomial, p);
        }

        public FiniteFieldPolynomial GetInverse()
        {
            // We need to compute the inverse of the current polynomial
            // modulo the irreducible polynomial. We'll do this with 
            // a simplified version of the Euclidean algorithm:
            // http://en.wikipedia.org/wiki/Extended_Euclidean_algorithm#Computing_a_multiplicative_inverse_in_a_finite_field
            // Instead of allowing division by arbitrary polynomials such as
            // x^4 + x^3 + 1, we'll always divide by monomials like x^4.
            // This makes multiplication and division by using just shifts.

            BigInteger r_minus2 = _PrimePolynomial.PolynomialValue;
            BigInteger r_minus1 = _PolynomialSetCoefficients;
            BigInteger a_minus2 = BigInteger.Zero;
            BigInteger a_minus1 = BigInteger.One;

            while (!r_minus1.Equals(BigInteger.One))
            {
                // How much do I need to shift by?
                int shiftAmount = r_minus2.GetBitLength() - r_minus1.GetBitLength();

                if (shiftAmount < 0)
                {
                    Swap(ref r_minus2, ref r_minus1);
                    Swap(ref a_minus2, ref a_minus1);
                    shiftAmount = -shiftAmount;
                }

                // Now r_minus2 should be as big or bigger than r_minus1
                // q = BigInteger.One.ShiftLeft(shiftAmount)
                BigInteger r_minus1TimesQ = r_minus1 << shiftAmount;
                BigInteger r_new = r_minus1TimesQ ^ r_minus2;

                BigInteger a_new = (a_minus1 << shiftAmount) ^ a_minus2;

                r_minus2 = r_minus1;
                r_minus1 = r_new;
                a_minus2 = a_minus1;
                a_minus1 = a_new;
            }

            return new FiniteFieldPolynomial(_PrimePolynomial, a_minus1);
        }

        public FiniteFieldPolynomial Zero
        {
            get { return GetValueInField(0); }
        }

        public FiniteFieldPolynomial One
        {
            get { return GetValueInField(1); }
        }

        private FiniteFieldPolynomial GetValueInField(long n)
        {
            return new FiniteFieldPolynomial(_PrimePolynomial, new BigInteger(n));
        }

        public override string ToString()
        {
            return _PolynomialSetCoefficients.ToPolynomialString();
        }

        private static void Swap(ref BigInteger a, ref BigInteger b)
        {
            var temp = b;
            b = a;
            a = temp;
        }

        public override bool Equals(object obj)
        {
            var other = obj as FiniteFieldPolynomial;
            if (other == null)
            {
                return base.Equals(obj);
            }

            return (PrimePolynomial == other.PrimePolynomial)
                   &&
                   (PolynomialValue.Equals(other.PolynomialValue));
        }

        public override int GetHashCode()
        {
            return PrimePolynomial.GetHashCode() ^ PolynomialValue.GetHashCode();
        }
    }
}
