using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Neo.IO;
using Neo.SmartContract.Native.Votes.Interface;
using System.Runtime.Serialization.Formatters.Binary;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class VoteState
    {
        private UInt160 Voter;
        private ICandidate Records;

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
            if (reader.BaseStream.Length == 4)
            {
                SingleCandidate candidate = new SingleCandidate();
                candidate.Deserialize(reader);
                Records = candidate;
            }
            else
            {
                MultiCandidate candidate = new MultiCandidate();
                candidate.Deserialize(reader);
                Records = candidate;
            }
        }
        
        public byte[] ToByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        public static VoteState FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as VoteState;
                }
                catch (Exception e)
                {
                    throw e;
                }

            }
        }
    }
}
