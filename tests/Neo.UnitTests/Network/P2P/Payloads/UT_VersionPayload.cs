// Copyright (C) 2015-2026 The Neo Project.
//
// UT_VersionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Extensions.Collections;
using Neo.Extensions.IO;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads;

[TestClass]
public class UT_VersionPayload
{
    [TestMethod]
    public void SizeAndEndPoint_Get()
    {
        var test = VersionPayload.Create(ProtocolSettings.Default, new(), "neo3");
        Assert.AreEqual(148, test.Size);

        test = VersionPayload.Create(ProtocolSettings.Default, new(), "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
        Assert.AreEqual(151, test.Size);
    }

    [TestMethod]
    public void DeserializeAndSerialize()
    {
        var test = VersionPayload.Create(ProtocolSettings.Default, new(), "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
        var clone = test.ToArray().AsSerializable<VersionPayload>();

        CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());
        Assert.AreEqual(test.UserAgent, clone.UserAgent);
        Assert.AreEqual(test.NodeId, clone.NodeId);
        Assert.AreEqual(test.Timestamp, clone.Timestamp);
        CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());

        Assert.ThrowsExactly<FormatException>(() => _ = VersionPayload.Create(ProtocolSettings.Default, new(), "neo3",
            new NodeCapability[] {
                new ServerCapability(NodeCapabilityType.TcpServer, 22) ,
                new ServerCapability(NodeCapabilityType.TcpServer, 22)
            }).ToArray().AsSerializable<VersionPayload>());
    }
}
