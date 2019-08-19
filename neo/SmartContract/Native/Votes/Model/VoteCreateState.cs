using System;
using System.Collections.Generic;
using System.IO;
using Neo.IO;

namespace Neo.SmartContract.Native.Votes.Model
{
    internal class VoteCreateState : ISerializable
    {
        private UInt256 TransactionHash;
        private UInt160 CallingScriptHash;
        public  UInt160 Originator;
        public  string Title;
        public  string Description;
        public  UInt32 CandidateNumber;
        public  bool IsSequence;

        public int Size => throw new NotImplementedException();

        public VoteCreateState() { }

        public VoteCreateState(UInt256 transactionHash, UInt160 callingScriptHash, UInt160 originator, string title, string description, UInt32 candidate, bool IsSeq)
        {
            TransactionHash = transactionHash;
            CallingScriptHash = callingScriptHash;
            Originator = originator;
            Title = title;
            Description = description;
            CandidateNumber = candidate;
            IsSequence = IsSeq;
        }
        //public byte[] GetId() => new Crypto().Hash160(ConcatByte(TransactionHash.ToArray(), CallingScriptHash.ToArray()));
        public byte[] GetId() => UInt160.Zero.ToArray();
        public static byte[] ConcatByte(byte[] byteSource, byte[] newData)
        {
            List<byte> result = new List<byte>(byteSource);
            result.AddRange(newData);
            return result.ToArray();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TransactionHash);
            writer.Write(CallingScriptHash);
            writer.Write(Originator);
            writer.Write(System.Text.Encoding.UTF8.GetBytes(Title));
            writer.Write(System.Text.Encoding.UTF8.GetBytes(Description));
            writer.Write(CandidateNumber);
            writer.Write(IsSequence);

        }

        public void Deserialize(BinaryReader reader)
        {
            TransactionHash = new UInt256(reader.ReadBytes(42));
            CallingScriptHash = reader.ReadBytes(20).ToScriptHash();
            Originator = reader.ReadBytes(20).ToScriptHash();
            Title = reader.ReadString();
            Description = reader.ReadString();
            CandidateNumber = reader.ReadUInt32();
            IsSequence = reader.ReadBoolean();
        }
    }
}
