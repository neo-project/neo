using Neo.IO;
using System.IO;

namespace Neo.Network.Payloads
{
    public class GetBlocksPayload : ISerializable
    {
        public UInt256[] HashStart;
        public UInt256 HashStop;

        public int Size => HashStart.GetVarSize() + HashStop.Size;

        public static GetBlocksPayload Create(UInt256 hash_start, UInt256 hash_stop = null)
        {
            return new GetBlocksPayload
            {
                HashStart = new[] { hash_start },
                HashStop = hash_stop ?? UInt256.Zero
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            HashStart = reader.ReadSerializableArray<UInt256>(16);
            HashStop = reader.ReadSerializable<UInt256>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(HashStart);
            writer.Write(HashStop);
        }
    }
}
