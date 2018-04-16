using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM;
using System.IO;
using System.Text;

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



        [TestMethod]
        public void Header_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Header.Should().NotBeNull();
            uut.Header.PrevHash.Should().Be(val256);
            uut.Header.MerkleRoot.Should().Be(merkRootVal);
            uut.Header.Timestamp.Should().Be(timestampVal);
            uut.Header.Index.Should().Be(indexVal);
            uut.Header.ConsensusData.Should().Be(consensusDataVal);
            uut.Header.Script.Should().Be(scriptVal);
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);
            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // block 1
            uut.Size.Should().Be(109);
        }

        private IssueTransaction getIssueTransaction(bool inputVal, decimal outputVal, UInt256 assetId)
        {
            TestUtils.SetupTestBlockchain(assetId);

            CoinReference[] inputsVal;
            if (inputVal)
            {
                inputsVal = new[]
                {
                    TestUtils.GetCoinReference(null)
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
                        ScriptHash = Contract.CreateMultiSigRedeemScript(1, TestUtils.StandbyValidators).ToScriptHash()
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

        private ContractTransaction getContractTransaction(bool inputVal, decimal outputVal, UInt256 assetId)
        {
            TestUtils.SetupTestBlockchain(assetId);

            CoinReference[] inputsVal;
            if (inputVal)
            {
                inputsVal = new[]
                {
                    TestUtils.GetCoinReference(null)
                };
            }
            else
            {
                inputsVal = new CoinReference[0];
            }

            return new ContractTransaction
            {
                Attributes = new TransactionAttribute[0],
                Inputs = inputsVal,
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = assetId,
                        Value = Fixed8.FromDecimal(outputVal),
                        ScriptHash = Contract.CreateMultiSigRedeemScript(1, TestUtils.StandbyValidators).ToScriptHash()
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



        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                TestUtils.GetMinerTransaction()
            };

            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // block 11
            uut.Size.Should().Be(119);
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[3] {
                TestUtils.GetMinerTransaction(),
                TestUtils.GetMinerTransaction(),
                TestUtils.GetMinerTransaction()
            };

            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // block 31
            uut.Size.Should().Be(139);
        }

        [TestMethod]
        public void CalculateNetFee_EmptyTransactions()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void CalculateNetFee_Ignores_MinerTransactions()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                TestUtils.GetMinerTransaction()
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void CalculateNetFee_Ignores_ClaimTransactions()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                TestUtils.GetClaimTransaction()
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.Zero);
        }


        [TestMethod]
        public void CalculateNetFee_Out()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                getContractTransaction(false, 100, Blockchain.UtilityToken.Hash)
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(-100));
        }

        [TestMethod]
        public void CalculateNetFee_In()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                getContractTransaction(true, 0, Blockchain.UtilityToken.Hash)
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(50));
        }

        [TestMethod]
        public void CalculateNetFee_In_And_Out()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                getContractTransaction(true, 100, Blockchain.UtilityToken.Hash)
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(-50));
        }

        [TestMethod]
        public void CalculateNetFee_SystemFee()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                TestUtils.GetIssueTransaction(true, 0, new UInt256(TestUtils.GetByteArray(32, 0x42)))
            };

            Block.CalculateNetFee(uut.Transactions).Should().Be(Fixed8.FromDecimal(-500));
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 1, 0, 0, 29, 172, 43, 124, 0, 0, 0, 0 };

            data.Length.Should().Be(119);
            for (int i = 0; i < 119; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(new Block(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 1, 0, 0, 29, 172, 43, 124, 0, 0, 0, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            assertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, consensusDataVal, scriptVal, transactionsVal);
        }

        private void assertStandardBlockTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, uint timestampVal, uint indexVal, ulong consensusDataVal, Witness scriptVal, Transaction[] transactionsVal, bool testTransactions = true)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.ConsensusData.Should().Be(consensusDataVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Script.InvocationScript.Length.Should().Be(0);
            uut.Script.Size.Should().Be(scriptVal.Size);
            uut.Script.VerificationScript[0].Should().Be(scriptVal.VerificationScript[0]);
            if (testTransactions)
            {
                uut.Transactions.Length.Should().Be(1);
                uut.Transactions[0].Should().Be(transactionsVal[0]);
            }
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            uut.Equals(uut).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DiffObj()
        {
            Block newBlock = new Block();
            UInt256 val256 = UInt256.Zero;
            UInt256 prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(newBlock, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 0);

            uut.Equals(newBlock).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_Null()
        {
            uut.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_SameHash()
        {

            Block newBlock = new Block();
            UInt256 prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(newBlock, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            uut.Equals(newBlock).Should().BeTrue();
        }

        [TestMethod]
        public void Trim()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            byte[] data = uut.Trim();
            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 1, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251 };

            data.Length.Should().Be(141);
            for (int i = 0; i < 141; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void FromTrimmedData()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(new Block(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 1, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251 };

            uut = Block.FromTrimmedData(data, 0, x => TestUtils.GetMinerTransaction());

            assertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, consensusDataVal, scriptVal, transactionsVal);
            uut.Transactions[0].Should().Be(TestUtils.GetMinerTransaction());
        }

        [TestMethod]
        public void FromTrimmedData_MultipleTx()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(new Block(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 3);

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 3, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251 };

            uut = Block.FromTrimmedData(data, 0, x => TestUtils.GetMinerTransaction());

            assertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, consensusDataVal, scriptVal, transactionsVal, testTransactions: false);
            uut.Transactions.Length.Should().Be(3);
            uut.Transactions[0].Should().Be(TestUtils.GetMinerTransaction());
            uut.Transactions[1].Should().Be(TestUtils.GetMinerTransaction());
            uut.Transactions[2].Should().Be(TestUtils.GetMinerTransaction());
        }

        [TestMethod]
        public void RebuildMerkleRoot_Updates()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            UInt256 merkleRoot = uut.MerkleRoot;

            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 3);
            uut.RebuildMerkleRoot();

            uut.MerkleRoot.Should().NotBe(merkleRoot);
        }

        [TestMethod]
        public void ToJson()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0x4520462a8c80056291f871da523bff0eb17e29d44ab4317e69ff7a42083cb39d");
            jObj["size"].AsNumber().Should().Be(119);
            jObj["version"].AsNumber().Should().Be(0);
            jObj["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            jObj["merkleroot"].AsString().Should().Be("0xfb5bd72b2d6792d75dc2f1084ffa9e9f70ca85543c717a6b13d9959b452a57d6");
            jObj["time"].AsNumber().Should().Be(4244941696);
            jObj["index"].AsNumber().Should().Be(0);
            jObj["nonce"].AsString().Should().Be("000000000000001e");
            jObj["nextconsensus"].AsString().Should().Be("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

            JObject scObj = jObj["script"];
            scObj["invocation"].AsString().Should().Be("");
            scObj["verification"].AsString().Should().Be("51");

            jObj["tx"].Should().NotBeNull();
            JArray txObj = (JArray)jObj["tx"];
            txObj[0]["txid"].AsString().Should().Be("0xfb5bd72b2d6792d75dc2f1084ffa9e9f70ca85543c717a6b13d9959b452a57d6");
            txObj[0]["size"].AsNumber().Should().Be(10);
            txObj[0]["type"].AsString().Should().Be("MinerTransaction");
            txObj[0]["version"].AsNumber().Should().Be(0);
            ((JArray)txObj[0]["attributes"]).Count.Should().Be(0);
            ((JArray)txObj[0]["vin"]).Count.Should().Be(0);
            ((JArray)txObj[0]["vout"]).Count.Should().Be(0);
            txObj[0]["sys_fee"].AsString().Should().Be("0");
            txObj[0]["net_fee"].AsString().Should().Be("0");
            ((JArray)txObj[0]["scripts"]).Count.Should().Be(0);
            txObj[0]["nonce"].AsNumber().Should().Be(2083236893);
        }

        [TestMethod]
        public void Verify_CompletelyFalse()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            uut.Verify(false).Should().BeTrue();
        }

        [TestMethod]
        public void Verify_CompletelyFalse_MinerTransaction_After_First()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 3);

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            uut.Verify(false).Should().BeFalse();
        }

        [TestMethod]
        public void Verify_CompletelyTrue_NextConsensus_Fail()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal, out transactionsVal, 1);
            // passing NextConsensus below 
            // uut.NextConsensus = new UInt160(new byte[] { 23, 52, 98, 203, 0, 206, 138, 37, 140, 16, 251, 231, 61, 120, 218, 200, 182, 125, 120, 73 });

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            uut.Verify(true).Should().BeFalse();
        }
    }
}
