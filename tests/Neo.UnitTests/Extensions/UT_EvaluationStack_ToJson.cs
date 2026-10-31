// Copyright (C) 2015-2026 The Neo Project.
//
// UT_EvaluationStack_ToJson.cs file belongs to the neo project and is free
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
using Neo.VM;
using System;

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_EvaluationStack_ToJson
    {
        [TestMethod]
        public void ToJson_MaxSizeNonPositive_Throws()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.PUSH1 }, snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => engine.ResultStack.ToJson(0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => engine.ResultStack.ToJson(-1));
        }

        [TestMethod]
        public void ToJson_SerializesStackItems()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH2 }, snapshot);
            Assert.AreEqual(VMState.HALT, engine.State);

            var json = engine.ResultStack.ToJson();
            Assert.IsTrue(json.Count >= 1);
            Assert.IsNotNull(json[0]!["type"]);
        }

        [TestMethod]
        public void ToJson_TinyMaxSize_ThrowsWhenExceeded()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH2, (byte)OpCode.PUSH3 }, snapshot);
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.ResultStack.ToJson(maxSize: 5));
        }
    }
}
