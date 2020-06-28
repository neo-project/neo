using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Text;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_StateRoot
    {
        StateRoot state_root;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            state_root = new StateRoot()
            {
                Version = 0,
                Index = 1234,
                RootHash = UInt256.Parse("5f4f1af77b127c9037c0204f682420a5ce621f3d8f4c8bdd9fd37422e0c58e9b"),
                Witness = new Witness()
                {
                    InvocationScript = new byte[] { 0x01 },
                    VerificationScript = new byte[] { 0x02 }
                }
            };
        }

        [TestMethod]
        public void TestSerializeUnsigned()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            state_root.SerializeUnsigned(writer);

            Assert.AreEqual("00d20400009b8ec5e02274d39fdd8b4c8f3d1f62cea52024684f20c037907c127bf71a4f5f", ms.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestDesializeUnsigned()
        {
            var data = "00d20400009b8ec5e02274d39fdd8b4c8f3d1f62cea52024684f20c037907c127bf71a4f5f".HexToBytes();
            using MemoryStream ms = new MemoryStream(data, false);
            using BinaryReader reader = new BinaryReader(ms, Encoding.UTF8);

            var state_root_d = new StateRoot();
            state_root_d.DeserializeUnsigned(reader);

            Assert.AreEqual(state_root.Version, state_root_d.Version);
            Assert.AreEqual(state_root.Index, state_root_d.Index);
            Assert.AreEqual(state_root.RootHash, state_root_d.RootHash);
        }

        [TestMethod]
        public void TestGetScriptHashesForVerifying()
        {
            state_root.Index = 0;
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var scriptHashes = state_root.GetScriptHashesForVerifying(snapshot);
            Assert.AreEqual(Blockchain.GenesisBlock.NextConsensus, scriptHashes[0]);
        }

        [TestMethod]
        public void TestToJson()
        {
            var json = state_root.ToJson();
            Assert.AreEqual(0, json["version"].AsNumber());
            Assert.AreEqual(1234, json["index"].AsNumber());
            Assert.AreEqual(state_root.RootHash.ToString(), json["stateroot"].AsString());
        }
    }
}
