using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.IO;

namespace Neo.UnitTests
{
    public class TestVerifiable : IWitnessed
    {
        private readonly string testStr = "testStr";

        public Witness[] Witnesses
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public int Size => throw new NotImplementedException();

        public void Deserialize(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
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
