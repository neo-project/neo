// Copyright (C) 2015-2026 The Neo Project.
//
// UT_IInteroperable_Clone.cs file belongs to the neo project and is free
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
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_IInteroperable_Clone
    {
        [TestMethod]
        public void Clone_CopiesStackItemState()
        {
            var original = new AccountState { Balance = 12345 };
            var clone = (AccountState)((IInteroperable)original).Clone();
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Balance, clone.Balance);

            clone.Balance = 1;
            Assert.AreEqual(new BigInteger(12345), original.Balance);
        }

        [TestMethod]
        public void FromReplica_OverwritesState()
        {
            var source = new AccountState { Balance = 99 };
            var target = new AccountState { Balance = 0 };
            ((IInteroperable)target).FromReplica(source);
            Assert.AreEqual(new BigInteger(99), target.Balance);
        }
    }
}
