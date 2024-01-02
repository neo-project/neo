// Copyright (C) 2015-2024 The Neo Project.
//
// UT_LogEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_LogEventArgs
    {
        [TestMethod]
        public void TestGeneratorAndGet()
        {
            IVerifiable container = new Header();
            UInt160 scripthash = UInt160.Zero;
            string message = "lalala";
            LogEventArgs logEventArgs = new LogEventArgs(container, scripthash, message);
            Assert.IsNotNull(logEventArgs);
            Assert.AreEqual(container, logEventArgs.ScriptContainer);
            Assert.AreEqual(scripthash, logEventArgs.ScriptHash);
            Assert.AreEqual(message, logEventArgs.Message);
        }
    }
}
