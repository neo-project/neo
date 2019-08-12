using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.IO;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Witness
    {
        class dummyVerificable : IVerifiable
        {
            private UInt160 _hash;

            public Witness[] Witnesses { get; set; }

            public int Size => 1;

            public dummyVerificable(UInt160 hash)
            {
                _hash = hash;
            }

            public void Deserialize(BinaryReader reader)
            {
                DeserializeUnsigned(reader);
                Witnesses = reader.ReadSerializableArray<Witness>(16);
            }

            public void DeserializeUnsigned(BinaryReader reader)
            {
                reader.ReadByte();
            }

            public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
            {
                return new UInt160[] { _hash };
            }

            public void Serialize(BinaryWriter writer)
            {
                SerializeUnsigned(writer);
                writer.Write(Witnesses);
            }

            public void SerializeUnsigned(BinaryWriter writer)
            {
                writer.Write((byte)1);
            }
        }

        Witness uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Witness();
        }

        [TestMethod]
        public void InvocationScript_Get()
        {
            uut.InvocationScript.Should().BeNull();
        }

        [TestMethod]
        public void MaxSize()
        {
            var store = TestBlockchain.GetStore();
            var wallet = UT_Transaction.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // Prepare

            var maxAccounts = 14;
            var address = new WalletAccount[maxAccounts];
            var wallets = new NEP6Wallet[maxAccounts];
            var walletsUnlocks = new IDisposable[maxAccounts];

            for (int x = 0; x < maxAccounts; x++)
            {
                wallets[x] = UT_Transaction.GenerateTestWallet();
                walletsUnlocks[x] = wallets[x].Unlock("123");
                address[x] = wallets[x].CreateAccount();
            }

            // Generate multisignature

            var multiSignContract = Contract.CreateMultiSigContract(maxAccounts, address.Select(a => a.GetKey().PublicKey).ToArray());

            for (int x = 0; x < maxAccounts; x++)
            {
                wallets[x].CreateAccount(multiSignContract, address[x].GetKey());
            }

            // Sign

            var data = new ContractParametersContext(new dummyVerificable(multiSignContract.ScriptHash));

            for (int x = 0; x < maxAccounts; x++)
            {
                Assert.IsTrue(wallets[x].Sign(data));
            }

            Assert.IsTrue(data.Completed);
            var witnesses = data.GetWitnesses()[0];

            // Check max size

            Assert.IsTrue(witnesses.Size < 1500);

            var copy = witnesses.ToArray().AsSerializable<Witness>();

            CollectionAssert.AreEqual(witnesses.InvocationScript, copy.InvocationScript);
            CollectionAssert.AreEqual(witnesses.VerificationScript, copy.VerificationScript);
        }

        [TestMethod]
        public void InvocationScript_Set()
        {
            byte[] dataArray = new byte[] { 0, 32, 32, 20, 32, 32 };
            uut.InvocationScript = dataArray;
            uut.InvocationScript.Length.Should().Be(6);
            Assert.AreEqual(uut.InvocationScript.ToHexString(), "002020142020");
        }

        private void setupWitnessWithValues(Witness uut, int lenghtInvocation, int lengthVerification, out byte[] invocationScript, out byte[] verificationScript)
        {
            invocationScript = TestUtils.GetByteArray(lenghtInvocation, 0x20);
            verificationScript = TestUtils.GetByteArray(lengthVerification, 0x20);
            uut.InvocationScript = invocationScript;
            uut.VerificationScript = verificationScript;
        }

        [TestMethod]
        public void SizeWitness_Small_Arrary()
        {
            byte[] invocationScript;
            byte[] verificationScript;
            setupWitnessWithValues(uut, 252, 253, out invocationScript, out verificationScript);

            uut.Size.Should().Be(509); // (1 + 252*1) + (1 + 2 + 253*1)
        }

        [TestMethod]
        public void SizeWitness_Large_Arrary()
        {
            byte[] invocationScript;
            byte[] verificationScript;
            setupWitnessWithValues(uut, 65535, 65536, out invocationScript, out verificationScript);

            uut.Size.Should().Be(131079); // (1 + 2 + 65535*1) + (1 + 4 + 65536*1)
        }

        [TestMethod]
        public void ToJson()
        {
            byte[] invocationScript;
            byte[] verificationScript;
            setupWitnessWithValues(uut, 2, 3, out invocationScript, out verificationScript);

            JObject json = uut.ToJson();
            Assert.IsTrue(json.ContainsProperty("invocation"));
            Assert.IsTrue(json.ContainsProperty("verification"));
            Assert.AreEqual(json["invocation"].AsString(), "2020");
            Assert.AreEqual(json["verification"].AsString(), "202020");
        }
    }
}