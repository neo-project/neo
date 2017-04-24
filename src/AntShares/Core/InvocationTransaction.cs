using AntShares.IO;
using AntShares.IO.Json;
using System.IO;

namespace AntShares.Core
{
    public class InvocationTransaction : Transaction
    {
        public byte[] Script;

        public override int Size => base.Size + Script.GetVarSize();

        public InvocationTransaction()
            : base(TransactionType.InvocationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Script = reader.ReadVarBytes(65536);
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["script"] = Script.ToHexString();
            return json;
        }
    }
}
