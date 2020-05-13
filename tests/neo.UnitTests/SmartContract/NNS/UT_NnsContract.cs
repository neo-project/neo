using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.SmartContract.NNS;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_NnsContract
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Check_Name() => NativeContract.NNS.Name().Should().Be("NNS");

        [TestMethod]
        public void Check_Symbol() => NativeContract.NNS.Symbol().Should().Be("nns");

        [TestMethod]
        public void Check_Decimals() => NativeContract.NNS.Decimals().Should().Be(0);

        [TestMethod]
        public void Check_SupportedStandards() => NativeContract.NNS.SupportedStandards().Should().BeEquivalentTo(new string[] { "NEP-11", "NEP-10" });

        [TestMethod]
        public void Check_All()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };
            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            NativeContract.NNS.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));
            var factor = 1;
            var supply = NativeContract.NNS.TotalSupply(snapshot);
            supply.Should().Be(0);

            //check_setAdmin
            var ret = Check_SetAdmin(snapshot, from, true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            //check_setRootName
            ret = Check_RegisterRootName(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA"), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            //check_getRootName
            var ret_getRootName = Check_GetRootName(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA"), true);
            IEnumerator eumerator_RootName = ((InteropInterface)ret_getRootName.Result).GetInterface<IEnumerator>();
            eumerator_RootName.MoveNext().Should().BeTrue();
            eumerator_RootName.Current.Equals("AA").Should().Be(true);
            ret_getRootName.State.Should().BeTrue();

            //check_transfer_create_first-level_domain
            ret = Check_Transfer(snapshot, from, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), factor, true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            //check_ownerof
            var ret_OwnerOf = Check_OwnerOf(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            IEnumerator eumerator_OwnerOf = ((InteropInterface)ret_OwnerOf.Result).GetInterface<IEnumerator>();
            eumerator_OwnerOf.MoveNext().Should().BeTrue();
            eumerator_OwnerOf.Current.Equals(Blockchain.GetConsensusAddress(Blockchain.StandbyValidators)).Should().Be(true);
            ret_OwnerOf.State.Should().BeTrue();

            //check_tokensof
            var ret_TokensOf = Check_TokensOf(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            IEnumerator eumerator_TokensOf = ((InteropInterface)ret_TokensOf.Result).GetInterface<IEnumerator>();
            eumerator_TokensOf.MoveNext().Should().BeTrue();
            ((DomainState)(eumerator_TokensOf.Current)).Name.Equals("AA.AA").Should().Be(true);
            ret_TokensOf.State.Should().BeTrue();

            //check_balanceOf
            var ret_BalanceOf = Check_BalanceOf(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_BalanceOf.Result.GetBigInteger().Should().Be(1);
            ret_BalanceOf.State.Should().BeTrue();

            //check_transfer_create_second/third-level_domain
            ret = Check_Transfer(snapshot, from, from, System.Text.Encoding.UTF8.GetBytes("AA.AA.AA"), factor, true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            //check_transfer_from_A_to_B
            byte[] to = Contract.CreateSignatureContract(Blockchain.StandbyValidators[0]).ScriptHash.ToArray();
            ret = Check_Transfer(snapshot, from, to, System.Text.Encoding.UTF8.GetBytes("AA.AA.AA"), factor, true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            //check_getProperties
            var ret_properties = Check_GetProperties(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_properties.Result.GetString().Should().Be("{}");
            ret_properties.State.Should().BeTrue();

            //check_totalSupply
            var ret_totalSupply = Check_TotalSupply(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_totalSupply.Result.GetBigInteger().Should().Be(3);
            ret_totalSupply.State.Should().BeTrue();

            //check_setText
            var ret_setText = Check_SetText(snapshot, new UInt160(from), System.Text.Encoding.UTF8.GetBytes("AA.AA"), "BBB", 0, true);
            ret_setText.Result.Should().Be(true);
            ret_setText.State.Should().BeTrue();

            //check_setOperator
            var ret_setOperator = Check_SetOperator(snapshot, new UInt160(from), System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_setOperator.Result.Should().Be(true);
            ret_setOperator.State.Should().BeTrue();

            //check_resolve
            var ret_resolve = Check_Resolve(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_resolve.Result.GetString().Should().Be("{\"text\":\"BBB\",\"recordType\":\"A\"}");
            ret_resolve.State.Should().BeTrue();
        }

        internal static (bool State, StackItem Result) Check_Resolve(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var address = NativeContract.NEO.GetCommitteeMultiSigAddress(snapshot);
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? address : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(tokenId);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("resolve");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteString));

            return (true, result);
        }

        internal static (bool State, bool Result) Check_SetOperator(StoreView snapshot, UInt160 account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? account : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(account);
            script.EmitPush(tokenId);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("setOperator");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        internal static (bool State, bool Result) Check_SetText(StoreView snapshot, UInt160 account, byte[] tokenId, String text, int recordType, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? account : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(recordType);
            script.EmitPush(text);
            script.EmitPush(tokenId);
            script.EmitPush(3);
            script.Emit(OpCode.PACK);
            script.EmitPush("setText");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        internal static (bool State, bool Result) Check_SetAdmin(StoreView snapshot, byte[] account, bool signAccount)
        {
            var address = NativeContract.NEO.GetCommitteeMultiSigAddress(snapshot);
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? address : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(account);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("setAdmin");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterRootName(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(tokenId);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("registerRootName");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        internal static (bool State, StackItem Result) Check_OwnerOf(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(tokenId);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("ownerOf");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.InteropInterface));

            return (true, result);
        }

        internal static (bool State, StackItem Result) Check_TokensOf(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(account);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("tokensOf");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.InteropInterface));

            return (true, result);
        }

        internal static (bool State, StackItem Result) Check_BalanceOf(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(tokenId);
            script.EmitPush(account);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("balanceOf");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return (true, result);
        }

        internal static (bool State, StackItem Result) Check_GetProperties(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(account);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("properties");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteString));

            return (true, result);
        }

        internal static (bool State, StackItem Result) Check_TotalSupply(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("totalSupply");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return (true, result);
        }


        internal static (bool State, StackItem Result) Check_GetRootName(StoreView snapshot, byte[] account, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getRootName");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.InteropInterface));

            return (true, result);
        }

        internal static (bool State, bool Result) Check_Transfer(StoreView snapshot, byte[] from, byte[] to, byte[] tokenId, BigInteger amount, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(from) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(tokenId);
            script.EmitPush(amount);
            script.EmitPush(to);
            script.EmitPush(from);
            script.EmitPush(4);
            script.Emit(OpCode.PACK);
            script.EmitPush("transfer");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }
    }
}
