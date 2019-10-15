using Neo.IO;
using System;
using System.IO;

namespace Neo.SmartContract.Native.Votes.Interface
{
    internal class SingleCandidate : ICandidate
    {
        private int _candidate = 0;
        public int Size => sizeof(byte) + sizeof(int);

        public SingleCandidate() { }
        public SingleCandidate(int candidate) => _candidate = candidate;

        public void Serialize(BinaryWriter write)
        {
            write.WriteVarInt(1);
            write.Write(_candidate);
        }

        public void Deserialize(BinaryReader reader)
        {
            var count = reader.ReadVarInt();
            if (count < 1) throw new FormatException();
            _candidate = reader.ReadInt32();
        }

        public int GetCandidate()
        {
            return _candidate;
        }
    }
}
