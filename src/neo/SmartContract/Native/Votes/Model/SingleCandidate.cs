using System.Collections.Generic;
using System.IO;
using Neo.IO;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class SingleCandidate : ISerializable, IInteroperable
    {
        private int candidate = 0;
        public int Size => 4;

        public SingleCandidate() { }

        public SingleCandidate(int candidate) => this.candidate = candidate;

        public void Serialize(BinaryWriter write)
        {
            write.Write(candidate);
        }

        public void Deserialize(BinaryReader reader)
        {
            candidate = reader.ReadInt32();
        }

        public int GetCandidate()
        {
            return candidate;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[]
            {
                candidate,
                Size
            });
        }
    }
}
