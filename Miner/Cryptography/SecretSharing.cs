using AntShares.Algebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Cryptography
{
    public static class SecretSharing
    {
        private static readonly Diffuser DefaultDiffuser = new SsssDiffuser();

        public static byte[] Combine(IEnumerable<FiniteFieldPoint> points)
        {
            var allShares = points.ToArray();

            if (allShares.Length == 0)
            {
                throw new ArgumentException("You must provide at least one secret share (piece).", nameof(points));
            }

            var secretCoefficient = LagrangeInterpolator.EvaluateAtZero(allShares);
            var scrambledValue = secretCoefficient.PolynomialValue;
            var unscrambledValue = DefaultDiffuser.Unscramble(scrambledValue, scrambledValue.ToByteArray().Length);
            return unscrambledValue.ToUnsignedBigEndianBytes();
        }

        public static SplitSecret Split(byte[] secret, int threshold)
        {
            var irreduciblePolynomial = IrreduciblePolynomial.CreateOfByteSize(secret.Length);
            var rawSecret = secret.ToBigIntegerFromBigEndianUnsignedBytes();
            var diffusedSecret = DefaultDiffuser.Scramble(rawSecret, secret.Length);
            var secretCoefficient = new FiniteFieldPolynomial(irreduciblePolynomial, diffusedSecret);

            var allCoefficients = new[] { secretCoefficient }
                .Concat(
                    GetRandomPolynomials(
                        irreduciblePolynomial,
                        threshold - 1)
                )
                .ToArray();

            return new SplitSecret(threshold, irreduciblePolynomial, allCoefficients);
        }

        private static IEnumerable<FiniteFieldPolynomial> GetRandomPolynomials(IrreduciblePolynomial irreduciblePolynomial, int total)
        {
            var rng = RandomNumberGenerator.Create();

            for (int i = 0; i < total; i++)
            {
                var randomCoefficientBytes = new byte[irreduciblePolynomial.SizeInBytes];
                rng.GetBytes(randomCoefficientBytes);
                yield return new FiniteFieldPolynomial(irreduciblePolynomial, randomCoefficientBytes.ToBigIntegerFromLittleEndianUnsignedBytes());
            }
        }
    }
}
