using Neo.Network.P2P;

namespace Neo
{
    public class NodeStartConfig
    {
        /// <summary>
        /// Tcp configuration
        /// </summary>
        public EndPointConfig Tcp { get; set; } = new EndPointConfig() { };

        /// <summary>
        /// Udp configuration
        /// </summary>
        public EndPointConfig Udp { get; set; } = new EndPointConfig() { };

        /// <summary>
        /// Web socket configuration
        /// </summary>
        public EndPointConfig WebSocket { get; set; } = new EndPointConfig() { };

        /// <summary>
        /// Minimum desired connections
        /// </summary>
        public int MinDesiredConnections { get; set; } = Peer.DefaultMinDesiredConnections;

        /// <summary>
        /// Maxx allowed connections
        /// </summary>
        public int MaxConnections { get; set; } = Peer.DefaultMaxConnections;

        /// <summary>
        /// Maxx allowed connections per address
        /// </summary>
        public int MaxConnectionsPerAddress { get; set; } = 3;
    }
}