using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep11TokenState : IInteroperable, ISerializable
    {
        public abstract int Size { get; }

        public abstract void Deserialize(BinaryReader reader);
        public abstract void FromStackItem(StackItem stackItem);
        public abstract void Serialize(BinaryWriter writer);
        public abstract StackItem ToStackItem(ReferenceCounter referenceCounter);
    }
}
