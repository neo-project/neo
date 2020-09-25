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
            return new JObject
            {
                ["type"] = TransactionAttributeType.HighPriority
            };
        }

        protected override void DeserializeJson(TransactionAttributeType type, JObject json)
        {
            if (type != TransactionAttributeType.HighPriority)
                throw new FormatException();
        }
    }
}
