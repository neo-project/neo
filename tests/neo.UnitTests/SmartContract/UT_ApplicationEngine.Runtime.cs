using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System;
using System.Numerics;
namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestGetRandomSameBlock()
        {

            var tx = TestUtils.GetTransaction(UInt160.Zero);
            // Even if persisting the same block, in different ApplicationEngine instance, the random number should be different
            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, tx, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, tx, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine_1.LoadScript(new byte[] { 0x01 });
            engine_2.LoadScript(new byte[] { 0x01 });

            var rand_1 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_2 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_3 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_4 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_5 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();

            var rand_6 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_7 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_8 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_9 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_10 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();

            rand_1.Should().Be("23c919ec06fdc03d408a214ddf5ed4ef00");
            rand_2.Should().Be("4b8c639785bb8aa6b99f1a74aa757dd400");
            rand_3.Should().Be("37114c35d0c0637db9fdf67b6f932b66");
            rand_4.Should().Be("ad039fc7a63b68ba8f7ab7f197b653eb00");
            rand_5.Should().Be("7721046c41c4f15d31dd495f4ac9d48d00");

            rand_1.Should().Be(rand_6);
            rand_2.Should().Be(rand_7);
            rand_3.Should().Be(rand_8);
            rand_4.Should().Be(rand_9);
            rand_5.Should().Be(rand_10);
        }

        [TestMethod]
        public void TestGetRandomDifferentBlock()
        {

            // Block block_1 = TestBlockchain.TheNeoSystem.GenesisBlock;
            var tx_1 = TestUtils.GetTransaction(UInt160.Zero);

            var tx_2 = new Transaction
            {
                Version = 0,
                Nonce = 2083236893,
                ValidUntilBlock = 0,
                Signers = Array.Empty<Signer>(),
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = Array.Empty<byte>(),
                SystemFee = 0,
                NetworkFee = 0,
                Witnesses = Array.Empty<Witness>()
            };

            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, tx_1, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            // The next_nonce shuld be reinitialized when a new block is persisting
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, tx_2, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            // engine_1.LoadScript(new byte[] { 0x01 });
            // engine_2.LoadScript(new byte[] { 0x02 });

            var rand_1 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_2 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_3 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_4 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_5 = engine_1.GetRandom().ToByteArray().ToHexString().ToString();

            var rand_6 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_7 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_8 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_9 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();
            var rand_10 = engine_2.GetRandom().ToByteArray().ToHexString().ToString();

            rand_1.Should().Be("23c919ec06fdc03d408a214ddf5ed4ef00");
            rand_2.Should().Be("4b8c639785bb8aa6b99f1a74aa757dd400");
            rand_3.Should().Be("37114c35d0c0637db9fdf67b6f932b66");
            rand_4.Should().Be("ad039fc7a63b68ba8f7ab7f197b653eb00");
            rand_5.Should().Be("7721046c41c4f15d31dd495f4ac9d48d00");

            rand_1.Should().NotBe(rand_6);
            rand_2.Should().NotBe(rand_7);
            rand_3.Should().NotBe(rand_8);
            rand_4.Should().NotBe(rand_9);
            rand_5.Should().NotBe(rand_10);
        }
    }
}
