using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.IO;

namespace Neo.UnitTests
{
    public class TestVerifiable : IVerifiable
    {
        private string testStr = "testStr";

        public Witness[] Witnesses { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Size => throw new NotImplementedException();

        public void Deserialize(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public byte[] GetMessage()
        {
            throw new NotImplementedException();
        }

        public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write((string)testStr);
        }
    }
}