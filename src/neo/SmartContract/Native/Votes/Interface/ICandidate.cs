using System.Collections.Generic;
using System.IO;
using Neo.IO;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Votes.Interface
{
    internal class MultiCandidate : ISerializable, IInteroperable
    {
        private List<int> candidateList;
        public int Size => candidateList.ToArray().Length;

        public MultiCandidate() => this.candidateList = new List<int>();
        public MultiCandidate(List<int> lists) => this.candidateList = lists;        

        public List<int> GetCandidate()
        {
            return candidateList;
        }

        public void Serialize(BinaryWriter write)
        {
            write.Write(Size);
            foreach (var candidate in candidateList)
            {
                write.Write(candidate);
            }
        }
        public void Deserialize(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            candidateList = new List<int>();
            for (int i = 0; i < size; i++)
            {
                candidateList.Add(reader.ReadInt32());
            }
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            List<StackItem> stackLists = new List<StackItem>();
            foreach (int i in candidateList)
            {
                stackLists.Add(new Integer(i));
            }
            stackLists.Add(new Integer(Size));
            return new Array(referenceCounter ,stackLists.ToArray());
        }
    }
    internal class SingleCandidate : ISerializable , IInteroperable
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
