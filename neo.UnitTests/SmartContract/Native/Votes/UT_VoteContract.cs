using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

namespace Neo.UnitTests.SmartContract.Native.Votes
{
    [TestClass]
    public class UT_VoteContract
    {
        Store store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void Check_GetVoteDetails()
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
            Transaction TestTx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                Sender = UInt160.Zero,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 1,
                Attributes = new TransactionAttribute[0] { },
                Cosigners = new Cosigner[0],
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
                UInt256 test = new UInt256(result);
            }

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(NativeContract.Vote.Hash, "getVoteDetails", result);
                var engine = ApplicationEngine.Run(sb.ToArray(), TestTx, testMode: true);
                result = engine.ResultStack.Pop().GetByteArray();
            }
        }
    }
}
