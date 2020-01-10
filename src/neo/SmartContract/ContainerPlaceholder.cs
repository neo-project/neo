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

        public override bool Equals(object obj) => throw new NotSupportedException();

        public override int GetHashCode() => throw new NotSupportedException();

        public override bool ToBoolean() => throw new NotSupportedException();
    }
}
