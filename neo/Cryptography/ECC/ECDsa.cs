using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Neo.Cryptography.ECC
{
    public class ECDsa
    {
        private readonly byte[] privateKey;
        private readonly ECPoint publicKey;
        private readonly ECCurve curve;

        public ECDsa(byte[] privateKey, ECCurve curve)
            : this(curve.G * privateKey)
        {
            this.privateKey = privateKey;
        }

        public ECDsa(ECPoint publicKey)
        {
            this.publicKey = publicKey;
            this.curve = publicKey.Curve;
        }

        private BigInteger CalculateE(BigInteger n, byte[] message)
        {
            int messageBitLength = message.Length * 8;
            BigInteger trunc = new BigInteger(message.Reverse().Concat(new byte[1]).ToArray());
            if (n.GetBitLength() < messageBitLength)
            {
                trunc >>= messageBitLength - n.GetBitLength();
            }
            return trunc;
        }

        public BigInteger[] GenerateSignature(byte[] message)
        {
            if (privateKey == null) throw new InvalidOperationException();
            BigInteger e = CalculateE(curve.N, message);
            BigInteger d = new BigInteger(privateKey.Reverse().Concat(new byte[1]).ToArray());
            BigInteger r, s;
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                do
                {
                    BigInteger k;
                    do
                    {
                        do
                        {
                            k = rng.NextBigInteger(curve.N.GetBitLength());
                        }
                        while (k.Sign == 0 || k.CompareTo(curve.N) >= 0);
                        ECPoint p = ECPoint.Multiply(curve.G, k);
                        BigInteger x = p.X.Value;
                        r = x.Mod(curve.N);
                    }
                    while (r.Sign == 0);
                    s = (k.ModInverse(curve.N) * (e + d * r)).Mod(curve.N);
                    if (s > curve.N / 2)
                    {
                        s = curve.N - s;
                    }
                }
                while (s.Sign == 0);
            }
            return new BigInteger[] { r, s };
        }

        private static ECPoint SumOfTwoMultiplies(ECPoint P, BigInteger k, ECPoint Q, BigInteger l)
        {
            int m = Math.Max(k.GetBitLength(), l.GetBitLength());
            ECPoint Z = P + Q;
            ECPoint R = P.Curve.Infinity;
            for (int i = m - 1; i >= 0; --i)
            {
                R = R.Twice();
                if (k.TestBit(i))
                {
                    if (l.TestBit(i))
                        R = R + Z;
                    else
                        R = R + P;
                }
                else
                {
                    if (l.TestBit(i))
                        R = R + Q;
                }
            }
            return R;
        }

        public bool VerifySignature(byte[] message, BigInteger r, BigInteger s)
        {
            if (r.Sign < 1 || s.Sign < 1 || r.CompareTo(curve.N) >= 0 || s.CompareTo(curve.N) >= 0)
                return false;
            BigInteger e = CalculateE(curve.N, message);
            BigInteger c = s.ModInverse(curve.N);
            BigInteger u1 = (e * c).Mod(curve.N);
            BigInteger u2 = (r * c).Mod(curve.N);
            ECPoint point = SumOfTwoMultiplies(curve.G, u1, publicKey, u2);
            BigInteger v = point.X.Value.Mod(curve.N);
            return v.Equals(r);
        }

        public static ECPoint RecoverPubKeyFromSignature(ECCurve curve, BigInteger r, BigInteger s, byte[] msg, bool doChecks)
        {
            // calculate h
            BigInteger h = (curve.Q + 1 + 2 * (BigInteger)Math.Sqrt((double)curve.Q)) / curve.N;
            ECPoint Q = new ECPoint();
            ECPoint W = new ECPoint();
            for (int j = 0; j <= h; j++)
            {
                // step 1.1 x = (n * i) + r
                BigInteger Rx = curve.N * j + r;
                if (Rx > curve.Q) break;

                // step 1.2 and 1.3 get point R
                ECPoint R0 = ECPoint.DecompressPoint(0, Rx, curve);
                ECPoint R1 = ECPoint.DecompressPoint(1, Rx, curve);
                if (doChecks)
                {
                    if (ECPoint.Multiply(R0, curve.N) != curve.Infinity)
                        continue;
                }

                // step 1.5 compute e
                int messageBitLength = msg.Length * 8;
                BigInteger e = new BigInteger(msg.Reverse().Concat(new byte[1]).ToArray());
                if (curve.N.GetBitLength() < messageBitLength)
                {
                    e >>= messageBitLength - curve.N.GetBitLength();
                }

                // step 1.6 Q = r^-1 (sR-eG)
                BigInteger invr = r.ModInverse(curve.N);
                ECPoint t0 = ECPoint.Multiply(R0, s) - ECPoint.Multiply(curve.G, e);
                ECPoint t1 = ECPoint.Multiply(R1, s) - ECPoint.Multiply(curve.G, e);
                Q = ECPoint.Multiply(t0, invr);
                W = ECPoint.Multiply(t1, invr);
            }
            var test = W;
            return Q;
        }

        public static ECPoint KeyRecover(ECCurve curve, BigInteger r, BigInteger s, byte[] msg, bool IsEven, bool doChecks)
        {
            // calculate h
            BigInteger h = (curve.Q + 1 + 2 * (BigInteger)Math.Sqrt((double)curve.Q)) / curve.N;
            ECPoint Q = new ECPoint();

            for (int j = 0; j <= h; j++)
            {
                // step 1.1 x = (n * i) + r
                BigInteger Rx = curve.N * j + r;
                if (Rx > curve.Q) break;

                // step 1.2 and 1.3 get point R
                ECPoint R;
                if (IsEven)
                {
                    R = ECPoint.DecompressPoint(0, Rx, curve);
                }
                else
                {
                    R = ECPoint.DecompressPoint(1, Rx, curve);
                }
                if (doChecks)
                {
                    if (ECPoint.Multiply(R, curve.N) != curve.Infinity)
                        continue;
                }

                // step 1.5 compute e
                int messageBitLength = msg.Length * 8;
                BigInteger e = new BigInteger(msg.Reverse().Concat(new byte[1]).ToArray());
                if (curve.N.GetBitLength() < messageBitLength)
                {
                    e >>= messageBitLength - curve.N.GetBitLength();
                }

                // step 1.6 Q = r^-1 (sR-eG)
                BigInteger invr = r.ModInverse(curve.N);
                ECPoint t0 = ECPoint.Multiply(R, s) - ECPoint.Multiply(curve.G, e);
                Q = ECPoint.Multiply(t0, invr);
            }
            return Q;
        }
    }
}
