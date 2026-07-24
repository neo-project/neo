// Copyright (C) 2015-2026 The Neo Project.
//
// UT_P2P_TypeEnums.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_P2P_TypeEnums
    {
        [TestMethod]
        public void InventoryType_MatchesMessageCommands()
        {
            Assert.AreEqual((byte)MessageCommand.Transaction, (byte)InventoryType.TX);
            Assert.AreEqual((byte)MessageCommand.Block, (byte)InventoryType.Block);
            Assert.AreEqual((byte)MessageCommand.Extensible, (byte)InventoryType.Extensible);
        }

        [TestMethod]
        public void TransactionAttributeType_Values()
        {
            Assert.AreEqual(0x01, (byte)TransactionAttributeType.HighPriority);
            Assert.AreEqual(0x11, (byte)TransactionAttributeType.OracleResponse);
            Assert.AreEqual(0x20, (byte)TransactionAttributeType.NotValidBefore);
            Assert.AreEqual(0x21, (byte)TransactionAttributeType.Conflicts);
            Assert.AreEqual(0x22, (byte)TransactionAttributeType.NotaryAssisted);
        }

        [TestMethod]
        public void NodeCapabilityType_Values()
        {
            Assert.AreEqual(0x01, (byte)NodeCapabilityType.TcpServer);
            Assert.AreEqual(0x03, (byte)NodeCapabilityType.DisableCompression);
            Assert.AreEqual(0x10, (byte)NodeCapabilityType.FullNode);
            Assert.AreEqual(0x11, (byte)NodeCapabilityType.ArchivalNode);
            Assert.AreEqual(0xf0, (byte)NodeCapabilityType.Extension0);
        }
    }
}
