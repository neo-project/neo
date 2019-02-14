using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class CommitPayloadCompact : ISerializable
        {
            public ushort ValidatorIndex;
            public byte[] Signature;
            //public byte[] InvocationScript;

            int ISerializable.Size =>
                sizeof(ushort) +    //ValidatorIndex
                Signature.Length;   //Signature

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadUInt16();
                Signature = reader.ReadBytes(64);
            }

            public static CommitPayloadCompact FromPayload(ConsensusPayload payload)
            {
                Commit message = payload.GetDeserializedMessage<Commit>();
                return new CommitPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    Signature = message.Signature,
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.Write(Signature);
            }
        }
    }
}
