using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Numerics;

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

            ECPoint recoverKey = ECDsa.KeyRecover(ECCurve.Secp256k1, signatures[0], signatures[1], message, true, true);
            ECPoint recoverKey2 = ECDsa.KeyRecover(ECCurve.Secp256k1, signatures[0], signatures[1], message, false, true);
            //due to generated signature does not have Ytilde.
            Assert.IsTrue(recoverKey.Equals(publickey) || recoverKey2.Equals(publickey));

            //wrong key part
            var r = new System.Numerics.BigInteger(generatekey(32));
            try
            {
                recoverKey = ECDsa.KeyRecover(ECCurve.Secp256k1, r, signatures[1], message, publickey.Y.Value.IsEven, true);
                Assert.IsFalse(recoverKey.Equals(publickey));
            }
            //wrong key may cause exception in decompresspoint
            catch { }

            try
            {
                var s = new System.Numerics.BigInteger(generatekey(32));
                recoverKey = ECDsa.KeyRecover(ECCurve.Secp256k1, signatures[0], s, message, publickey.Y.Value.IsEven, true);
                Assert.IsFalse(recoverKey.Equals(publickey));
            }
            //wrong key may cause exception in decompresspoint
            catch { }
        }
    }
}