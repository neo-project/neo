using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Neo.Cryptography.ECC
{
    /// <summary>
    /// 提供椭圆曲线数字签名算法（ECDSA）的功能
    /// </summary>
    public class ECDsa
    {
        private readonly byte[] privateKey;
        private readonly ECPoint publicKey;
        private readonly ECCurve curve;

        /// <summary>
        /// 根据指定的私钥和曲线参数来创建新的ECDsa对象，该对象可用于签名
        /// </summary>
        /// <param name="privateKey">私钥</param>
        /// <param name="curve">椭圆曲线参数</param>
        public ECDsa(byte[] privateKey, ECCurve curve)
            : this(curve.G * privateKey)
        {
            this.privateKey = privateKey;
        }

        /// <summary>
        /// 根据指定的公钥来创建新的ECDsa对象，该对象可用于验证签名
        /// </summary>
        /// <param name="publicKey">公钥</param>
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

        /// <summary>
        /// 生成椭圆曲线数字签名
        /// </summary>
        /// <param name="message">要签名的消息</param>
        /// <returns>返回签名的数字编码（r,s）</returns>
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

        /// <summary>
        /// 验证签名的合法性
        /// </summary>
        /// <param name="message">要验证的消息</param>
        /// <param name="r">签名的数字编码</param>
        /// <param name="s">签名的数字编码</param>
        /// <returns>返回验证的结果</returns>
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
    }
}
