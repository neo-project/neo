namespace Neo.Network.P2P.Capabilities
{
    public class UdpServerCapability : ServerCapability
    {
        public UdpServerCapability() : base(NodeCapabilities.UdpServer) { }

        public UdpServerCapability(ushort port) : base(NodeCapabilities.UdpServer, port) { }
    }
}