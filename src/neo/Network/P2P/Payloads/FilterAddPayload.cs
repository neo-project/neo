using Neo.Cryptography;
using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to update the items for the <see cref="BloomFilter"/>.
    /// </summary>
    public class FilterAddPayload : ISerializable
    {
        /// <summary>
        /// The items to be added.
        /// </summary>
        public byte[] Data;

        public int Size => Data.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Data = reader.ReadVarBytes(520);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Data);
        }
    }
}
