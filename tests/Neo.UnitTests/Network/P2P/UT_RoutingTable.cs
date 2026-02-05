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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System;
using System.Linq;
using System.Net;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_RoutingTable
    {
        [TestMethod]
        public void Constructor_RejectsInvalidBucketConfiguration()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RoutingTable(UInt256.Zero, bucketSize: 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RoutingTable(UInt256.Zero, replacementSize: -1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RoutingTable(UInt256.Zero, badThreshold: 0));
        }

        [TestMethod]
        public void Update_FindClosest_Remove_And_Sample_WorkAcrossBuckets()
        {
            var table = new RoutingTable(UInt256.Zero, bucketSize: 2, replacementSize: 1, badThreshold: 1);
            var ids = new[] { WithBit(0), WithBit(1), WithBit(2), WithBit(3) };

            Assert.IsFalse(table.Update(UInt256.Zero, Endpoint(10000, EndpointKind.Observed)));

            for (int i = 0; i < ids.Length; i++)
                Assert.IsTrue(table.Update(ids[i], Endpoint(10001 + i, EndpointKind.Observed), (ulong)(1 << i)));

            var closest = table.FindClosest(UInt256.Zero, 3);
            Assert.HasCount(3, closest);
            Assert.AreEqual(ids[0], closest[0].NodeId);
            Assert.AreEqual(ids[1], closest[1].NodeId);
            Assert.AreEqual(ids[2], closest[2].NodeId);

            Assert.IsEmpty(table.FindClosest(UInt256.Zero, 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => table.FindClosest(UInt256.Zero, -1));

            var sample = table.Sample(10);
            Assert.HasCount(4, sample);
            Assert.AreEqual(4, sample.Select(p => p.NodeId).Distinct().Count());
            Assert.IsEmpty(table.Sample(0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => table.Sample(-1));

            table.Remove(ids[1]);
            Assert.IsFalse(table.FindClosest(UInt256.Zero, 10).Any(p => p.NodeId == ids[1]));
        }

        [TestMethod]
        public void MarkFailure_PromotesMostRecentReplacement()
        {
            var table = new RoutingTable(UInt256.Zero, bucketSize: 1, replacementSize: 2, badThreshold: 2);
            var primary = WithBits(5);
            var replacement = WithBits(5, 0);
            var newestReplacement = WithBits(5, 1);

            Assert.IsTrue(table.Update(primary, Endpoint(10001, EndpointKind.Observed)));
            Assert.IsFalse(table.Update(replacement, Endpoint(10002, EndpointKind.Observed)));
            Assert.IsFalse(table.Update(newestReplacement, Endpoint(10003, EndpointKind.Observed)));

            table.MarkFailure(primary);
            Assert.AreEqual(primary, table.FindClosest(primary, 1)[0].NodeId);

            table.MarkFailure(primary);
            Assert.AreEqual(newestReplacement, table.FindClosest(primary, 1)[0].NodeId);
        }

        [TestMethod]
        public void MarkSuccess_ResetsFailureCountBeforeEviction()
        {
            var table = new RoutingTable(UInt256.Zero, bucketSize: 1, replacementSize: 1, badThreshold: 2);
            var primary = WithBits(6);
            var replacement = WithBits(6, 0);

            table.Update(primary, Endpoint(10001, EndpointKind.Observed));
            table.Update(replacement, Endpoint(10002, EndpointKind.Observed));

            table.MarkFailure(primary);
            table.MarkSuccess(primary);
            table.MarkFailure(primary);

            Assert.AreEqual(primary, table.FindClosest(primary, 1)[0].NodeId);
        }

        [TestMethod]
        public void KBucket_UpdatesExistingContactsAndDropsFailedReplacements()
        {
            var bucket = new KBucket(capacity: 1, replacementCapacity: 1, badThreshold: 1);
            var primary = WithBits(7);
            var replacement = WithBits(7, 0);

            Assert.IsTrue(bucket.Update(new NodeContact(primary, [Endpoint(10001, EndpointKind.Observed)], 1)));
            Assert.IsFalse(bucket.Update(new NodeContact(replacement, [Endpoint(10002, EndpointKind.Observed)], 2)));

            Assert.IsTrue(bucket.TryGet(primary, out var contact));
            Assert.AreEqual(1ul, contact.Features);

            bucket.Update(new NodeContact(primary, [Endpoint(10001, EndpointKind.Advertised)], 4));
            Assert.IsTrue(bucket.TryGet(primary, out contact));
            Assert.AreEqual(5ul, contact.Features);
            Assert.HasCount(1, contact.Endpoints);
            Assert.AreEqual(EndpointKind.Observed | EndpointKind.Advertised, contact.Endpoints[0].Kind);

            bucket.MarkFailure(replacement);
            bucket.MarkFailure(primary);
            Assert.IsFalse(bucket.TryGet(primary, out _));
            Assert.IsFalse(bucket.TryGet(replacement, out _));
        }

        [TestMethod]
        public void NodeContact_MergesPromotesAndTrimsEndpoints()
        {
            var nodeId = WithBit(8);
            var observed = Endpoint(10001, EndpointKind.Observed);
            var advertised = observed.WithKind(EndpointKind.Advertised);
            var derived = Endpoint(10002, EndpointKind.Derived);
            var relay = Endpoint(10003, EndpointKind.Relay);
            var unknown = Endpoint(10004, 0);
            var secondAdvertised = Endpoint(10005, EndpointKind.Advertised);

            var contact = new NodeContact(nodeId, [observed], features: 1);
            contact.AddOrPromoteEndpoint(advertised);
            Assert.HasCount(1, contact.Endpoints);
            Assert.AreEqual(EndpointKind.Observed | EndpointKind.Advertised, contact.Endpoints[0].Kind);

            contact.AddOrPromoteEndpoint(derived);
            contact.AddOrPromoteEndpoint(relay);
            contact.AddOrPromoteEndpoint(unknown);
            contact.AddOrPromoteEndpoint(secondAdvertised);

            Assert.HasCount(4, contact.Endpoints);
            Assert.AreEqual(secondAdvertised, contact.Endpoints[0]);
            Assert.IsFalse(contact.Endpoints.Any(p => p.EndPoint.Port == relay.EndPoint.Port));
            Assert.StartsWith(nodeId.ToString(), contact.ToString());
        }

        [TestMethod]
        public void OverlayEndpoint_EqualityIgnoresKind()
        {
            var observed = Endpoint(10001, EndpointKind.Observed);
            var advertised = observed.WithKind(EndpointKind.Advertised);
            var differentPort = Endpoint(10002, EndpointKind.Observed);

            Assert.AreEqual(TransportProtocol.Tcp, observed.Transport);
            Assert.AreEqual(EndpointKind.Observed, observed.Kind);
            Assert.AreEqual(observed, advertised);
            Assert.IsTrue(observed == advertised);
            Assert.IsFalse(observed != advertised);
            Assert.AreEqual(observed.GetHashCode(), advertised.GetHashCode());
            Assert.AreNotEqual(observed, differentPort);
            Assert.AreNotEqual(observed, new object());
            Assert.AreEqual("tcp:127.0.0.1:10001", observed.ToString());
        }

        private static OverlayEndpoint Endpoint(int port, EndpointKind kind)
        {
            return new OverlayEndpoint(
                TransportProtocol.Tcp,
                new IPEndPoint(IPAddress.Loopback, port),
                kind);
        }

        private static UInt256 WithBit(int bit)
        {
            var bytes = new byte[UInt256.Length];
            bytes[bit / 8] = (byte)(1 << (bit % 8));
            return new UInt256(bytes);
        }

        private static UInt256 WithBits(params int[] bits)
        {
            var bytes = new byte[UInt256.Length];
            foreach (int bit in bits)
                bytes[bit / 8] |= (byte)(1 << (bit % 8));
            return new UInt256(bytes);
        }
    }
}
