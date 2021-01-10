using Neo.IO;
using System;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class ChangeViewPayloadCompact : ISerializable
        {
            public byte ValidatorIndex;
            public byte OriginalViewNumber;
            public ulong Timestamp;
            public byte[] InvocationScript;

            int ISerializable.Size =>
                sizeof(byte) +                  //ValidatorIndex
                sizeof(byte) +                  //OriginalViewNumber
                sizeof(ulong) +                 //Timestamp
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadByte();
                if (ValidatorIndex >= ProtocolSettings.Default.ValidatorsCount)
                    throw new FormatException();
                OriginalViewNumber = reader.ReadByte();
                Timestamp = reader.ReadUInt64();
                InvocationScript = reader.ReadVarBytes(1024);
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.Write(OriginalViewNumber);
                writer.Write(Timestamp);
                writer.WriteVarBytes(InvocationScript);
            }
        }
    }
}
