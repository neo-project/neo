// Copyright (C) 2015-2024 The Neo Project.
//
// UT_KeyBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_KeyBuilder
    {
        private struct TestKey
        {
            public int Value;
        }

        [TestMethod]
        public void Test()
        {
            var key = new KeyBuilder(1, 2);

            Assert.AreEqual("0100000002", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new byte[] { 3, 4 });
            Assert.AreEqual("01000000020304", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new byte[] { 3, 4 });
            key = key.Add(UInt160.Zero);
            Assert.AreEqual("010000000203040000000000000000000000000000000000000000", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new TestKey { Value = 123 });
            Assert.AreEqual("01000000027b000000", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 0);
            key = key.AddBigEndian(new TestKey { Value = 1 });
            Assert.AreEqual("010000000000000001", key.ToArray().ToHexString());
        }
    }
}
