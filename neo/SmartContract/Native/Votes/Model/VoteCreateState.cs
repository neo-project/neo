using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using Neo.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class VoteCreateState : ISerializable
    {
        private UInt256 TransactionHash;
        private UInt160 CallingScriptHash;
        public readonly UInt160 Originator;
        public readonly string Title;
        public readonly string Description;
        public readonly int CandidateNumber;
        public readonly bool IsSequence;

        public VoteCreateState() { }

        public VoteCreateState(UInt256 transactionHash, UInt160 callingScriptHash, UInt160 originator, string title, string description, int candidate, bool IsSeq)
        {
            TransactionHash = transactionHash;
            CallingScriptHash = callingScriptHash;
            Originator = originator;
            Title = title;
            Description = description;
            CandidateNumber = candidate;
            IsSequence = IsSeq;
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

        public static VoteCreateState FromByteArray(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(memoryStream) as VoteCreateState;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
        //public byte[] GetId() => new Crypto().Hash160(ConcatByte(TransactionHash.ToArray(), CallingScriptHash.ToArray()));
        public byte[] GetId() => UInt160.Zero.ToArray();
        public static byte[] ConcatByte(byte[] byteSource, byte[] newData)
        {
            List<byte> result = new List<byte>(byteSource);
            result.AddRange(newData);
            return result.ToArray();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
