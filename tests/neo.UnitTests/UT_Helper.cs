using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using Neo.Network.P2P;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void GetHashData()
        {
            TestVerifiable verifiable = new TestVerifiable();
            byte[] res = verifiable.GetHashData();
            res.ToHexString().Should().Be("4e454f000774657374537472");
        }

        [TestMethod]
        public void Sign()
        {
            TestVerifiable verifiable = new TestVerifiable();
            byte[] res = verifiable.Sign(new KeyPair(TestUtils.GetByteArray(32, 0x42)));
            res.Length.Should().Be(64);
        }

        [TestMethod]
        public void ToScriptHash()
        {
            byte[] testByteArray = TestUtils.GetByteArray(64, 0x42);
            UInt160 res = testByteArray.ToScriptHash();
            res.Should().Be(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
        }

        [TestMethod]
        public void TestGetLowestSetBit()
        {
            var big1 = new BigInteger(0);
            big1.GetLowestSetBit().Should().Be(-1);

            var big2 = new BigInteger(512);
            big2.GetLowestSetBit().Should().Be(9);

            var big3 = new BigInteger(int.MinValue);
            big3.GetLowestSetBit().Should().Be(31);

            var big4 = new BigInteger(long.MinValue);
            big4.GetLowestSetBit().Should().Be(63);
        }

        [TestMethod]
        public void TestGetBitLength()
        {
            new BigInteger(100).GetBitLength().Should().Be(7);
            new BigInteger(-100).GetBitLength().Should().Be(7);
            new BigInteger(0).GetBitLength().Should().Be(8);
            new BigInteger(512).GetBitLength().Should().Be(10);
            new BigInteger(short.MinValue).GetBitLength().Should().Be(15);
            new BigInteger(short.MaxValue).GetBitLength().Should().Be(15);
            new BigInteger(int.MinValue).GetBitLength().Should().Be(31);
            new BigInteger(int.MaxValue).GetBitLength().Should().Be(31);
            new BigInteger(long.MinValue).GetBitLength().Should().Be(63);
            new BigInteger(long.MaxValue).GetBitLength().Should().Be(63);
        }

        [TestMethod]
        public void TestHexToBytes()
        {
            string nullStr = null;
            nullStr.HexToBytes().ToHexString().Should().Be(new byte[0].ToHexString());
            string emptyStr = "";
            emptyStr.HexToBytes().ToHexString().Should().Be(new byte[0].ToHexString());
            string str1 = "hab";
            Action action = () => str1.HexToBytes();
            action.Should().Throw<FormatException>();
            string str2 = "0102";
            byte[] bytes = str2.HexToBytes();
            bytes.ToHexString().Should().Be(new byte[] { 0x01, 0x02 }.ToHexString());
        }

        [TestMethod]
        public void TestRemoveHashsetDictionary()
        {
            var a = new HashSet<int>
            {
                1,
                2,
                3
            };

            var b = new Dictionary<int, object>
            {
                [2] = null
            };

            a.Remove(b);

            CollectionAssert.AreEqual(new int[] { 1, 3 }, a.ToArray());

            b[4] = null;
            b[5] = null;
            b[1] = null;
            a.Remove(b);

            CollectionAssert.AreEqual(new int[] { 3 }, a.ToArray());
        }

        [TestMethod]
        public void TestRemoveHashsetSet()
        {
            var a = new HashSet<int>
            {
                1,
                2,
                3
            };

            var b = new SortedSet<int>()
            {
                2
            };

            a.Remove(b);

            CollectionAssert.AreEqual(new int[] { 1, 3 }, a.ToArray());

            b.Add(4);
            b.Add(5);
            b.Add(1);
            a.Remove(b);

            CollectionAssert.AreEqual(new int[] { 3 }, a.ToArray());
        }

        [TestMethod]
        public void TestRemoveHashsetHashSetCache()
        {
            var a = new HashSet<int>
            {
                1,
                2,
                3
            };

            var b = new HashSetCache<int>(10)
            {
                2
            };

            a.Remove(b);

            CollectionAssert.AreEqual(new int[] { 1, 3 }, a.ToArray());

            b.Add(4);
            b.Add(5);
            b.Add(1);
            a.Remove(b);

            CollectionAssert.AreEqual(new int[] { 3 }, a.ToArray());
        }

        [TestMethod]
        public void TestToHexString()
        {
            byte[] nullStr = null;
            Assert.ThrowsException<NullReferenceException>(() => nullStr.ToHexString());
            byte[] empty = new byte[0];
            empty.ToHexString().Should().Be("");
            empty.ToHexString(false).Should().Be("");
            empty.ToHexString(true).Should().Be("");

            byte[] str1 = new byte[] { (byte)'n', (byte)'e', (byte)'o' };
            str1.ToHexString().Should().Be("6e656f");
            str1.ToHexString(false).Should().Be("6e656f");
            str1.ToHexString(true).Should().Be("6f656e");
        }

        [TestMethod]
        public void TestGetVersion()
        {
            string version = typeof(TestMethodAttribute).Assembly.GetVersion();
            version.Should().Be("14.0.4701.02");

            // assembly without version

            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .Where(u => u.FullName == "Anonymously Hosted DynamicMethods Assembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                .FirstOrDefault();
            version = asm?.GetVersion() ?? "";
            version.Should().Be("0.0.0");
        }

        [TestMethod]
        public void TestToByteArrayStandard()
        {
            BigInteger number = BigInteger.Zero;
            Assert.AreEqual("", number.ToByteArrayStandard().ToHexString());

            number = BigInteger.One;
            Assert.AreEqual("01", number.ToByteArrayStandard().ToHexString());
        }

        [TestMethod]
        public void TestNextBigIntegerForRandom()
        {
            Random ran = new Random();
            Action action1 = () => ran.NextBigInteger(-1);
            action1.Should().Throw<ArgumentException>();

            ran.NextBigInteger(0).Should().Be(0);
            ran.NextBigInteger(8).Should().NotBeNull();
            ran.NextBigInteger(9).Should().NotBeNull();
        }

        [TestMethod]
        public void TestNextBigIntegerForRandomNumberGenerator()
        {
            var ran = RandomNumberGenerator.Create();
            Action action1 = () => ran.NextBigInteger(-1);
            action1.Should().Throw<ArgumentException>();

            ran.NextBigInteger(0).Should().Be(0);
            ran.NextBigInteger(8).Should().NotBeNull();
            ran.NextBigInteger(9).Should().NotBeNull();
        }

        [TestMethod]
        public void TestUnmapForIPAddress()
        {
            var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            addr.Unmap().Should().Be(addr);

            var addr2 = addr.MapToIPv6();
            addr2.Unmap().Should().Be(addr);
        }

        [TestMethod]
        public void TestUnmapForIPEndPoin()
        {
            var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            var endPoint = new IPEndPoint(addr, 8888);
            endPoint.Unmap().Should().Be(endPoint);

            var addr2 = addr.MapToIPv6();
            var endPoint2 = new IPEndPoint(addr2, 8888);
            endPoint2.Unmap().Should().Be(endPoint);
        }

        [TestMethod]
        public void XorUInt256List()
        {
            ((UInt256[])null).XorUInt256List().Should().Be(UInt256.Zero);
            new UInt256[] { }.XorUInt256List().Should().Be(UInt256.Zero);
            new UInt256[] { UInt256.Zero }.XorUInt256List().Should().Be(UInt256.Zero);
            new UInt256[] { UInt256.Parse("0x557f5c9d0c865a211a749899681e5b4fbf745b3bcf0c395e6d6a7f1edb0d86f1"),
                UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02") }
            .XorUInt256List().Should().Be(UInt256.Parse("0xf17fa39df386a521e5746799971ea44f4074a43b300cc65e926a801e240d79f3"));
        }
    }
}
