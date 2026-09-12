// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WhitelistedContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_WhitelistedContract
    {
        [TestMethod]
        public void StackItem_RoundTrip()
        {
            var hash = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var original = new WhitelistedContract
            {
                ContractHash = hash,
                Method = "transfer",
                ArgCount = 3,
                FixedFee = 1_000_000
            };

            var item = original.ToStackItem();
            Assert.IsInstanceOfType<Struct>(item);

            var restored = new WhitelistedContract
            {
                ContractHash = UInt160.Zero,
                Method = "",
                ArgCount = 0,
                FixedFee = 0
            };
            restored.FromStackItem(item);

            Assert.AreEqual(original.ContractHash, restored.ContractHash);
            Assert.AreEqual(original.Method, restored.Method);
            Assert.AreEqual(original.ArgCount, restored.ArgCount);
            Assert.AreEqual(original.FixedFee, restored.FixedFee);
        }

        [TestMethod]
        public void ToStackItem_ContainsExpectedFields()
        {
            var hash = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");
            var contract = new WhitelistedContract
            {
                ContractHash = hash,
                Method = "balanceOf",
                ArgCount = 1,
                FixedFee = 42
            };

            var @struct = (Struct)contract.ToStackItem();
            Assert.AreEqual(4, @struct.Count);
            Assert.IsTrue(hash.ToArray().AsSpan().SequenceEqual(@struct[0].GetSpan()));
            Assert.AreEqual("balanceOf", @struct[1].GetString());
            Assert.AreEqual(1, (int)@struct[2].GetInteger());
            Assert.AreEqual(42L, (long)@struct[3].GetInteger());
        }
    }
}
