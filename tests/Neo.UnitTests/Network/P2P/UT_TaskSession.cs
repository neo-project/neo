// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TaskSession.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P;

[TestClass]
public class UT_TaskSession
{
    [TestMethod]
    public void CreateTest()
    {
        var ses = new TaskSession(VersionPayload.Create(ProtocolSettings.Default, ECCurve.Secp256r1.G, "", new FullNodeCapability(123)));

        Assert.IsFalse(ses.HasTooManyTasks);
        Assert.AreEqual((uint)123, ses.LastBlockIndex);
        Assert.IsEmpty(ses.IndexTasks);
        Assert.IsTrue(ses.IsFullNode);

        ses = new TaskSession(VersionPayload.Create(ProtocolSettings.Default, ECCurve.Secp256r1.G, ""));

        Assert.IsFalse(ses.HasTooManyTasks);
        Assert.AreEqual((uint)0, ses.LastBlockIndex);
        Assert.IsEmpty(ses.IndexTasks);
        Assert.IsFalse(ses.IsFullNode);
    }
}
