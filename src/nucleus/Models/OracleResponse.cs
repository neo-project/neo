using System;
using System.IO;
using Neo.IO;

namespace Neo.Models
{
    public class OracleResponse : TransactionAttribute
    {
        private const int MaxResultSize = 1024;

        public ulong Id;
        public OracleResponseCode Code;
        public byte[] Result;

        public override bool AllowMultiple => false;
        
        public override int Size => 
            sizeof(byte) +                  // TransactionAttributeType
            sizeof(ulong) +                 // Id
            sizeof(OracleResponseCode) +    // ResponseCode
            Result.GetVarSize();            // Result

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)TransactionAttributeType.OracleResponse);
            writer.Write(Id);
            writer.Write((byte)Code);
            writer.WriteVarBytes(Result);
        }

        protected override void Deserialize(TransactionAttributeType type, BinaryReader reader)
        {
            if (type != TransactionAttributeType.HighPriority)
                throw new FormatException();

            Id = reader.ReadUInt64();
            Code = (OracleResponseCode)reader.ReadByte();
            if (!Enum.IsDefined(typeof(OracleResponseCode), Code))
                throw new FormatException();
            Result = reader.ReadVarBytes(MaxResultSize);
            if (Code != OracleResponseCode.Success && Result.Length > 0)
                throw new FormatException();
        }
    }
}
