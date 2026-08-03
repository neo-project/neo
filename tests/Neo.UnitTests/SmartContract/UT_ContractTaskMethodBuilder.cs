// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractTaskMethodBuilder.cs file belongs to the neo project and is free
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
    public class UT_ContractTaskMethodBuilder
    {
        [TestMethod]
        public void Create_SetResult_CompletesTask()
        {
            var builder = ContractTaskMethodBuilder.Create();
            var task = builder.Task;
            Assert.IsFalse(task.GetAwaiter().IsCompleted);
            builder.SetResult();
            Assert.IsTrue(task.GetAwaiter().IsCompleted);
            task.GetAwaiter().GetResult();
        }

        [TestMethod]
        public void Create_SetException_FaultsTask()
        {
            var builder = ContractTaskMethodBuilder.Create();
            var task = builder.Task;
            builder.SetException(new InvalidOperationException("boom"));
            Assert.IsTrue(task.GetAwaiter().IsCompleted);
            Assert.ThrowsExactly<InvalidOperationException>(() => task.GetAwaiter().GetResult());
        }

        [TestMethod]
        public void Generic_SetResult_ReturnsValue()
        {
            var builder = ContractTaskMethodBuilder<int>.Create();
            var task = builder.Task;
            builder.SetResult(42);
            Assert.IsTrue(task.GetAwaiter().IsCompleted);
            Assert.AreEqual(42, task.GetAwaiter().GetResult());
        }

        [TestMethod]
        public void Generic_SetException_FaultsTask()
        {
            var builder = ContractTaskMethodBuilder<string>.Create();
            builder.SetException(new ArgumentException("bad"));
            Assert.ThrowsExactly<ArgumentException>(() => builder.Task.GetAwaiter().GetResult());
        }

        [TestMethod]
        public void SetStateMachine_IsNoOp()
        {
            var builder = ContractTaskMethodBuilder.Create();
            builder.SetStateMachine(null);
            var generic = ContractTaskMethodBuilder<bool>.Create();
            generic.SetStateMachine(null);
        }
    }
}
