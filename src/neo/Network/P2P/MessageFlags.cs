using System;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Represents the flags of a message.
    /// </summary>
    [Flags]
    public enum MessageFlags : byte
    {
        /// <summary>
        /// No flag is set for the message.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the message is compressed.
        /// </summary>
        Compressed = 1 << 0
    }
}
