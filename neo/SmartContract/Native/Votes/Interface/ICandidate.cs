using System.Collections.Generic;
using System.IO;

namespace Neo.SmartContract.Native.Votes.Interface
{
    interface ICandidate
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }

    internal class MultiCandidate : ICandidate
    {
        public MultiCandidate() => this.candidateList = new List<int>();
        public MultiCandidate(List<int> lists) => this.candidateList = lists;

        private List<int> candidateList = null;

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
    internal class SingleCandidate : ICandidate
    {
        public SingleCandidate() { }
        public SingleCandidate(int candidate) => this.candidate = candidate;

        private int candidate = 0;

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
