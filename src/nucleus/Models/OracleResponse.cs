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

        public static readonly byte[] FixedScript;

        public ulong Id;
        public OracleResponseCode Code;
        public byte[] Result;

        public override TransactionAttributeType Type => TransactionAttributeType.OracleResponse;
        public override bool AllowMultiple => false;

        public override int Size => base.Size +
            sizeof(ulong) +                 //Id
            sizeof(OracleResponseCode) +    //ResponseCode
            Result.GetVarSize();            //Result

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
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

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write((byte)Code);
            writer.WriteVarBytes(Result);
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

        protected override void DeserializeJson(JObject json)
        {
            Id = ulong.Parse(json["id"].AsString(), NumberStyles.HexNumber);
            Code = (OracleResponseCode)Enum.Parse(typeof(OracleResponseCode), json["code"].AsString(), false);
            Result = Convert.FromBase64String(json["result"].AsString());
            ValidateDeserialized();
        }
    }
}
