// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RoutingTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P;
using System.Net;

namespace Neo.UnitTests.Network.P2P;

[TestClass]
public class UT_RoutingTable
{
    [TestMethod]
    public void FindClosest_ReturnsContactsOrderedByXorDistance()
    {
        var table = new RoutingTable(UInt256.Zero, bucketSize: 2, replacementSize: 0, badThreshold: 1);
        UInt256 target = UInt256.Zero;

        Add(table, WithBit(3), 10003);
        Add(table, WithBit(1), 10001);
        Add(table, WithBit(2), 10002);
        Add(table, WithBit(0), 10000);

        IReadOnlyList<NodeContact> result = table.FindClosest(target, 3);

        Assert.HasCount(3, result);
        Assert.AreEqual(WithBit(0), result[0].NodeId);
        Assert.AreEqual(WithBit(1), result[1].NodeId);
        Assert.AreEqual(WithBit(2), result[2].NodeId);
    }

    [TestMethod]
    public void FindClosest_ConsidersAllBucketsWhenLowerBucketIsCloser()
    {
        var table = new RoutingTable(UInt256.Zero, bucketSize: 1, replacementSize: 0, badThreshold: 1);
        UInt256 target = WithBit(128);
        UInt256 closest = WithBit(0);

        for (int bit = 127; bit >= 124; bit--)
            Add(table, WithBit(bit), 10000 + bit);
        for (int bit = 129; bit <= 132; bit++)
            Add(table, WithBit(bit), 10000 + bit);
        Add(table, closest, 10000);

        IReadOnlyList<NodeContact> result = table.FindClosest(target, 1);

        Assert.HasCount(1, result);
        Assert.AreEqual(closest, result[0].NodeId);
    }

    private static void Add(RoutingTable table, UInt256 nodeId, int port)
    {
        table.Update(nodeId, new OverlayEndpoint(
            TransportProtocol.Tcp,
            new IPEndPoint(IPAddress.Loopback, port),
            EndpointKind.Observed));
    }

    private static UInt256 WithBit(int bit)
    {
        var bytes = new byte[UInt256.Length];
        bytes[bit / 8] = (byte)(1 << (bit % 8));
        return new UInt256(bytes);
    }
}
