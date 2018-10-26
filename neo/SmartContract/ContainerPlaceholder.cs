using Neo.VM;
using System;

namespace Neo.SmartContract
{
    internal class ContainerPlaceholder : StackItem
    {
        public StackItemType Type;
        public int ElementCount;

        public override bool Equals(StackItem other)
        {
            throw new NotSupportedException();
        }

        public override byte[] GetByteArray()
        {
            throw new NotSupportedException();
        }
    }
}
