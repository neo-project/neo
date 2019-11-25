using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class FullNodeCapability : NodeCapability
    {
        public uint StartHeight;

        public override int Size =>
            base.Size +    // Type
            sizeof(uint);  // Start Height

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startHeight">Start Height</param>
        public FullNodeCapability(uint startHeight = 0) : base(NodeCapabilityType.FullNode)
        {
            StartHeight = startHeight;
        }

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            StartHeight = reader.ReadUInt32();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(StartHeight);
        }
    }
}
