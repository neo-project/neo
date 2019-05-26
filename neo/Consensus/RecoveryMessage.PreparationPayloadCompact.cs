using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class PreparationPayloadCompact : ISerializable
        {
            public ushort ValidatorIndex;
            public byte[] PayloadSignature;

            int ISerializable.Size =>
                sizeof(ushort) +            //ValidatorIndex
                PayloadSignature.Length;    //PayloadSignature

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadUInt16();
                PayloadSignature = reader.ReadBytes(64);
            }

            public static PreparationPayloadCompact FromPayload(ConsensusPayload payload)
            {
                return new PreparationPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    PayloadSignature = payload.Signature
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.Write(PayloadSignature);
            }
        }
    }
}
