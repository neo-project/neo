using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Numerics;
using System;

namespace Neo.Cryptography.ECC.Tests
{
    [TestClass()]
    public class UT_ECDsa
    {
        private ECDsa ecdsa;

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
            byte[] privateKey = generatekey(32);
            ECPoint publickey = ECCurve.Secp256k1.G * privateKey;
            ecdsa = new ECDsa(privateKey, ECCurve.Secp256k1);
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
            ECPoint recoverKey = ECDsa.KeyRecover(ECCurve.Secp256k1, r, s, message, v, true);
            Assert.IsTrue(recoverKey.Equals(publickey));
            //wrong key part
            r = new System.Numerics.BigInteger(generatekey(32));
            try
            {
                recoverKey = ECDsa.KeyRecover(ECCurve.Secp256k1, r, signatures[1], message, true, true);
                Assert.IsFalse(recoverKey.Equals(publickey));
            }
            //wrong key may cause exception in decompresspoint
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType() == typeof(ArithmeticException));
            }
            s = new System.Numerics.BigInteger(generatekey(32));
            try
            {
                recoverKey = ECDsa.KeyRecover(ECCurve.Secp256k1, signatures[0], s, message, true, true);
                Assert.IsFalse(recoverKey.Equals(publickey));
            }
            //wrong key may cause exception in decompresspoint
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType() == typeof(ArithmeticException));
            }
        }
    }
}