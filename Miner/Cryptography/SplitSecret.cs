using AntShares.Algebra;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AntShares.Cryptography
{
    public class SplitSecret
    {
        private readonly IrreduciblePolynomial _IrreduciblePolynomial;
        private readonly FiniteFieldPolynomial[] _AllCoefficients;

        public int Threshold { get; private set; }

        public SplitSecret(int threshold, IrreduciblePolynomial irreduciblePolynomial, FiniteFieldPolynomial[] allCoefficients)
        {
            Threshold = threshold;
            _IrreduciblePolynomial = irreduciblePolynomial;
            _AllCoefficients = allCoefficients;
        }

        public FiniteFieldPoint GetShare(int n)
        {
            var xPoly = new FiniteFieldPolynomial(_IrreduciblePolynomial, new BigInteger(n));
            var y = FiniteFieldPolynomial.EvaluateAt(n, _AllCoefficients);
            return new FiniteFieldPoint(xPoly, y);
        }

        public IEnumerable<FiniteFieldPoint> GetShares(int totalShares)
        {
            return Enumerable.Range(1, totalShares).Select(GetShare);
        }
    }
}
