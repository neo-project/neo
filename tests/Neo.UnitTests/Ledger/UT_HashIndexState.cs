// Copyright (C) 2015-2025 The Neo Project.
//
// UT_HashIndexState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_HashIndexState
    {
        HashIndexState origin;

        [TestInitialize]
        public void Initialize()
        {
            origin = new HashIndexState
            {
                Hash = UInt256.Zero,
                Index = 10
            };
        }

        [TestMethod]
        public void TestDeserialize()
        {
            var data = BinarySerializer.Serialize(((IInteroperable)origin).ToStackItem(null), ExecutionEngineLimits.Default);
            var reader = new MemoryReader(data);

            HashIndexState dest = new();
            ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

            Assert.AreEqual(origin.Hash, dest.Hash);
            Assert.AreEqual(origin.Index, dest.Index);
        }
    }
}
