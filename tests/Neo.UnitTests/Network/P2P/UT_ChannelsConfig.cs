// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ChannelsConfig.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System.Net;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_ChannelsConfig
    {
        [TestMethod]
        public void CreateTest()
        {
            var config = new ChannelsConfig();

            Assert.IsNull(config.Tcp);
            Assert.AreEqual(10, config.MinDesiredConnections);
            Assert.AreEqual(40, config.MaxConnections);
            Assert.AreEqual(3, config.MaxConnectionsPerAddress);

            config.Tcp = new IPEndPoint(IPAddress.Any, 21);
            config.MaxConnectionsPerAddress++;
            config.MaxConnections++;
            config.MinDesiredConnections++;

            Assert.AreSame(config.Tcp, config.Tcp);
            CollectionAssert.AreEqual(IPAddress.Any.GetAddressBytes(), config.Tcp.Address.GetAddressBytes());
            Assert.AreEqual(21, config.Tcp.Port);
            Assert.AreEqual(11, config.MinDesiredConnections);
            Assert.AreEqual(41, config.MaxConnections);
            Assert.AreEqual(4, config.MaxConnectionsPerAddress);
        }
    }
}
