using System;
using System.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class HighPriorityAttribute : TransactionAttribute
    {
        public override bool AllowMultiple => false;

        public override int Size => sizeof(byte);

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)TransactionAttributeType.HighPriority);
        }

        protected override void Deserialize(TransactionAttributeType type, BinaryReader reader)
        {
            if (type != TransactionAttributeType.HighPriority)
                throw new FormatException();
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["type"] = TransactionAttributeType.HighPriority;
            return json;
        }

        protected override void FromJson(TransactionAttributeType type, JObject json)
        {
            if (type != TransactionAttributeType.HighPriority)
                throw new FormatException();
        }
    }
}
