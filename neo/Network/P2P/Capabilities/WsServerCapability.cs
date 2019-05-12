namespace Neo.Network.P2P.Capabilities
{
    public class WsServerCapability : ServerCapability
    {
        public WsServerCapability() : base(NodeCapabilities.WsServer) { }

        public WsServerCapability(ushort port) : base(NodeCapabilities.WsServer, port) { }
    }
}