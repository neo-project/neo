// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NotifyEventArgs_StackItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// StackItem conversion coverage not covered by UT_NotifyEventArgs container tests.
    /// </summary>
    [TestClass]
    public class UT_NotifyEventArgs_StackItem
    {
        [TestMethod]
        public void ToStackItem_IncludesHashNameAndState()
        {
            var hash = UInt160.Parse("0x179ab5d297fd34ecd48643894242fc3527f42853");
            var state = new Array { "a", 1 };
            var args = new NotifyEventArgs(null, hash, "Transfer", state);

            var item = (Array)args.ToStackItem();
            Assert.HasCount(3, item);
            Assert.AreEqual(hash, new UInt160(item[0].GetSpan()));
            Assert.AreEqual("Transfer", item[1].GetString());
            Assert.IsInstanceOfType<Array>(item[2]);
            Assert.AreSame(state, item[2]);
        }

        [TestMethod]
        public void FromStackItem_ThrowsNotSupported()
        {
            var args = new NotifyEventArgs(null, UInt160.Zero, "x", new Array());
            Assert.ThrowsExactly<NotSupportedException>(() => args.FromStackItem(StackItem.Null));
        }

        [TestMethod]
        public void Properties_MatchConstructor()
        {
            var container = new TestVerifiable();
            var hash = UInt160.Zero;
            var state = new Array();
            var args = new NotifyEventArgs(container, hash, "evt", state);

            Assert.AreEqual(container, args.ScriptContainer);
            Assert.AreEqual(hash, args.ScriptHash);
            Assert.AreEqual("evt", args.EventName);
            Assert.AreSame(state, args.State);
        }
    }
}
