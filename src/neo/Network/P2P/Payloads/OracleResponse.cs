using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResponse : TransactionAttribute
    {
        private const int MaxResultSize = 1024;

        public ulong Id;
        public bool Success;
        public byte[] Result;

        public override TransactionAttributeType Type => TransactionAttributeType.OracleResponse;
        public override bool AllowMultiple => false;

        public override int Size => base.Size +
            sizeof(ulong) +         //Id
            sizeof(bool) +          //Success
            Result.GetVarSize();    //Result

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Success = reader.ReadBoolean();
            Result = reader.ReadVarBytes(MaxResultSize);
            if (!Success && Result.Length > 0) throw new FormatException();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Success);
            writer.WriteVarBytes(Result);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["id"] = Id;
            json["success"] = Success;
            json["result"] = Convert.ToBase64String(Result);
            return json;
        }
    }
}
