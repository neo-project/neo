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
        public byte[] Result;

        public override TransactionAttributeType Type => TransactionAttributeType.OracleResponse;
        public override bool AllowMultiple => false;

        public override int Size => base.Size + sizeof(ulong) + Result.GetVarSize();

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Result = reader.ReadVarBytes(MaxResultSize);
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.WriteVarBytes(Result);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["id"] = Id;
            json["result"] = Convert.ToBase64String(Result);
            return json;
        }
    }
}
