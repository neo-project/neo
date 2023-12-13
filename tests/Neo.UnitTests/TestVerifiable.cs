using System;
using System.IO;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;

namespace Neo.UnitTests
{
    public class TestVerifiable : IVerifiable
    {
        private readonly string testStr = "testStr";

        public Witness[] Witnesses
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public int Size => throw new NotImplementedException();

        public void Deserialize(ref MemoryReader reader)
        {
            throw new NotImplementedException();
        }

        public void DeserializeUnsigned(ref MemoryReader reader)
        {
            throw new NotImplementedException();
        }

        public UInt160[] GetScriptHashesForVerifying(DataCache snapshot)
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
