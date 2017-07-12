using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.VM;
using Neo.Wallets;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Block
    {
        Block uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Block();
        }

        [TestMethod]
        public void Transactions_Get()
        {
            uut.Transactions.Should().BeNull();
        }

        [TestMethod]
        public void Transactions_Set()
        {
            Transaction[] val = new Transaction[10];
            uut.Transactions = val;
            uut.Transactions.Length.Should().Be(10);
        }

        private void setupBlockWithValues(out UInt256 val256, out uint timestampVal, out uint indexVal, out ulong consensusDataVal, out Witness scriptVal)
        {
            val256 = UInt256.Zero;
            uut.PrevHash = val256;
            uut.MerkleRoot = val256;
            timestampVal = DateTime.Now.ToTimestamp();
            uut.Timestamp = timestampVal;
            indexVal = 0;
            uut.Index = indexVal;
            consensusDataVal = 30;
            uut.ConsensusData = consensusDataVal;
            UInt160 val160 = UInt160.Zero;
            uut.NextConsensus = val160;
            uut.Transactions = new Transaction[0];
            scriptVal = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            };
            uut.Script = scriptVal;
        }

        [TestMethod]
        public void Header_Get()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Header.Should().NotBeNull();
            uut.Header.PrevHash.Should().Be(val256);
            uut.Header.MerkleRoot.Should().Be(val256);
            uut.Header.Timestamp.Should().Be(timestampVal);
            uut.Header.Index.Should().Be(indexVal);
            uut.Header.ConsensusData.Should().Be(consensusDataVal);
            uut.Header.Script.Should().Be(scriptVal);
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // block 1
            uut.Size.Should().Be(109);
        }

        private MinerTransaction getMinerTransaction()
        {
            return new MinerTransaction
            {
                Nonce = 2083236893,
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                Scripts = new Witness[0]
            };
        }

        private ClaimTransaction getClaimTransaction()
        {
            return new ClaimTransaction
            {
                Claims = new CoinReference[0]
            };
        }

        private readonly ECPoint[] StandbyValidators = new ECPoint[] { ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1) };
        private IssueTransaction getIssueTransaction(bool inputVal, decimal outputVal, UInt256 assetId)
        {
            setupTestBlockchain(assetId);

            CoinReference[] inputsVal;
            if (inputVal)
            {
                inputsVal = new[]
                {
                    new CoinReference
                    {
                        PrevHash = UInt256.Zero,
                        PrevIndex = 0
                    }
                };
            }
            else
            {
                inputsVal = new CoinReference[0];
            }
            

            return new IssueTransaction
            {
                Attributes = new TransactionAttribute[0],
                Inputs = inputsVal,
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = assetId,
                        Value = Fixed8.FromDecimal(outputVal),
                        ScriptHash = Contract.CreateMultiSigRedeemScript(1, new ECPoint[] { ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1)  }).ToScriptHash()
                    }
                },
                Scripts = new[]
                {
                    new Witness
                    {
                        InvocationScript = new byte[0],
                        VerificationScript = new[] { (byte)OpCode.PUSHT }
                    }
                }
            };
        }

        private void setupTestBlockchain(UInt256 assetId)
        {
            Blockchain testBlockchain = new TestBlockchain(assetId);
            Blockchain.RegisterBlockchain(testBlockchain);
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getMinerTransaction()
            };

            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // block 11
            uut.Size.Should().Be(119);
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[3] {
                getMinerTransaction(),
                getMinerTransaction(),
                getMinerTransaction()
            };

            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // block 31
            uut.Size.Should().Be(139);
        }

        [TestMethod]
        public void CalculateNetFee_EmptyTransactions()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void CalculateNetFee_Ignores_MinerTransactions()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getMinerTransaction()
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void CalculateNetFee_Ignores_ClaimTransactions()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getClaimTransaction()
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.Zero);
        }


        [TestMethod]
        public void CalculateNetFee_Out()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getIssueTransaction(false, 100, Blockchain.SystemCoin.Hash)
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(-100));
        }

        [TestMethod]
        public void CalculateNetFee_In()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getIssueTransaction(true, 0, Blockchain.SystemCoin.Hash)
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(50));
        }

        [TestMethod]
        public void CalculateNetFee_In_And_Out()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getIssueTransaction(true, 100, Blockchain.SystemCoin.Hash)
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(-50));
        }

        [TestMethod]
        public void CalculateNetFee_SystemFee()
        {
            UInt256 val256;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            setupBlockWithValues(out val256, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Transactions = new Transaction[1] {
                getIssueTransaction(true, 0, new UInt256(TestUtils.GetByteArray(32, 0x42)))
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(-500));
        }
    }
}
