// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MessageFlags.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_MessageFlags
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(0, (byte)MessageFlags.None);
            Assert.AreEqual(1, (byte)MessageFlags.Compressed);
        }

        [TestMethod]
        public void Flags_CanBeCombined()
        {
            var flags = MessageFlags.None | MessageFlags.Compressed;
            Assert.IsTrue(flags.HasFlag(MessageFlags.Compressed));
            Assert.AreEqual(MessageFlags.Compressed, flags & MessageFlags.Compressed);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (MessageFlags flag in Enum.GetValues<MessageFlags>())
                Assert.IsTrue(Enum.IsDefined(flag));
        }
    }
}
