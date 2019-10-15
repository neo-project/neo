using Neo.IO;
using System.Collections.Generic;
using System.IO;

namespace Neo.SmartContract.Native.Votes.Interface
{
    internal class MultiCandidate : ICandidate
    {
        private List<int> _candidateList = null;

        public int Count => _candidateList.Count;
        public int Size => _candidateList.Count.GetVarSize() + sizeof(int);

        public MultiCandidate() => _candidateList = new List<int>();
        public MultiCandidate(List<int> lists) => _candidateList = lists;

        public void Serialize(BinaryWriter write)
        {
            write.WriteVarInt(_candidateList.Count);
            foreach (var candidate in _candidateList)
            {
                write.Write(candidate);
            }
        }
        public void Deserialize(BinaryReader reader)
        {
            var count = (int)reader.ReadVarInt(ushort.MaxValue);
            _candidateList = new List<int>(count);
            while (count > 0)
            {
                _candidateList.Add(reader.ReadInt32());
                count--;
            }
        }

        public List<int> GetCandidate()
        {
            return _candidateList;
        }
    }
}
