// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NotifyEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_NotifyEventArgs
    {
        [TestMethod]
        public void TestGetScriptContainer()
        {
            IVerifiable container = new TestVerifiable();
            UInt160 script_hash = new byte[] { 0x00 }.ToScriptHash();
            NotifyEventArgs args = new NotifyEventArgs(container, script_hash, "Test", null);
            args.ScriptContainer.Should().Be(container);
        }
    }
}
