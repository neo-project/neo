// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MessageCommand_Values.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;

namespace Neo.UnitTests.Network.P2P
{
    /// <summary>
    /// Full MessageCommand wire map (UT_P2P_Enums covers only a subset).
    /// </summary>
    [TestClass]
    public class UT_MessageCommand_Values
    {
        [TestMethod]
        public void AllDefinedValues_MatchSpecification()
        {
            Assert.AreEqual(0x00, (byte)MessageCommand.Version);
            Assert.AreEqual(0x01, (byte)MessageCommand.Verack);
            Assert.AreEqual(0x10, (byte)MessageCommand.GetAddr);
            Assert.AreEqual(0x11, (byte)MessageCommand.Addr);
            Assert.AreEqual(0x18, (byte)MessageCommand.Ping);
            Assert.AreEqual(0x19, (byte)MessageCommand.Pong);
            Assert.AreEqual(0x20, (byte)MessageCommand.GetHeaders);
            Assert.AreEqual(0x21, (byte)MessageCommand.Headers);
            Assert.AreEqual(0x24, (byte)MessageCommand.GetBlocks);
            Assert.AreEqual(0x25, (byte)MessageCommand.Mempool);
            Assert.AreEqual(0x27, (byte)MessageCommand.Inv);
            Assert.AreEqual(0x28, (byte)MessageCommand.GetData);
            Assert.AreEqual(0x29, (byte)MessageCommand.GetBlockByIndex);
            Assert.AreEqual(0x2a, (byte)MessageCommand.NotFound);
            Assert.AreEqual(0x2b, (byte)MessageCommand.Transaction);
            Assert.AreEqual(0x2c, (byte)MessageCommand.Block);
            Assert.AreEqual(0x2e, (byte)MessageCommand.Extensible);
            Assert.AreEqual(0x2f, (byte)MessageCommand.Reject);
            Assert.AreEqual(0x30, (byte)MessageCommand.FilterLoad);
            Assert.AreEqual(0x31, (byte)MessageCommand.FilterAdd);
            Assert.AreEqual(0x32, (byte)MessageCommand.FilterClear);
            Assert.AreEqual(0x38, (byte)MessageCommand.MerkleBlock);
            Assert.AreEqual(0x40, (byte)MessageCommand.Alert);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (MessageCommand command in System.Enum.GetValues<MessageCommand>())
                Assert.IsTrue(System.Enum.IsDefined(command));
        }
    }
}
