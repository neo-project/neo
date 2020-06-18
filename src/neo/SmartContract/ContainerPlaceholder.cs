using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    internal class ContainerPlaceholder : StackItem
    {
        public override StackItemType Type { get; }
        public int ElementCount { get; }

        public ContainerPlaceholder(StackItemType type, int count)
        {
            Type = type;
            ElementCount = count;
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is ContainerPlaceholder b)
            {
                return Type == b.Type && ElementCount == b.ElementCount;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, ElementCount);
        }

        public override bool ToBoolean() => throw new NotSupportedException();
    }
}
