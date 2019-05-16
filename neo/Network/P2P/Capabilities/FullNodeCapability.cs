using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class FullNodeCapability : NodeCapabilityBase
    {
        public uint StartHeight { get; set; }

        public override int Size =>
            base.Size +    // Type
            sizeof(uint);  // Start Height

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startHeight">Start Height</param>
        public FullNodeCapability(uint startHeight = 0) : base(NodeCapabilities.FullNode)
        {
            StartHeight = startHeight;
        }

        public override void DeserializeWithoutType(BinaryReader reader)
        {
            base.DeserializeWithoutType(reader);
            StartHeight = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(StartHeight);
        }
    }
}