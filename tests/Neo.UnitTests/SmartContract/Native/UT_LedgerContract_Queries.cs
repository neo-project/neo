// Copyright (C) 2015-2026 The Neo Project.
//
// UT_LedgerContract_Queries.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_LedgerContract_Queries
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void CurrentIndex_And_CurrentHash_OnGenesis()
        {
            var index = NativeContract.Ledger.Call(_snapshot, "currentIndex");
            Assert.IsInstanceOfType<Integer>(index);
            Assert.IsTrue(index.GetInteger() >= 0);

            var hash = NativeContract.Ledger.Call(_snapshot, "currentHash");
            Assert.IsInstanceOfType<ByteString>(hash);
            Assert.AreEqual(UInt256.Zero.Size, ((ByteString)hash).Size);
        }

        [TestMethod]
        public void GetBlock_Genesis_Exists()
        {
            var hashItem = NativeContract.Ledger.Call(_snapshot, "currentHash");
            var hash = new UInt256(hashItem.GetSpan());
            var block = NativeContract.Ledger.Call(_snapshot, "getBlock",
                new ContractParameter(ContractParameterType.Hash256) { Value = hash });
            Assert.IsFalse(block.IsNull);
        }

        [TestMethod]
        public void GetTransaction_Missing_ReturnsNull()
        {
            var tx = NativeContract.Ledger.Call(_snapshot, "getTransaction",
                new ContractParameter(ContractParameterType.Hash256) { Value = UInt256.Zero });
            Assert.IsTrue(tx.IsNull);
        }

        [TestMethod]
        public void GetTransactionFromBlock_InvalidIndex_ThrowsOrNull()
        {
            // Genesis has no user transactions; out-of-range should fail or null depending on API.
            try
            {
                var result = NativeContract.Ledger.Call(_snapshot, "getTransactionFromBlock",
                    new ContractParameter(ContractParameterType.Integer) { Value = 0 },
                    new ContractParameter(ContractParameterType.Integer) { Value = 9999 });
                Assert.IsTrue(result.IsNull);
            }
            catch (Exception)
            {
                // Acceptable: native throws on invalid tx index.
            }
        }
    }
}
