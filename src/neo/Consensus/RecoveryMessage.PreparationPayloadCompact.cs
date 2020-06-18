using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class PreparationPayloadCompact : ISerializable
        {
            public ushort ValidatorIndex;
            public byte[] InvocationScript;
            public byte[] StateRootSignature;

            int ISerializable.Size =>
                sizeof(ushort) +                    //ValidatorIndex
                InvocationScript.GetVarSize() +     //InvocationScript
                StateRootSignature.Length;          //StateRootSignature

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadUInt16();
                InvocationScript = reader.ReadVarBytes(1024);
                StateRootSignature = reader.ReadFixedBytes(64);
            }

            public static PreparationPayloadCompact FromPayload(ConsensusPayload payload)
            {
                byte[] state_root_sig = Array.Empty<byte>();
                if (payload.ConsensusMessage is PrepareResponse req)
                    state_root_sig = req.StateRootSignature;
                else if (payload.ConsensusMessage is PrepareResponse resp)
                    state_root_sig = resp.StateRootSignature;
                return new PreparationPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    InvocationScript = payload.Witness.InvocationScript,
                    StateRootSignature = state_root_sig
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.WriteVarBytes(InvocationScript);
                writer.Write(StateRootSignature);
            }
        }
    }
}
