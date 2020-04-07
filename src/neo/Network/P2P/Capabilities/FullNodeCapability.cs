using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class FullNodeCapability : NodeCapability
    {
        public uint StartHeight;
        public int MemPoolCount;

        public override int Size =>
            base.Size +    // Type
            sizeof(uint) + // Start Height
            sizeof(uint);  // MemPool count

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startHeight">Start Height</param>
        /// <param name="memPoolCount">Mem pool count</param>
        public FullNodeCapability(uint startHeight = 0, int memPoolCount = 0) : base(NodeCapabilityType.FullNode)
        {
            StartHeight = startHeight;
            MemPoolCount = memPoolCount;
        }

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            StartHeight = reader.ReadUInt32();
            MemPoolCount = reader.ReadInt32();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(StartHeight);
            writer.Write(MemPoolCount);
        }
    }
}
