using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class DisconnectPayload : ISerializable
    {
        public const int MaxDataSize = 1024;

        public DisconnectReason Reason;
        public byte[] Data;

        public int Size => sizeof(DisconnectReason) + Data.GetVarSize();

        public static DisconnectPayload Create(DisconnectReason reason, byte[] data = null)
        {
            return new DisconnectPayload
            {
                Reason = reason,
                Data = data ?? Array.Empty<byte>()
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
