// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO.Caching;
using Neo.Network.P2P;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void GetSignData()
        {
            TestVerifiable verifiable = new();
            byte[] res = verifiable.GetSignData(TestProtocolSettings.Default.Network);
            Assert.AreEqual("4e454f3350b51da6bb366be3ea50140cda45ba7df575287c0371000b2037ed3898ff8bf5", res.ToHexString());
        }

        [TestMethod]
        public void Sign()
        {
            TestVerifiable verifiable = new();
            byte[] res = verifiable.Sign(new KeyPair(TestUtils.GetByteArray(32, 0x42)), TestProtocolSettings.Default.Network);
            Assert.AreEqual(64, res.Length);
        }

        [TestMethod]
        public void ToScriptHash()
        {
            byte[] testByteArray = TestUtils.GetByteArray(64, 0x42);
            UInt160 res = testByteArray.ToScriptHash();
            Assert.AreEqual(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"), res);
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
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullStr.ToHexString());
            byte[] empty = Array.Empty<byte>();
            Assert.AreEqual("", empty.ToHexString());
            Assert.AreEqual("", empty.ToHexString(false));
            Assert.AreEqual("", empty.ToHexString(true));

            byte[] str1 = new byte[] { (byte)'n', (byte)'e', (byte)'o' };
            Assert.AreEqual("6e656f", str1.ToHexString());
            Assert.AreEqual("6e656f", str1.ToHexString(false));
            Assert.AreEqual("6f656e", str1.ToHexString(true));
        }

        [TestMethod]
        public void TestGetVersion()
        {
            // assembly without version

            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .Where(u => u.FullName == "Anonymously Hosted DynamicMethods Assembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                .FirstOrDefault();
            string version = asm?.GetVersion() ?? "";
            Assert.AreEqual("0.0.0", version);
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
            Random ran = new();
            Action action1 = () => ran.NextBigInteger(-1);
            Assert.ThrowsExactly<ArgumentException>(() => action1());

            Assert.AreEqual(0, ran.NextBigInteger(0));
            Assert.IsNotNull(ran.NextBigInteger(8));
            Assert.IsNotNull(ran.NextBigInteger(9));
        }

        [TestMethod]
        public void TestUnmapForIPAddress()
        {
            var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            Assert.AreEqual(addr, addr.UnMap());

            var addr2 = addr.MapToIPv6();
            Assert.AreEqual(addr, addr2.UnMap());
        }

        [TestMethod]
        public void TestUnmapForIPEndPoin()
        {
            var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            var endPoint = new IPEndPoint(addr, 8888);
            Assert.AreEqual(endPoint, endPoint.UnMap());

            var addr2 = addr.MapToIPv6();
            var endPoint2 = new IPEndPoint(addr2, 8888);
            Assert.AreEqual(endPoint, endPoint2.UnMap());
        }
    }
}
