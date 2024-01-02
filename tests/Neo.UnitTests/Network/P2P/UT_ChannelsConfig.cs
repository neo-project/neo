// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ChannelsConfig.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
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

            config.Tcp.Should().BeNull();
            config.WebSocket.Should().BeNull();
            config.MinDesiredConnections.Should().Be(10);
            config.MaxConnections.Should().Be(40);
            config.MaxConnectionsPerAddress.Should().Be(3);

            config.Tcp = config.WebSocket = new IPEndPoint(IPAddress.Any, 21);
            config.MaxConnectionsPerAddress++;
            config.MaxConnections++;
            config.MinDesiredConnections++;

            config.Tcp.Should().BeSameAs(config.WebSocket);
            config.Tcp.Address.Should().BeEquivalentTo(IPAddress.Any);
            config.Tcp.Port.Should().Be(21);
            config.MinDesiredConnections.Should().Be(11);
            config.MaxConnections.Should().Be(41);
            config.MaxConnectionsPerAddress.Should().Be(4);
        }
    }
}
