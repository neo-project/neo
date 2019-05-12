using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class AcceptRelayCapability : NodeCapabilityBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AcceptRelayCapability() : base(NodeCapabilities.AcceptRelay) { }

        public override void Deserialize(BinaryReader reader) => base.Deserialize(reader);

        public override void Serialize(BinaryWriter writer) => base.Serialize(writer);
    }
}