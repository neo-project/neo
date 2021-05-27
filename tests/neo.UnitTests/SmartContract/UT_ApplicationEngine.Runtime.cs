using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {


        [TestMethod]
        public void TestGetRandomSameBlock()
        {

            Block block_1 = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = TestBlockchain.TheNeoSystem.GenesisBlock.Timestamp,
                    Index = 0,
                    PrimaryIndex = 0,
                    NextConsensus = TestBlockchain.TheNeoSystem.GenesisBlock.NextConsensus,
                    Witness = TestBlockchain.TheNeoSystem.GenesisBlock.Witness,
                },
                Transactions = new[]
                {
                    new Transaction
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
                    },
                    new Transaction
                    {
                        Version = 0,
                        Nonce = 0,
                        ValidUntilBlock = 0,
                        Signers = Array.Empty<Signer>(),
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Script = Array.Empty<byte>(),
                        SystemFee = 0,
                        NetworkFee = 0,
                        Witnesses = Array.Empty<Witness>()
                    },
                }
            };
            // Even if persisting the same block, in different ApplicationEngine instance, the random number should be different
            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, null, null, block_1, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, null, null, block_1, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            uint rand_1 = engine_1.GetRandom();
            uint rand_2 = engine_1.GetRandom();
            uint rand_3 = engine_1.GetRandom();
            uint rand_4 = engine_1.GetRandom();
            uint rand_5 = engine_1.GetRandom();

            uint rand_6 = engine_2.GetRandom();
            uint rand_7 = engine_2.GetRandom();
            uint rand_8 = engine_2.GetRandom();
            uint rand_9 = engine_2.GetRandom();
            uint rand_10 = engine_2.GetRandom();

            rand_1.Should().Be(2083236893u);
            rand_2.Should().Be(3262894070u);
            rand_3.Should().Be(1882351097u);
            rand_4.Should().Be(2376063185u);
            rand_5.Should().Be(2304732336u);

            rand_1.Should().NotBe(rand_6);
            rand_2.Should().NotBe(rand_7);
            rand_3.Should().NotBe(rand_8);
            rand_4.Should().NotBe(rand_9);
            rand_5.Should().NotBe(rand_10);
        }

        [TestMethod]
        public void TestGetRandomDifferentBlock()
        {

            Block block_1 = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = 0,
                    Index = 0,
                    PrimaryIndex = 0,
                    NextConsensus = TestBlockchain.TheNeoSystem.GenesisBlock.NextConsensus,
                    Witness = TestBlockchain.TheNeoSystem.GenesisBlock.Witness,
                },
                Transactions = new[]
                {
                    new Transaction
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
                    },
                    new Transaction
                    {
                        Version = 0,
                        Nonce = 0,
                        ValidUntilBlock = 0,
                        Signers = Array.Empty<Signer>(),
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Script = Array.Empty<byte>(),
                        SystemFee = 0,
                        NetworkFee = 0,
                        Witnesses = Array.Empty<Witness>()
                    }
                }
            };

            Block block_2 = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = block_1.Timestamp,
                    Index = 0,
                    PrimaryIndex = 0,
                    NextConsensus = block_1.NextConsensus,
                    Witness = block_1.Witness,
                },
                Transactions = new[]
                {
                    new Transaction
                    {
                        Version = 0,
                        Nonce = 0,
                        ValidUntilBlock = 0,
                        Signers = Array.Empty<Signer>(),
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Script = Array.Empty<byte>(),
                        SystemFee = 0,
                        NetworkFee = 0,
                        Witnesses = Array.Empty<Witness>()
                    },
                    new Transaction
                    {
                        Version = 0,
                        Nonce = 2083236893+1,
                        ValidUntilBlock = 0,
                        Signers = Array.Empty<Signer>(),
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Script = Array.Empty<byte>(),
                        SystemFee = 0,
                        NetworkFee = 0,
                        Witnesses = Array.Empty<Witness>()
                    }
                }
            };

            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, null, null, block_1, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            // The next_nonce shuld be reinitialized when a new block is persisting
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, null, null, block_2, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            uint rand_1 = engine_1.GetRandom();
            uint rand_2 = engine_1.GetRandom();
            uint rand_3 = engine_1.GetRandom();
            uint rand_4 = engine_1.GetRandom();
            uint rand_5 = engine_1.GetRandom();

            uint rand_6 = engine_2.GetRandom();
            uint rand_7 = engine_2.GetRandom();
            uint rand_8 = engine_2.GetRandom();
            uint rand_9 = engine_2.GetRandom();
            uint rand_10 = engine_2.GetRandom();

            rand_1.Should().NotBe(2083236893u);
            rand_2.Should().NotBe(3262894070u);
            rand_3.Should().NotBe(1882351097u);
            rand_4.Should().NotBe(2376063185u);
            rand_5.Should().NotBe(2304732336u);

            rand_1.Should().NotBe(rand_6);
            rand_2.Should().NotBe(rand_7);
            rand_3.Should().NotBe(rand_8);
            rand_4.Should().NotBe(rand_9);
            rand_5.Should().NotBe(rand_10);
        }
    }
}
