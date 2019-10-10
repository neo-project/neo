using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public enum DisconnectionReason : byte
    {
        MaxConnectionReached = 0x01,
        MaxPerAddressConnectionReached = 0x02,
        DuplicateConnection = 0x03,
        MagicNumberIncompatible = 0x04,
        ConnectionTimeout = 0x05,
        UntrustedIpAddresses = 0x06, 

        FormatExcpetion = 0x10,
        InternalError = 0x11,
    }

    public class DisconnectionPayload : ISerializable
    {

        public const int MaxDataSize = 5120;

        public DisconnectionReason Reason;
        public string Message;
        public byte[] Data;

        public int Size => sizeof(DisconnectionReason) + Message.GetVarSize() + Data.GetVarSize();


        public static DisconnectionPayload Create(DisconnectionReason reason, string message = "", byte[] data = null)
        {
            return new DisconnectionPayload
            {
                Reason = reason,
                Message = message,
                Data = data == null ? new byte[0] : data
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Reason = (DisconnectionReason) reader.ReadByte();
            Message = reader.ReadString();
            Data = reader.ReadVarBytes(MaxDataSize);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Reason);
            writer.Write(Message);
            writer.WriteVarBytes(Data);
        }
    }
}
