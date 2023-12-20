// Copyright (C) 2015-2024 The Neo Project.
//
// UT_AddrPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;
using System.Net;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_AddrPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new AddrPayload() { AddressList = new NetworkAddressWithTime[0] };
            test.Size.Should().Be(1);

            test = AddrPayload.Create(new NetworkAddressWithTime[] { new NetworkAddressWithTime() { Address = IPAddress.Any, Capabilities = new Neo.Network.P2P.Capabilities.NodeCapability[0], Timestamp = 1 } });
            test.Size.Should().Be(22);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = AddrPayload.Create(new NetworkAddressWithTime[] { new NetworkAddressWithTime()
            {
                Address = IPAddress.Any,
                Capabilities = new Neo.Network.P2P.Capabilities.NodeCapability[0], Timestamp = 1
            }
            });
            var clone = test.ToArray().AsSerializable<AddrPayload>();

            CollectionAssert.AreEqual(test.AddressList.Select(u => u.EndPoint).ToArray(), clone.AddressList.Select(u => u.EndPoint).ToArray());

            Assert.ThrowsException<FormatException>(() => new AddrPayload() { AddressList = new NetworkAddressWithTime[0] }.ToArray().AsSerializable<AddrPayload>());
        }
    }
}
