namespace Neo.Network.P2P.Capabilities
{
    public class TcpServerCapability : ServerCapability
    {
        public TcpServerCapability() : base(NodeCapabilities.TcpServer) { }

        public TcpServerCapability(ushort port) : base(NodeCapabilities.TcpServer, port) { }
    }
}