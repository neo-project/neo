using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class ChangeViewPayloadCompact : ISerializable
        {
            public ushort ValidatorIndex;
            public byte OriginalViewNumber;
            public uint Timestamp;
            public byte[] PayloadSignature;

            int ISerializable.Size =>
                sizeof(ushort) +            //ValidatorIndex
                sizeof(byte) +              //OriginalViewNumber
                sizeof(uint) +              //Timestamp
                PayloadSignature.Length;    //PayloadSignature

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadUInt16();
                OriginalViewNumber = reader.ReadByte();
                Timestamp = reader.ReadUInt32();
                PayloadSignature = reader.ReadBytes(64);
            }

            public static ChangeViewPayloadCompact FromPayload(ConsensusPayload payload)
            {
                ChangeView message = payload.GetDeserializedMessage<ChangeView>();
                return new ChangeViewPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    OriginalViewNumber = message.ViewNumber,
                    Timestamp = message.Timestamp,
                    PayloadSignature = payload.Signature
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.Write(OriginalViewNumber);
                writer.Write(Timestamp);
                writer.Write(PayloadSignature);
            }
        }
    }
}
