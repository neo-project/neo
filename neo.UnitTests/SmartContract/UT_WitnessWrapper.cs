using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.UnitTests.Ledger;
using System.IO;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_WitnessWrapper
    {
        [TestMethod]
        public void TestCreate()
        {
            var mockSnapshot = new Mock<Snapshot>();
            var myDataCache = new MyDataCache<UInt160, ContractState>();
            ContractState item1 = new ContractState
            {
                Script = new byte[] { 0x00 }
            };
            ContractState item2 = new ContractState
            {
                Script = new byte[] { 0x01 }
            };
            var hash1 = item1.Script.ToScriptHash();
            var hash2 = item2.Script.ToScriptHash();
            myDataCache.Add(hash1, item1);
            myDataCache.Add(hash2, item2);

            TestVerifiable verifiable = new TestVerifiable();
            Witness witness1 = new Witness()
            {
                VerificationScript = new byte[0]
            };
            Witness witness2 = new Witness()
            {
                VerificationScript = new byte[0]
            };
            verifiable.Witnesses = new Witness[] { witness1, witness2 };
            UInt160[] Hashes = new UInt160[2] { hash1, hash2 };
            verifiable.Hashes = Hashes;
            mockSnapshot.SetupGet(p => p.Contracts).Returns(myDataCache);
            WitnessWrapper[] wrappers = WitnessWrapper.Create(verifiable, mockSnapshot.Object);
            wrappers.Length.Should().Be(2);
            wrappers[0].VerificationScript.Should().BeEquivalentTo(item1.Script);
            wrappers[1].VerificationScript.Should().BeEquivalentTo(item2.Script);
        }
    }

    public class TestVerifiable : IVerifiable
    {
        public Witness[] Witnesses { get; set; }

        public UInt160[] Hashes { get; set; }

        public int Size => throw new System.NotImplementedException();

        public void Deserialize(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }

        public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            return Hashes;
        }

        public void Serialize(BinaryWriter writer)
        {
            throw new System.NotImplementedException();
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            throw new System.NotImplementedException();
        }
    }
}
