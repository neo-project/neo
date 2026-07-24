// Copyright (C) 2015-2026 The Neo Project.
//
// UT_OracleRequest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_OracleRequest
    {
        [TestMethod]
        public void ToStackItem_And_FromStackItem_RoundTrip_WithFilter()
        {
            var original = new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 1_000_000,
                Url = "https://example.com/api",
                Filter = "$.data",
                CallbackContract = UInt160.Zero,
                CallbackMethod = "callback",
                UserData = [0x01, 0x02, 0x03]
            };

            var item = original.ToStackItem();
            Assert.IsInstanceOfType<Array>(item);

            var clone = new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 0,
                Url = "",
                Filter = null,
                CallbackContract = UInt160.Zero,
                CallbackMethod = "",
                UserData = []
            };
            clone.FromStackItem(item);

            Assert.AreEqual(original.OriginalTxid, clone.OriginalTxid);
            Assert.AreEqual(original.GasForResponse, clone.GasForResponse);
            Assert.AreEqual(original.Url, clone.Url);
            Assert.AreEqual(original.Filter, clone.Filter);
            Assert.AreEqual(original.CallbackContract, clone.CallbackContract);
            Assert.AreEqual(original.CallbackMethod, clone.CallbackMethod);
            CollectionAssert.AreEqual(original.UserData, clone.UserData);
        }

        [TestMethod]
        public void ToStackItem_And_FromStackItem_RoundTrip_NullFilter()
        {
            var original = new OracleRequest
            {
                OriginalTxid = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001"),
                GasForResponse = 42,
                Url = "https://neo.org",
                Filter = null,
                CallbackContract = UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"),
                CallbackMethod = "onResponse",
                UserData = []
            };

            var item = (Array)original.ToStackItem();
            Assert.IsTrue(item[3].IsNull);

            var clone = new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 0,
                Url = "",
                Filter = "ignore",
                CallbackContract = UInt160.Zero,
                CallbackMethod = "",
                UserData = [9]
            };
            clone.FromStackItem(item);

            Assert.AreEqual(original.OriginalTxid, clone.OriginalTxid);
            Assert.AreEqual(42, clone.GasForResponse);
            Assert.AreEqual("https://neo.org", clone.Url);
            Assert.IsNull(clone.Filter);
            Assert.AreEqual(original.CallbackContract, clone.CallbackContract);
            Assert.AreEqual("onResponse", clone.CallbackMethod);
            Assert.IsEmpty(clone.UserData);
        }
    }
}
