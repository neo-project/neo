using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class CommitPayloadCompact : ISerializable
        {
            public byte ViewNumber;
            public ushort ValidatorIndex;
            public byte[] Signature;
            public byte[] PayloadSignature;

            int ISerializable.Size =>
                sizeof(byte) +              //ViewNumber
                sizeof(ushort) +            //ValidatorIndex
                Signature.Length +          //Signature
                PayloadSignature.Length;    //PayloadSignature

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ViewNumber = reader.ReadByte();
                ValidatorIndex = reader.ReadUInt16();
                Signature = reader.ReadBytes(64);
                PayloadSignature = reader.ReadBytes(64);
            }

            public static CommitPayloadCompact FromPayload(ConsensusPayload payload)
            {
                Commit message = payload.GetDeserializedMessage<Commit>();
                return new CommitPayloadCompact
                {
                    ViewNumber = message.ViewNumber,
                    ValidatorIndex = payload.ValidatorIndex,
                    Signature = message.Signature,
                    PayloadSignature = payload.Signature
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ViewNumber);
                writer.Write(ValidatorIndex);
                writer.Write(Signature);
                writer.Write(PayloadSignature);
            }
        }
    }
}
