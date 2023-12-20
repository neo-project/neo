// Copyright (C) 2015-2024 The Neo Project.
//
// UtExecutionContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Test
{
    [TestClass]
    public class UtExecutionContext
    {
        class TestState
        {
            public bool Flag = false;
        }

        [TestMethod]
        public void StateTest()
        {
            var context = new ExecutionContext(Array.Empty<byte>(), -1, new ReferenceCounter());

            // Test factory

            var flag = context.GetState(() => new TestState() { Flag = true });
            Assert.IsTrue(flag.Flag);

            flag.Flag = false;

            flag = context.GetState(() => new TestState() { Flag = true });
            Assert.IsFalse(flag.Flag);

            // Test new

            var stack = context.GetState<Stack<int>>();
            Assert.AreEqual(0, stack.Count);
            stack.Push(100);
            stack = context.GetState<Stack<int>>();
            Assert.AreEqual(100, stack.Pop());
            stack.Push(100);

            // Test clone

            var copy = context.Clone();
            var copyStack = copy.GetState<Stack<int>>();
            Assert.AreEqual(1, copyStack.Count);
            copyStack.Push(200);
            copyStack = context.GetState<Stack<int>>();
            Assert.AreEqual(200, copyStack.Pop());
            Assert.AreEqual(100, copyStack.Pop());
            copyStack.Push(200);

            stack = context.GetState<Stack<int>>();
            Assert.AreEqual(200, stack.Pop());
        }
    }
}
