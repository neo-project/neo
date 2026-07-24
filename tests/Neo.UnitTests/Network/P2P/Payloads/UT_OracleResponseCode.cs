// Copyright (C) 2015-2026 The Neo Project.
//
// UT_OracleResponseCode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OracleResponseCode
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(0x00, (byte)OracleResponseCode.Success);
            Assert.AreEqual(0x10, (byte)OracleResponseCode.ProtocolNotSupported);
            Assert.AreEqual(0x12, (byte)OracleResponseCode.ConsensusUnreachable);
            Assert.AreEqual(0x14, (byte)OracleResponseCode.NotFound);
            Assert.AreEqual(0x16, (byte)OracleResponseCode.Timeout);
            Assert.AreEqual(0x18, (byte)OracleResponseCode.Forbidden);
            Assert.AreEqual(0x1a, (byte)OracleResponseCode.ResponseTooLarge);
            Assert.AreEqual(0x1c, (byte)OracleResponseCode.InsufficientFunds);
            Assert.AreEqual(0x1f, (byte)OracleResponseCode.ContentTypeNotSupported);
            Assert.AreEqual(0xff, (byte)OracleResponseCode.Error);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (OracleResponseCode code in System.Enum.GetValues<OracleResponseCode>())
                Assert.IsTrue(System.Enum.IsDefined(code));
        }
    }
}
