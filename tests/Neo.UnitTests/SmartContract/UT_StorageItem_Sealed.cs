// Copyright (C) 2015-2026 The Neo Project.
//
// UT_StorageItem_Sealed.cs file belongs to the neo project and is free
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
using Neo.SmartContract.Native;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Coverage for sealed interoperable storage helpers not covered by UT_StorageItem.
    /// </summary>
    [TestClass]
    public class UT_StorageItem_Sealed
    {
        [TestMethod]
        public void BigInteger_Constructor_MaterializesValue()
        {
            var item = new StorageItem(new BigInteger(12345));
            Assert.IsFalse(item.Value.IsEmpty);
            CollectionAssert.AreEqual(new BigInteger(12345).ToByteArrayStandard(), item.Value.ToArray());
        }

        [TestMethod]
        public void CreateSealed_AccountState_IsSerializable()
        {
            var state = new AccountState { Balance = 100 };
            Assert.IsTrue(StorageItem.IsSerializable(state));

            var sealedItem = StorageItem.CreateSealed(state);
            Assert.IsFalse(sealedItem.Value.IsEmpty);

            var roundTrip = sealedItem.GetInteroperable<AccountState>();
            Assert.AreEqual(100, (int)roundTrip.Balance);
        }

        [TestMethod]
        public void Add_IncrementsIntegerValue()
        {
            var item = new StorageItem(new BigInteger(10));
            item.Add(5);
            CollectionAssert.AreEqual(new BigInteger(15).ToByteArrayStandard(), item.Value.ToArray());
        }
    }
}
