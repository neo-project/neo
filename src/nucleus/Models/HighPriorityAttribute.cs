using System;
using System.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class HighPriorityAttribute : TransactionAttribute
    {
        public override bool AllowMultiple => false;
        public override TransactionAttributeType Type => TransactionAttributeType.HighPriority;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
        }

        protected override void DeserializeJson(JObject json)
        {
        }
    }
}
