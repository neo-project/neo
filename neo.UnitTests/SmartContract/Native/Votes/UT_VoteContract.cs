using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Votes.Model;
using Neo.VM;
using System.IO;
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
        public void Check_CreateSingleVote()
        {
            var snapshot = store.GetSnapshot().Clone();

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });
            Transaction TestTx = new Transaction
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
            object[] parameter = new object[] { UInt160.Zero, "Title", "Descritpion", 1 };
            byte[] result;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(NativeContract.Vote.Hash , "createSingleVote",parameter );
                var engine = ApplicationEngine.Run(sb.ToArray(),TestTx, testMode: true);
                result = engine.ResultStack.Pop().GetByteArray();
                result.Length.Should().Be(32);
            }
        }

        [TestMethod]
        public void Check_CreateMultiVote()
        {
            var snapshot = store.GetSnapshot().Clone();

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });


            ContractParameter[] createParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
                new ContractParameter(ContractParameterType.String) {Value = "Title"},
                new ContractParameter(ContractParameterType.String) { Value = "Description"},
                new ContractParameter(ContractParameterType.Integer){ Value = 1}
            };
            var ret = NativeContract.Vote.Call(snapshot, TestTx, "createMultiVote", createParameters).GetByteArray();
            ret.Length.Should().Be(32);

            ContractParameter[] getParameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.ByteArray){ Value = ret}
            };
            var Detail = NativeContract.Vote.Call(snapshot, TestTx, "getVoteDetails", getParameters).GetByteArray();
            VoteCreateState state = new VoteCreateState();
            using (MemoryStream memoryStream = new MemoryStream(Detail, false))
            using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
                state.Deserialize(binaryReader);
            }
            state.CandidateNumber.ShouldBeEquivalentTo(1);
        }
    }
}
