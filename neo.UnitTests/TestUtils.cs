using System;
using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.VM;
using Neo.Wallets;
using Neo.SmartContract;

namespace Neo.UnitTests
{
    public static class TestUtils
    {
        public static byte[] GetByteArray(int length, byte firstByte)
        {            
            byte[] array = new byte[length];
            array[0] = firstByte;
            for (int i = 1; i < length; i++)
            {
                array[i] = 0x20;
            }
            return array;
        }

        public static readonly ECPoint[] StandbyValidators = new ECPoint[] { ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1) };

        public static ClaimTransaction GetClaimTransaction()
        {
            return new ClaimTransaction
            {
                Claims = new CoinReference[0],
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                Scripts = new Witness[0]
            };
        }

        public static MinerTransaction GetMinerTransaction()
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

        public static IssueTransaction GetIssueTransaction(bool inputVal, decimal outputVal, UInt256 assetId)
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

        public static CoinReference GetCoinReference(UInt256 prevHash)
        {
            if (prevHash == null) prevHash = UInt256.Zero;
            return new CoinReference
            {
                PrevHash = prevHash,
                PrevIndex = 0
            };
        }

        public static void SetupTestBlockchain(UInt256 assetId)
        {
            Blockchain testBlockchain = new TestBlockchain(assetId);
            Blockchain.RegisterBlockchain(testBlockchain);
        }

        public static void SetupHeaderWithValues(Header header, UInt256 val256, out UInt256 merkRootVal, out UInt160 val160, out uint timestampVal, out uint indexVal, out ulong consensusDataVal, out Witness scriptVal)
        {
            setupBlockBaseWithValues(header, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);
        }

        public static void SetupBlockWithValues(Block block, UInt256 val256, out UInt256 merkRootVal, out UInt160 val160, out uint timestampVal, out uint indexVal, out ulong consensusDataVal, out Witness scriptVal, out Transaction[] transactionsVal, int numberOfTransactions)
        {
            setupBlockBaseWithValues(block, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            transactionsVal = new Transaction[numberOfTransactions];
            if (numberOfTransactions > 0)
            {
                for (int i = 0; i < numberOfTransactions; i++)
                {
                    transactionsVal[i] = TestUtils.GetMinerTransaction();
                }
            }

            block.Transactions = transactionsVal;
        }

        private static void setupBlockBaseWithValues(BlockBase bb, UInt256 val256, out UInt256 merkRootVal, out UInt160 val160, out uint timestampVal, out uint indexVal, out ulong consensusDataVal, out Witness scriptVal)
        {
            bb.PrevHash = val256;
            merkRootVal = new UInt256(new byte[] { 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251 });
            bb.MerkleRoot = merkRootVal;
            timestampVal = new DateTime(1968, 06, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
            bb.Timestamp = timestampVal;
            indexVal = 0;
            bb.Index = indexVal;
            consensusDataVal = 30;
            bb.ConsensusData = consensusDataVal;
            val160 = UInt160.Zero;
            bb.NextConsensus = val160;
            scriptVal = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            };
            bb.Script = scriptVal;
        }
    }    
}
