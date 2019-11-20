using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    internal class ContainerPlaceholder : StackItem
    {
        public StackItemType Type;
        public int ElementCount;

        public override bool Equals(StackItem other) => throw new NotSupportedException();

        public override int GetHashCode() => throw new NotSupportedException();

        public override bool ToBoolean() => throw new NotSupportedException();
    }
}
