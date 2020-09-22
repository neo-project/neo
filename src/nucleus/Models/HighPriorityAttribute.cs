using System;
using System.IO;

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
    }
}
