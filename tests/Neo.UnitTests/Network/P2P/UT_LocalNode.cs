// Copyright (C) 2015-2026 The Neo Project.
//
// UT_LocalNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Akka.TestKit.MsTest;
using Neo.Network.P2P;
using System.Net;

namespace Neo.UnitTests.Network.P2P;

[TestClass]
public class UT_LocalNode : TestKit
{
    private static NeoSystem _system = null!;

    [TestInitialize]
    public void Init()
    {
        _system = TestBlockchain.GetSystem();
    }

    [TestMethod]
    public void TestDefaults()
    {
        var senderProbe = CreateTestProbe();
        senderProbe.Send(_system.LocalNode, new ChannelsConfig()); // No Tcp
        senderProbe.Send(_system.LocalNode, new LocalNode.GetInstance());
        var localnode = senderProbe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

        Assert.AreEqual(0, localnode.ListenerTcpPort);
        Assert.AreEqual(3, localnode.Config.MaxConnectionsPerAddress);
        Assert.AreEqual(10, localnode.Config.MinDesiredConnections);
        Assert.AreEqual(40, localnode.Config.MaxConnections);
        Assert.AreEqual(0, localnode.UnconnectedCount);
    }

    [TestMethod]
    public void ProcessesTcpConnectedAfterConfigArrives()
    {
        var connectionProbe = CreateTestProbe();
        var remote = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 20333);
        var local = new IPEndPoint(IPAddress.Loopback, 20334);

        connectionProbe.Send(_system.LocalNode, new Tcp.Connected(remote, local));
        connectionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(200), cancellationToken: CancellationToken.None);

        var configProbe = CreateTestProbe();
        configProbe.Send(_system.LocalNode, new ChannelsConfig());

        connectionProbe.ExpectMsg<Tcp.Register>(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None);
    }
}
