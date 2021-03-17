using Neo.Network.P2P;

namespace Neo.Plugins
{
    /// <summary>
    /// An interface that allows plugins to observe the messages on the network.
    /// </summary>
    public interface IP2PPlugin
    {
        /// <summary>
        /// Called when a message is received from a remote node.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the local node.</param>
        /// <param name="message">The received message.</param>
        /// <returns><see langword="false"/> if the <paramref name="message"/> should be dropped; otherwise, <see langword="true"/>.</returns>
        bool OnP2PMessage(NeoSystem system, Message message) => true;
    }
}
