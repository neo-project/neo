using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Numerics;
using System;

namespace Neo.Cryptography.ECC.Tests
{
    [TestClass()]
    public class UT_ECDsa
    {
        public static byte[] generatekey(int privateKeyLength)
        {
            byte[] privateKey = new byte[privateKeyLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return privateKey;
        }

        [TestMethod()]
        public void KeyRecoverTest()
        {
            KeyRecover(ECCurve.Secp256k1);
            KeyRecover(ECCurve.Secp256r1);
        }

        public static void KeyRecover(ECCurve Curve)
        {
            byte[] privateKey = generatekey(32);
            ECPoint publicKey = Curve.G * privateKey;
            ECDsa ecdsa = new ECDsa(privateKey, Curve);
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            BigInteger[] signatures = ecdsa.GenerateSignature(message);
            BigInteger r = signatures[0];
            BigInteger s = signatures[1];
            bool v;
            if (signatures[2] == 0)
            {
                v = true;
            }
            else
            {
                v = false;
            }
            ECPoint recoverKey = ECDsa.KeyRecover(Curve, r, s, message, v);
            Assert.IsTrue(recoverKey.Equals(publicKey));
            //wrong r part
            r = new BigInteger(generatekey(32));
            s = new BigInteger(generatekey(32));
            try
            {
                recoverKey = ECDsa.KeyRecover(Curve, r, s, message, v);
                Assert.IsFalse(recoverKey.Equals(publicKey));
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType() == typeof(ArithmeticException));
            }
        }
    }
}