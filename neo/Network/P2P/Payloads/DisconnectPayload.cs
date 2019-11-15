using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public enum DisconnectReason : byte
    {
        MaxConnectionReached = 0x01,
        MaxConnectionPerAddressReached = 0x02,
        DuplicateNonce = 0x03,
        MagicNumberIncompatible = 0x04,
        ConnectionTimeout = 0x05,

        FormatException = 0x10,
        InternalError = 0x11,
    }

    public class DisconnectPayload : ISerializable
    {
        public const int MaxDataSize = 5120;

        public DisconnectReason Reason;
        public byte[] Data;

        public int Size => sizeof(DisconnectReason) + Data.GetVarSize();

        public static DisconnectPayload Create(DisconnectReason reason, byte[] data = null)
        {
            return new DisconnectPayload
            {
                Reason = reason,
                Data = data == null ? new byte[0] : data
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Reason = (DisconnectReason)reader.ReadByte();
            Data = reader.ReadVarBytes(MaxDataSize);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Reason);
            writer.WriteVarBytes(Data);
        }
    }
}
