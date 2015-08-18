using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AntShares.Algebra
{
    /// <summary>
    /// Finds the y-intercept of a polynomial in a finite field.
    /// </summary>
    public class LagrangeInterpolator
    {
        public static FiniteFieldPolynomial EvaluateAtZero(IEnumerable<FiniteFieldPoint> points)
        {
            var originalPoints = points.ToArray();

            if (originalPoints.Length == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int threshold = originalPoints.Length;
            // We need to "correct" these points by removing the high term monomial
            var adjustedPoints = originalPoints.Select(p => AdjustPoint(threshold, p)).ToArray();

            // Use Lagrange interpolating polynomials ( http://en.wikipedia.org/wiki/Lagrange_polynomial )
            // to solve:

            //        (x-x2)(x-x3)...(x-xn)         (x-x1)(x-x3)...(x-xn)
            // P(x) = ------------------------ y1 + -------------------------- y2 + ... + 
            //        (x1-x2)(x1-x3)...(x1-xn)      (x2-x1)(x2-x3)...(x2-xn-1)

            // Simplifying things is that x is 0 since we want to find the constant term            

            var fieldPoly = originalPoints[0].Y;

            var total = fieldPoly.Zero;

            for (int ixCurrentPoint = 0; ixCurrentPoint < threshold; ixCurrentPoint++)
            {
                var currentNumerator = fieldPoly.One;
                var currentDenominator = fieldPoly.One;
                var currentPoint = adjustedPoints[ixCurrentPoint];

                for (int ixOtherPoint = 0; ixOtherPoint < threshold; ixOtherPoint++)
                {
                    if (ixCurrentPoint == ixOtherPoint)
                    {
                        continue;
                    }

                    // numerator needs multiplied by 
                    // (0-x_i) = -x_i = x_i 
                    // (since subtraction and addition are the same in GF[2]
                    currentNumerator *= adjustedPoints[ixOtherPoint].X;
                    currentDenominator *= (currentPoint.X + adjustedPoints[ixOtherPoint].X);
                }

                // Dividing is just multiplying by the inverse
                var denominatorInverse = currentDenominator.GetInverse();

                var fraction = currentNumerator * denominatorInverse;

                // Now, multiply the fraction by the relevant y_i
                var currentTermValue = fraction * currentPoint.Y;
                total += currentTermValue;
            }

            return total;
        }

        private static FiniteFieldPoint AdjustPoint(int totalPoints, FiniteFieldPoint point)
        {
            var correction = new FiniteFieldPolynomial(point.Y.PrimePolynomial, BigInteger.One);
            var correctionMultiplier = point.X;

            for (int i = 1; i <= totalPoints; i++)
            {
                correction = correction * correctionMultiplier;
            }

            var newY = point.Y + correction;
            return new FiniteFieldPoint(point.X, newY);
        }
    }
}
