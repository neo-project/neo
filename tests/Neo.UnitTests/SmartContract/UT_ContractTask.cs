// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractTask.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractTask
    {
        [TestMethod]
        public void CompletedTask_IsCompleted()
        {
            Assert.IsTrue(ContractTask.CompletedTask.GetAwaiter().IsCompleted);
            ContractTask.CompletedTask.GetAwaiter().GetResult();
            Assert.IsNull(ContractTask.CompletedTask.GetResult());
        }

        [TestMethod]
        public void Generic_CompletedTask_IsCompleted()
        {
            Assert.IsTrue(ContractTask<int>.CompletedTask.GetAwaiter().IsCompleted);
            Assert.AreEqual(default(int), ContractTask<int>.CompletedTask.GetAwaiter().GetResult());
        }

        [TestMethod]
        public void NewTask_SetResult_Completes()
        {
            var task = new ContractTask();
            Assert.IsFalse(task.GetAwaiter().IsCompleted);
            task.GetAwaiter().SetResult();
            Assert.IsTrue(task.GetAwaiter().IsCompleted);
        }

        [TestMethod]
        public void NewTask_SetException_ThrowsOnGetResult()
        {
            var task = new ContractTask();
            task.GetAwaiter().SetException(new InvalidOperationException("fail"));
            Assert.ThrowsExactly<InvalidOperationException>(() => task.GetAwaiter().GetResult());
        }

        [TestMethod]
        public void GenericTask_SetResult_ReturnsValue()
        {
            var task = new ContractTask<string>();
            task.GetAwaiter().SetResult("ok");
            Assert.AreEqual("ok", task.GetAwaiter().GetResult());
            Assert.AreEqual("ok", task.GetResult());
        }
    }
}
