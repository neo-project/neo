using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Witness
    {
        Witness uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Witness();
        }

        [TestMethod]
        public void InvocationScript_Get()
        {
            uut.InvocationScript.IsEmpty.Should().BeTrue();
        }

        private static Witness PrepareDummyWitness(int pubKeys, int m)
        {
            var address = new WalletAccount[pubKeys];
            var wallets = new NEP6Wallet[pubKeys];
            var snapshot = TestBlockchain.GetTestSnapshot();

            for (int x = 0; x < pubKeys; x++)
            {
                wallets[x] = TestUtils.GenerateTestWallet("123");
                address[x] = wallets[x].CreateAccount();
            }

            // Generate multisignature

            var multiSignContract = Contract.CreateMultiSigContract(m, address.Select(a => a.GetKey().PublicKey).ToArray());

            for (int x = 0; x < pubKeys; x++)
            {
                wallets[x].CreateAccount(multiSignContract, address[x].GetKey());
            }

            // Sign

            var data = new ContractParametersContext(snapshot, new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = new[] {new Signer()
                {
                    Account = multiSignContract.ScriptHash,
                    Scopes = WitnessScope.CalledByEntry
                }},
                NetworkFee = 0,
                Nonce = 0,
                Script = Array.Empty<byte>(),
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
                Witnesses = Array.Empty<Witness>()
            }, TestProtocolSettings.Default.Network);

            for (int x = 0; x < m; x++)
            {
                Assert.IsTrue(wallets[x].Sign(data));
            }

            Assert.IsTrue(data.Completed);
            return data.GetWitnesses()[0];
        }

        [TestMethod]
        public void MaxSize_OK()
        {
            var witness = PrepareDummyWitness(10, 10);

            // Check max size

            witness.Size.Should().Be(1023);
            witness.InvocationScript.GetVarSize().Should().Be(663);
            witness.VerificationScript.GetVarSize().Should().Be(360);

            var copy = witness.ToArray().AsSerializable<Witness>();

            Assert.IsTrue(witness.InvocationScript.Span.SequenceEqual(copy.InvocationScript.Span));
            Assert.IsTrue(witness.VerificationScript.Span.SequenceEqual(copy.VerificationScript.Span));
        }

        [TestMethod]
        public void MaxSize_Error()
        {
            var witness = new Witness
            {
                InvocationScript = new byte[1025],
                VerificationScript = new byte[10]
            };

            // Check max size

            Assert.ThrowsException<FormatException>(() => witness.ToArray().AsSerializable<Witness>());

            // Check max size

            witness.InvocationScript = new byte[10];
            witness.VerificationScript = new byte[1025];
            Assert.ThrowsException<FormatException>(() => witness.ToArray().AsSerializable<Witness>());
        }

        [TestMethod]
        public void InvocationScript_Set()
        {
            byte[] dataArray = new byte[] { 0, 32, 32, 20, 32, 32 };
            uut.InvocationScript = dataArray;
            uut.InvocationScript.Length.Should().Be(6);
            Assert.AreEqual(uut.InvocationScript.Span.ToHexString(), "002020142020");
        }

        private static void SetupWitnessWithValues(Witness uut, int lenghtInvocation, int lengthVerification, out byte[] invocationScript, out byte[] verificationScript)
        {
            invocationScript = TestUtils.GetByteArray(lenghtInvocation, 0x20);
            verificationScript = TestUtils.GetByteArray(lengthVerification, 0x20);
            uut.InvocationScript = invocationScript;
            uut.VerificationScript = verificationScript;
        }

        [TestMethod]
        public void SizeWitness_Small_Arrary()
        {
            SetupWitnessWithValues(uut, 252, 253, out _, out _);

            uut.Size.Should().Be(509); // (1 + 252*1) + (1 + 2 + 253*1)
        }

        [TestMethod]
        public void SizeWitness_Large_Arrary()
        {
            SetupWitnessWithValues(uut, 65535, 65536, out _, out _);

            uut.Size.Should().Be(131079); // (1 + 2 + 65535*1) + (1 + 4 + 65536*1)
        }

        [TestMethod]
        public void ToJson()
        {
            SetupWitnessWithValues(uut, 2, 3, out _, out _);

            JObject json = uut.ToJson();
            Assert.IsTrue(json.ContainsProperty("invocation"));
            Assert.IsTrue(json.ContainsProperty("verification"));
            Assert.AreEqual(json["invocation"].AsString(), "ICA=");
            Assert.AreEqual(json["verification"].AsString(), "ICAg");
        }
    }
}
