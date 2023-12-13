using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Wallets;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_KeyPair
    {
        [TestMethod]
        public void TestConstructor()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint publicKey = ECCurve.Secp256r1.G * privateKey;
            keyPair.PrivateKey.Should().BeEquivalentTo(privateKey);
            keyPair.PublicKey.Should().Be(publicKey);

            byte[] privateKey96 = new byte[96];
            for (int i = 0; i < privateKey96.Length; i++)
                privateKey96[i] = (byte)random.Next(256);
            keyPair = new KeyPair(privateKey96);
            publicKey = ECPoint.DecodePoint(new byte[] { 0x04 }.Concat(privateKey96.Skip(privateKey96.Length - 96).Take(64)).ToArray(), ECCurve.Secp256r1);
            keyPair.PrivateKey.Should().BeEquivalentTo(privateKey96.Skip(64).Take(32));
            keyPair.PublicKey.Should().Be(publicKey);

            byte[] privateKey31 = new byte[31];
            for (int i = 0; i < privateKey31.Length; i++)
                privateKey31[i] = (byte)random.Next(256);
            Action action = () => new KeyPair(privateKey31);
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestEquals()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            KeyPair keyPair2 = keyPair;
            keyPair.Equals(keyPair2).Should().BeTrue();

            KeyPair keyPair3 = null;
            keyPair.Equals(keyPair3).Should().BeFalse();

            byte[] privateKey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            byte[] privateKey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02};
            KeyPair keyPair4 = new KeyPair(privateKey1);
            KeyPair keyPair5 = new KeyPair(privateKey2);
            keyPair4.Equals(keyPair5).Should().BeFalse();
        }

        [TestMethod]
        public void TestEqualsWithObj()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            Object keyPair2 = keyPair as Object;
            keyPair.Equals(keyPair2).Should().BeTrue();
        }

        [TestMethod]
        public void TestExport()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            byte[] data = { 0x80, 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            keyPair.Export().Should().Be(Base58.Base58CheckEncode(data));
        }

        [TestMethod]
        public void TestGetPublicKeyHash()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            keyPair.PublicKeyHash.ToString().Should().Be("0x4ab3d6ac3a0609e87af84599c93d57c2d0890406");
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair1 = new KeyPair(privateKey);
            KeyPair keyPair2 = new KeyPair(privateKey);
            keyPair1.GetHashCode().Should().Be(keyPair2.GetHashCode());
        }

        [TestMethod]
        public void TestToString()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            keyPair.ToString().Should().Be("026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16");
        }
    }
}
