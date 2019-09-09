using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Votes.Model;
using Neo.VM;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Neo.IO;
using Neo.UnitTests.Extensions;

namespace Neo.UnitTests.SmartContract.Native.Votes
{
    [TestClass]
    public class UT_VoteContract
    {
        Store store;
        Transaction TestTx;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            store = TestBlockchain.GetStore();

            TestTx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                Sender = UInt160.Zero,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 1,
                Attributes = new TransactionAttribute[0] { },
                Cosigners = new Cosigner[]
                {
                    new Cosigner{Account = UInt160.Zero, Scopes = WitnessScope.Global, AllowedContracts = null, AllowedGroups = null}
                },
                Script = new byte[0],
                Witnesses = new Witness[0]
            };
        }

        [TestMethod]
        public void Check_SingleVote()
        {
            var snapshot = store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            ContractParameter[] createParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
                new ContractParameter(ContractParameterType.String) { Value = "Title"},
                new ContractParameter(ContractParameterType.String) { Value = "Description"},
                new ContractParameter(ContractParameterType.Integer){ Value = 2}
            };
            var ret = NativeContract.Vote.Call(snapshot, TestTx, "createSingleVote", createParameters).GetByteArray();

            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write(1);
                ContractParameter[] voteParameters = new ContractParameter[]
                {
                new ContractParameter(ContractParameterType.ByteArray){ Value = ret},
                new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
                new ContractParameter(ContractParameterType.ByteArray){ Value = memoryStream.ToArray()}
                };

                var vote = NativeContract.Vote.Call(snapshot, TestTx, "singleVote", voteParameters).GetBoolean();
                vote.Should().BeTrue();
            }

            ContractParameter[] resultParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.ByteArray){ Value = ret}
            };

            var result = NativeContract.Vote.Call(snapshot, TestTx, "getSingleStatistic", resultParameters).GetByteArray();
            using (MemoryStream memoryStream = new MemoryStream(result))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    int[] test = (int[])binaryFormatter.Deserialize(memoryStream);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        public void Check_MultiVote()
        {
            var snapshot = store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            ContractParameter[] createParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
                new ContractParameter(ContractParameterType.String) { Value = "Title"},
                new ContractParameter(ContractParameterType.String) { Value = "Description"},
                new ContractParameter(ContractParameterType.Integer){ Value = 2}
            };
            var ret = NativeContract.Vote.Call(snapshot, TestTx, "createMultiVote", createParameters).GetByteArray();



            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write(1);
                binaryWriter.Write(2);
                ContractParameter[] voteParameters = new ContractParameter[]
                {
                    new ContractParameter(ContractParameterType.ByteArray){ Value = ret},
                    new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
                    new ContractParameter(ContractParameterType.ByteArray){ Value = memoryStream.ToArray()}
                };

                var vote = NativeContract.Vote.Call(snapshot, TestTx, "multiVote", voteParameters).GetBoolean();
                vote.Should().BeTrue();
            }

            ContractParameter[] resultParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.ByteArray){ Value = ret}
            };
            var query = NativeContract.Vote.Call(snapshot, TestTx, "getVoteDetails", resultParameters).GetByteArray();
            using (MemoryStream memoryStream = new MemoryStream(query, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                try
                {
                    VoteCreateState state = new VoteCreateState();
                    state.Deserialize(binaryReader);
                }
                catch
                {
                    Assert.Fail();
                }
            }

            var result = NativeContract.Vote.Call(snapshot, TestTx, "getMultiStatistic", resultParameters).GetByteArray();
            using (MemoryStream memoryStream = new MemoryStream(result))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    int[,] test = (int[,])binaryFormatter.Deserialize(memoryStream);
                }
                catch
                {
                    Assert.Fail();
                }
            }

            var getResult = NativeContract.Vote.Call(snapshot, TestTx, "getResult", resultParameters).GetByteArray();
            using (MemoryStream memoryStream = new MemoryStream(getResult, false))
            {
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    int[,] test = (int[,])binaryFormatter.Deserialize(memoryStream);
                }
                catch
                {
                    Assert.Fail();
                }

            }
        }

        [TestMethod]
        public void Check_AccessControl()
        {
            var snapshot = store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            ContractParameter[] createParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
                new ContractParameter(ContractParameterType.String) { Value = "Title"},
                new ContractParameter(ContractParameterType.String) { Value = "Description"},
                new ContractParameter(ContractParameterType.Integer){ Value = 2}
            };
            var ret = NativeContract.Vote.Call(snapshot, TestTx, "createMultiVote", createParameters).GetByteArray();

            byte[] voterLists;
            bool isAdd = false;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write(new UInt160(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
                voterLists = memoryStream.ToArray();
            }

            ContractParameter[] AccessParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.Hash256){ Value = new UInt256(ret)},
                new ContractParameter(ContractParameterType.ByteArray) { Value = voterLists},
                new ContractParameter(ContractParameterType.Boolean) { Value = isAdd}
            };
            try
            {
                var result = NativeContract.Vote.Call(snapshot, TestTx, "accessControl", AccessParameters).GetBoolean();
                result.Should().BeTrue();
            }
            catch
            {
                Assert.Fail();
            }
        }
    }
}
