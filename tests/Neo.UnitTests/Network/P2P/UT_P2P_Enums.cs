// Copyright (C) 2015-2026 The Neo Project.
//
// UT_P2P_Enums.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_P2P_Enums
    {
        [TestMethod]
        public void WitnessScope_Flags()
        {
            Assert.AreEqual(WitnessScope.None, (WitnessScope)0);
            Assert.AreEqual((WitnessScope)0x01, WitnessScope.CalledByEntry);
            Assert.AreEqual((WitnessScope)0x10, WitnessScope.CustomContracts);
            Assert.AreEqual((WitnessScope)0x20, WitnessScope.CustomGroups);
            Assert.AreEqual((WitnessScope)0x40, WitnessScope.WitnessRules);
            Assert.AreEqual((WitnessScope)0x80, WitnessScope.Global);
            Assert.IsTrue((WitnessScope.CalledByEntry | WitnessScope.CustomContracts).HasFlag(WitnessScope.CalledByEntry));
        }

        [TestMethod]
        public void MessageCommand_HandshakeAndInventoryValues()
        {
            Assert.AreEqual(0x00, (byte)MessageCommand.Version);
            Assert.AreEqual(0x01, (byte)MessageCommand.Verack);
            Assert.AreEqual(0x10, (byte)MessageCommand.GetAddr);
            Assert.AreEqual(0x11, (byte)MessageCommand.Addr);
            Assert.AreEqual(0x18, (byte)MessageCommand.Ping);
            Assert.AreEqual(0x19, (byte)MessageCommand.Pong);
            Assert.AreEqual(0x20, (byte)MessageCommand.GetHeaders);
            Assert.AreEqual(0x21, (byte)MessageCommand.Headers);
            Assert.IsTrue(System.Enum.IsDefined(MessageCommand.Transaction));
            Assert.IsTrue(System.Enum.IsDefined(MessageCommand.Block));
            Assert.IsTrue(System.Enum.IsDefined(MessageCommand.Inv));
        }

        [TestMethod]
        public void UnhandledExceptionPolicy_Values()
        {
            Assert.AreEqual(0, (byte)UnhandledExceptionPolicy.Ignore);
            Assert.AreEqual(1, (byte)UnhandledExceptionPolicy.StopPlugin);
            Assert.AreEqual(2, (byte)UnhandledExceptionPolicy.StopNode);
        }
    }
}
