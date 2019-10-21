using System.Collections.Generic;
using System.IO;
using Neo.IO;

namespace Neo.SmartContract.Native.Votes.Interface
{
    internal class MultiCandidate : ISerializable
        { 
        public MultiCandidate() => this.candidateList = new List<int>();
        public MultiCandidate(List<int> lists) => this.candidateList = lists;

        private List<int> candidateList = null;

        public int Size => candidateList.ToArray().Length;

        public void Serialize(BinaryWriter write)
        {
            foreach (var candidate in candidateList)
            {
                write.Write(candidate);
            }
        }
        public void Deserialize(BinaryReader reader)
        {
            candidateList = new List<int>();
            while (true)
            {
                try
                {
                    candidateList.Add(reader.ReadInt32());
                }
                catch
                {
                    break;
                }
            }
        }

        public List<int> GetCandidate()
        {
            return candidateList;
        }
    }
    internal class SingleCandidate : ISerializable
    {
        public SingleCandidate() { }
        public SingleCandidate(int candidate) => this.candidate = candidate;

        private int candidate = 0;

        public int Size => 4;

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
    }
}
