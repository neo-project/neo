// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_AccountState
    {
        [TestMethod]
        public void ToStackItem_FromStackItem_RoundTrip()
        {
            var original = new AccountState { Balance = 1_234_567_890 };
            var item = original.ToStackItem();
            Assert.IsInstanceOfType<Struct>(item);

            var clone = new AccountState();
            clone.FromStackItem(item);
            Assert.AreEqual(original.Balance, clone.Balance);
        }

        [TestMethod]
        public void FromStackItem_ZeroBalance()
        {
            var state = new AccountState { Balance = 99 };
            state.FromStackItem(new Struct { BigInteger.Zero });
            Assert.AreEqual(BigInteger.Zero, state.Balance);
        }

        [TestMethod]
        public void ToStackItem_NegativeBalance()
        {
            var state = new AccountState { Balance = -5 };
            var item = (Struct)state.ToStackItem();
            Assert.AreEqual(new BigInteger(-5), item[0].GetInteger());
        }
    }
}
