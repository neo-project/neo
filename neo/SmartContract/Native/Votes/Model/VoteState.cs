using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class VoteState
    {
        private readonly UInt160 Voter;
        private readonly ICandidate Records;

        public VoteState(UInt160 voter, ICandidate candidate)
        {
            Voter = voter;
            Records = candidate;
        }
        public UInt160 GetVoter() => this.Voter;
        public ICandidate GetCandidate() => this.Records;

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
