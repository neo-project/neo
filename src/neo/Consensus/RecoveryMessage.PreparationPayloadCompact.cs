using Neo.IO;
using System;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class PreparationPayloadCompact : ISerializable
        {
            public byte ValidatorIndex;
            public byte[] InvocationScript;

            int ISerializable.Size =>
                sizeof(byte) +                  //ValidatorIndex
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadByte();
                if (ValidatorIndex >= ProtocolSettings.Default.ValidatorsCount)
                    throw new FormatException();
                InvocationScript = reader.ReadVarBytes(1024);
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.WriteVarBytes(InvocationScript);
            }
        }
    }
}
