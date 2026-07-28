// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractTaskAwaiter_OnCompleted.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractTaskAwaiter_OnCompleted
    {
        [TestMethod]
        public void OnCompleted_RunsWhenSetResult()
        {
            var task = new ContractTask();
            var ran = false;
            task.GetAwaiter().OnCompleted(() => ran = true);
            Assert.IsFalse(ran);
            task.GetAwaiter().SetResult();
            Assert.IsTrue(ran);
        }

        [TestMethod]
        public void OnCompleted_AfterCompletion_DoesNotRunAutomatically()
        {
            var task = new ContractTask();
            task.GetAwaiter().SetResult();
            var ran = false;
            // Continuation registered after completion is not invoked until another completion path.
            task.GetAwaiter().OnCompleted(() => ran = true);
            Assert.IsFalse(ran);
        }
    }
}
