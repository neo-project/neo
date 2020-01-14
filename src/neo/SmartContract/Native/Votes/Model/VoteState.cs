using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Neo.IO;
using Neo.SmartContract.Native.Votes.Interface;
using System.Runtime.Serialization.Formatters.Binary;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class VoteState : ISerializable
    {
        private UInt160 voter;
        private ISerializable records;
        public int Size => records.Size + UInt160.Length;

        public UInt160 GetVoter() => this.voter;

        public VoteState() { }

        public VoteState(UInt160 voter, ISerializable candidate)
        {
            this.voter = voter;
            records = candidate;
        }
        
        public ISerializable GetCandidate() => this.records;

        public void Serialize(BinaryWriter write)
        {
            write.Write(voter);
            records.Serialize(write);
        }

        public void Deserialize(BinaryReader reader)
        {
            voter = new UInt160(reader.ReadBytes(20));
            if (reader.BaseStream.Length - UInt160.Length <= 4)
            {
                SingleCandidate candidate = new SingleCandidate();
                candidate.Deserialize(reader);
                records = candidate;
            }
            else
            {
                MultiCandidate candidate = new MultiCandidate();
                candidate.Deserialize(reader);
                records = candidate;
            }
        }
    }
}
