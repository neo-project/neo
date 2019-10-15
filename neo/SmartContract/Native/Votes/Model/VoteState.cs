using Neo.IO;
using Neo.SmartContract.Native.Votes.Interface;
using System.IO;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class VoteState : ISerializable
    {
        private UInt160 Voter;
        private ICandidate Records;

        public int Size => UInt160.Length + Records.Size;

        public VoteState() { }
        public VoteState(UInt160 voter, ICandidate candidate)
        {
            Voter = voter;
            Records = candidate;
        }

        public UInt160 GetVoter() => this.Voter;
        public ICandidate GetCandidate() => this.Records;

        public void Serialize(BinaryWriter write)
        {
            write.Write(Voter);
            Records.Serialize(write);
        }

        public void Deserialize(BinaryReader reader)
        {
            Voter = new UInt160(reader.ReadBytes(20));

            var candidate = new MultiCandidate();
            candidate.Deserialize(reader);

            if (candidate.Count == 1)
            {
                Records = new SingleCandidate(candidate.GetCandidate()[0]);
            }
            else
            {
                Records = candidate;
            }
        }
    }
}
