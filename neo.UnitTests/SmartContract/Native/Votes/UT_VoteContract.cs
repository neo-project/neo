using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Network.P2P.Payloads;
using System.Linq;

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



        //[TestMethod]
        //public void Check_CreateSingleVote()
        //{
        //    var snapshot = store.GetSnapshot().Clone();

        //    snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
        //    snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });


        //    ContractParameter[] parameters = new ContractParameter[] {
        //        new ContractParameter(ContractParameterType.Hash160){ Value = UInt160.Zero},
        //        new ContractParameter(ContractParameterType.String){ Value = "TitleTest"},
        //        new ContractParameter(ContractParameterType.String){ Value = "DescriptionTest"},
        //        new ContractParameter(ContractParameterType.Integer){ Value = 1}
        //    };
        //    var ret = NativeContract.Vote.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), "creatSingleVote", parameters);
        //    ret.Should().BeOfType<UInt160>();
        //}

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
                Version = 01,
                Nonce = 1,
                Sender = UInt160.Zero,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 1,
                Attributes = new TransactionAttribute[0] { },
                Script = new byte[0],
                Witnesses = new Witness[0]           
            };
            var engine = new ApplicationEngine(TriggerType.Application, TestTx, snapshot, 0, true);
            engine.LoadScript(NativeContract.Vote.Script);

            var script = new ScriptBuilder();

            for (var i = createParameters.Length - 1; i >= 0; i--)
            {
                script.EmitPush(createParameters[i]);
            }

            script.EmitPush(createParameters.Length);
            script.Emit(OpCode.PACK);
            script.EmitPush("createSingleVote");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() != VMState.HALT)
            {
                throw new System.Exception();
            }
            engine.ResultStack.Pop().Should().BeOfType<bool>();



            ContractParameter[] parameters = new ContractParameter[]
            {
                new ContractParameter(ContractParameterType.Hash256){ Value = new UInt256()}
            };
            var engine1 = new ApplicationEngine(TriggerType.Application, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);


            engine1.LoadScript(NativeContract.Vote.Script);

            script = new ScriptBuilder();

            for (var i = parameters.Length - 1; i >= 0; i--)
            {
                script.EmitPush(parameters[i]);
            }

            script.EmitPush(parameters.Length);
            script.Emit(OpCode.PACK);
            script.EmitPush("getVoteDetails");
            engine1.LoadScript(script.ToArray());

            if (engine1.Execute() != VMState.HALT)
            {
                throw new System.Exception();
            }

            engine.ResultStack.Pop().Should().BeOfType<byte[]>();
        }
    }
}
