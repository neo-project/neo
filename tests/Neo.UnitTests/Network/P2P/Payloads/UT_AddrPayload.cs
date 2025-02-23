// Copyright (C) 2015-2025 The Neo Project.
//
// UT_AddrPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Capabilities;
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
            Assert.AreEqual(1, test.Size);

            test = AddrPayload.Create(new NetworkAddressWithTime[] { new NetworkAddressWithTime() { Address = IPAddress.Any, Capabilities = new NodeCapability[0], Timestamp = 1 } });
            Assert.AreEqual(22, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = AddrPayload.Create(new NetworkAddressWithTime[] { new NetworkAddressWithTime()
            {
                Address = IPAddress.Any,
                Capabilities = new NodeCapability[0], Timestamp = 1
            }
            });
            var clone = test.ToArray().AsSerializable<AddrPayload>();

            CollectionAssert.AreEqual(test.AddressList.Select(u => u.EndPoint).ToArray(), clone.AddressList.Select(u => u.EndPoint).ToArray());

            Assert.ThrowsExactly<FormatException>(() => _ = new AddrPayload() { AddressList = new NetworkAddressWithTime[0] }.ToArray().AsSerializable<AddrPayload>());
        }
    }
}
