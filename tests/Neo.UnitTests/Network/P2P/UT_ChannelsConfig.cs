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
            config.MinDesiredConnections.Should().Be(10);
            config.MaxConnections.Should().Be(40);
            config.MaxConnectionsPerAddress.Should().Be(3);

            config.Tcp = config.Tcp = new IPEndPoint(IPAddress.Any, 21);
            config.MaxConnectionsPerAddress++;
            config.MaxConnections++;
            config.MinDesiredConnections++;

            config.Tcp.Should().BeSameAs(config.Tcp);
            config.Tcp.Address.Should().BeEquivalentTo(IPAddress.Any);
            config.Tcp.Port.Should().Be(21);
            config.MinDesiredConnections.Should().Be(11);
            config.MaxConnections.Should().Be(41);
            config.MaxConnectionsPerAddress.Should().Be(4);
        }
    }
}
