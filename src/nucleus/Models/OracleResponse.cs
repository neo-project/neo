using System;
using System.Globalization;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

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
            if (type != TransactionAttributeType.OracleResponse)
                throw new FormatException();

            Id = reader.ReadUInt64();
            Code = (OracleResponseCode)reader.ReadByte();
            Result = reader.ReadVarBytes(MaxResultSize);

            ValidateDeserialized();
        }

        private void ValidateDeserialized()
        {
            if (!Enum.IsDefined(typeof(OracleResponseCode), Code))
                throw new FormatException();
            if (Code != OracleResponseCode.Success && Result.Length > 0)
                throw new FormatException();
        }

        public override JObject ToJson()
        {
            return new JObject
            {
                ["type"] = TransactionAttributeType.OracleResponse,
                ["id"] = Id.ToString("x16"),
                ["code"] = Code,
                ["result"] = Convert.ToBase64String(Result),
            };
        }

        protected override void DeserializeJson(TransactionAttributeType type, JObject json)
        {
            if (type != TransactionAttributeType.OracleResponse)
                throw new FormatException();

            Id = ulong.Parse(json["id"].AsString(), NumberStyles.HexNumber);
            Code = (OracleResponseCode)Enum.Parse(typeof(OracleResponseCode), json["code"].AsString(), false);
            Result = Convert.FromBase64String(json["result"].AsString());

            ValidateDeserialized();
        }
    }
}
