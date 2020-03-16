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
            public byte[] StateRootSignature;
            public byte[] InvocationScript;

            int ISerializable.Size =>
                sizeof(byte) +                  //ViewNumber
                sizeof(ushort) +                //ValidatorIndex
                Signature.Length +              //Signature
                StateRootSignature.Length +              //Signature
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ViewNumber = reader.ReadByte();
                ValidatorIndex = reader.ReadUInt16();
                Signature = reader.ReadBytes(64);
                StateRootSignature = reader.ReadBytes(64);
                InvocationScript = reader.ReadVarBytes(1024);
            }

            public static CommitPayloadCompact FromPayload(ConsensusPayload payload)
            {
                Commit message = payload.GetDeserializedMessage<Commit>();
                return new CommitPayloadCompact
                {
                    ViewNumber = message.ViewNumber,
                    ValidatorIndex = payload.ValidatorIndex,
                    Signature = message.Signature,
                    StateRootSignature = message.StateRootSignature,
                    InvocationScript = payload.Witness.InvocationScript
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ViewNumber);
                writer.Write(ValidatorIndex);
                writer.Write(Signature);
                writer.Write(StateRootSignature);
                writer.WriteVarBytes(InvocationScript);
            }
        }
    }
}
