// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_Helper
    {
        [TestMethod]
        public void FaultHelpers_ReturnEmpty_WhenNotFaulted()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Assert.AreEqual(VMState.BREAK, engine.State);
            Assert.AreEqual("", engine.GetEngineStackInfoOnFault());
            Assert.AreEqual("", engine.GetEngineExceptionInfo());
        }

        [TestMethod]
        public void FaultHelpers_IncludeMessage_WhenFaulted()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            // ABORT forces FAULT
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.ABORT }, snapshot);
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.IsNotNull(engine.FaultException);

            var messageOnly = engine.GetEngineExceptionInfo(exceptionStackTrace: false, exceptionMessage: true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(messageOnly));

            var stackInfo = engine.GetEngineStackInfoOnFault(exceptionStackTrace: false, exceptionMessage: true);
            Assert.IsTrue(stackInfo.Contains("CurrentScriptHash=", StringComparison.Ordinal));
            Assert.IsTrue(stackInfo.Contains("EntryScriptHash=", StringComparison.Ordinal));
        }
    }
}
