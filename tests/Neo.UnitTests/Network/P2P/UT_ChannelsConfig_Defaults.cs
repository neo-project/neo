// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ChannelsConfig_Defaults.cs file belongs to the neo project and is free
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
    /// Default constant coverage not asserted in UT_ChannelsConfig.CreateTest.
    /// </summary>
    [TestClass]
    public class UT_ChannelsConfig_Defaults
    {
        [TestMethod]
        public void DefaultConstants_MatchPublicFields()
        {
            Assert.IsTrue(ChannelsConfig.DefaultEnableCompression);
            Assert.AreEqual(10, ChannelsConfig.DefaultMinDesiredConnections);
            Assert.AreEqual(40, ChannelsConfig.DefaultMaxConnections);
            Assert.AreEqual(3, ChannelsConfig.DefaultMaxConnectionsPerAddress);
            Assert.AreEqual(1000, ChannelsConfig.DefaultMaxKnownHashes);
        }

        [TestMethod]
        public void NewInstance_UsesCompressionAndKnownHashesDefaults()
        {
            var config = new ChannelsConfig();
            Assert.AreEqual(ChannelsConfig.DefaultEnableCompression, config.EnableCompression);
            Assert.AreEqual(ChannelsConfig.DefaultMaxKnownHashes, config.MaxKnownHashes);
        }

        [TestMethod]
        public void EnableCompression_CanBeDisabled()
        {
            var config = new ChannelsConfig { EnableCompression = false };
            Assert.IsFalse(config.EnableCompression);
        }

        [TestMethod]
        public void MessageFlags_Compressed_IsDistinctFromNone()
        {
            // Avoid constant-to-constant Assert.AreEqual (MSTEST0025).
            Assert.AreNotEqual(MessageFlags.None, MessageFlags.Compressed);
            Assert.IsTrue(MessageFlags.Compressed.HasFlag(MessageFlags.Compressed));
            Assert.IsFalse(MessageFlags.None.HasFlag(MessageFlags.Compressed));
            Assert.IsTrue((byte)default(MessageFlags) == (byte)MessageFlags.None);
            Assert.IsTrue(((int)MessageFlags.Compressed & 1) == 1);
        }
    }
}
