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
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };
            NativeContract.NNS.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));
            NativeContract.NEO.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));
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
        public void Check_Policy()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            //check_setAdmin
            var ret_setAdmin = Check_SetAdmin(snapshot, from);
            ret_setAdmin.Result.Should().BeTrue();
            ret_setAdmin.State.Should().BeTrue();

            //check_getAdmin
            var ret_getAdmin = Check_GetAdmin(snapshot);
            ret_getAdmin.Result.GetSpan().AsSerializable<UInt160>().Should().Be(new UInt160(from));
            ret_getAdmin.State.Should().BeTrue();

            //check_setReceiptAddress
            var ret_setReceiptAddress = Check_SetReceiptAddress(snapshot, from);
            ret_setReceiptAddress.Result.Should().BeTrue();
            ret_setReceiptAddress.State.Should().BeTrue();

            //check_getReceiptAddress
            var ret_getReceiptAddress = Check_GetReceiptAddress(snapshot);
            ret_getReceiptAddress.Result.GetSpan().AsSerializable<UInt160>().Should().Be(new UInt160(from));
            ret_getReceiptAddress.State.Should().BeTrue();

            //check_setRentalPrice
            var ret_setRentalPrice = Check_SetRentalPrice(snapshot, from, 10000000000L);
            ret_setRentalPrice.Result.Should().BeTrue();
            ret_setRentalPrice.State.Should().BeTrue();

            //check_getRentalPrice
            var ret_getRentalPrice = Check_GetRentalPrice(snapshot);
            ret_getRentalPrice.Result.GetBigInteger().Should().Be(10000000000L);
            ret_getRentalPrice.State.Should().BeTrue();
        }
        internal static (bool State, bool Result) Check_SetAdmin(StoreView snapshot, byte[] account)
        {
            var address = NativeContract.NEO.GetCommitteeMultiSigAddress(snapshot);
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(address), snapshot, 0, true);

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

        internal static (bool State, StackItem Result) Check_GetAdmin(StoreView snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getAdmin");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteString));

            return (true, result);
        }

        internal static (bool State, bool Result) Check_SetReceiptAddress(StoreView snapshot, byte[] account)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(new UInt160(account)), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(account);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("setReceiptAddress");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        internal static (bool State, StackItem Result) Check_GetReceiptAddress(StoreView snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getReceiptAddress");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteString));

            return (true, result);
        }

        internal static (bool State, bool Result) Check_SetRentalPrice(StoreView snapshot, byte[] account, BigInteger amount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(new UInt160(account)), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(amount);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("setRentalPrice");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        internal static (bool State, StackItem Result) Check_GetRentalPrice(StoreView snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getRentalPrice");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return (true, result);
        }

        [TestMethod]
        public void Check_RegisterCenter_RegisterRootName()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var ret = Check_GetAdmin(snapshot);
            var from = ret.Result.GetSpan().AsSerializable<UInt160>();

            //check_registerRootName
            var ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(true);
            ret_registerRootName.State.Should().BeTrue();

            //check_registerRootName double register wrong
            ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(false);
            ret_registerRootName.State.Should().BeTrue();

            //check_registerRootName wrong format
            ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"));
            ret_registerRootName.Result.Should().Be(false);
            ret_registerRootName.State.Should().BeTrue();

            //check_registerRootName wrong witness
            ret_registerRootName = Check_RegisterRootName(snapshot, UInt160.Zero.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(false);
            ret_registerRootName.State.Should().BeTrue();

            //check_getRootName
            var ret_getRootName = Check_GetRootName(snapshot);
            IEnumerator eumerator_RootName = ((InteropInterface)ret_getRootName.Result).GetInterface<IEnumerator>();
            eumerator_RootName.MoveNext().Should().BeTrue();
            eumerator_RootName.Current.Equals("AA").Should().Be(true);
            ret_getRootName.State.Should().BeTrue();
        }

        [TestMethod]
        public void Check_RegisterCenter_Transfer()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var factor = 1;
            var ret = Check_GetAdmin(snapshot);
            var from = ret.Result.GetSpan().AsSerializable<UInt160>();

            //check_registerRootName
            var ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(true);
            ret_registerRootName.State.Should().BeTrue();

            //check_getRootName
            var ret_getRootName = Check_GetRootName(snapshot);
            IEnumerator eumerator_RootName = ((InteropInterface)ret_getRootName.Result).GetInterface<IEnumerator>();
            eumerator_RootName.MoveNext().Should().BeTrue();
            eumerator_RootName.Current.Equals("AA").Should().Be(true);
            ret_getRootName.State.Should().BeTrue();

            //check_transfer wrong amount format
            var ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), factor + 1, true);
            ret_transfer.Result.Should().BeFalse();
            ret_transfer.State.Should().BeFalse();

            //check_transfer wrong domain level
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA.AA.AA.AA"), factor, true);
            ret_transfer.Result.Should().BeFalse();
            ret_transfer.State.Should().BeTrue();

            //check_transfer transfer root domain wrong
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"), factor, true);
            ret_transfer.Result.Should().BeFalse();
            ret_transfer.State.Should().BeTrue();

            //check_transfer cross-level wrong
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA.AA"), factor, true);
            ret_transfer.Result.Should().BeFalse();
            ret_transfer.State.Should().BeTrue();

            //check_transfer_create_first-level_domain
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            snapshot.BlockHashIndex.GetAndChange().Index = 1;

            //check_transfer expired domain
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            //check_transfer_create_second&third-level_domain
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            //check_transfer_from_A_to_B
            byte[] to = Contract.CreateSignatureContract(Blockchain.StandbyValidators[0]).ScriptHash.ToArray();
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), to, System.Text.Encoding.UTF8.GetBytes("AA.AA.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();
        }

        [TestMethod]
        public void Check_RegisterCenter_Renew()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var factor = 1;
            var ret = Check_GetAdmin(snapshot);
            var from = ret.Result.GetSpan().AsSerializable<UInt160>();
            UInt160 account = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators);

            //check_registerRootName
            var ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(true);
            ret_registerRootName.State.Should().BeTrue();

            //check_getRootName
            var ret_getRootName = Check_GetRootName(snapshot);
            IEnumerator eumerator_RootName = ((InteropInterface)ret_getRootName.Result).GetInterface<IEnumerator>();
            eumerator_RootName.MoveNext().Should().BeTrue();
            eumerator_RootName.Current.Equals("AA").Should().Be(true);
            ret_getRootName.State.Should().BeTrue();

            //check_transfer_create_first-level_domain
            var ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            snapshot.BlockHashIndex.GetAndChange().Index = 2000001;
            //check_renewName
            var ret_renewName = Check_RenewName(snapshot, from.ToArray(), account, System.Text.Encoding.UTF8.GetBytes("AA.AA"), 100000000L);
            ret_renewName.Result.Should().BeTrue();
            ret_renewName.State.Should().BeTrue();
        }

        internal static (bool State, bool Result) Check_RenewName(StoreView snapshot, byte[] from, UInt160 account, byte[] tokenId, BigInteger height)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(account), snapshot, 0, true);

            engine.LoadScript(NativeContract.NNS.Script);

            var script = new ScriptBuilder();

            script.EmitPush(account);
            script.EmitPush(height);
            script.EmitPush(tokenId);
            script.EmitPush(3);
            script.Emit(OpCode.PACK);
            script.EmitPush("renewName");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.ToBoolean());
        }

        [TestMethod]
        public void Check_Resolver()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var factor = 1;
            var ret = Check_GetAdmin(snapshot);
            var from = ret.Result.GetSpan().AsSerializable<UInt160>();
            snapshot.BlockHashIndex.GetAndChange().Index = 0;

            //check_registerRootName
            var ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(true);
            ret_registerRootName.State.Should().BeTrue();

            //check_getRootName
            var ret_getRootName = Check_GetRootName(snapshot);
            IEnumerator eumerator_RootName = ((InteropInterface)ret_getRootName.Result).GetInterface<IEnumerator>();
            eumerator_RootName.MoveNext().Should().BeTrue();
            eumerator_RootName.Current.Equals("AA").Should().Be(true);

            //check_transfer_create_first-level_domain
            var ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            //check_transfer_create_first-level_domain
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("BB.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            //check_transfer_create_first-level_domain
            ret_transfer = Check_Transfer(snapshot, from.ToArray(), from.ToArray(), System.Text.Encoding.UTF8.GetBytes("CC.AA"), factor, true);
            ret_transfer.Result.Should().BeTrue();
            ret_transfer.State.Should().BeTrue();

            //check_ownerof
            var ret_OwnerOf = Check_OwnerOf(snapshot, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            IEnumerator eumerator_OwnerOf = ((InteropInterface)ret_OwnerOf.Result).GetInterface<IEnumerator>();
            eumerator_OwnerOf.MoveNext().Should().BeTrue();
            eumerator_OwnerOf.Current.Should().Be(from);
            ret_OwnerOf.State.Should().BeTrue();

            //check_tokensof
            var ret_TokensOf = Check_TokensOf(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            IEnumerator eumerator_TokensOf = ((InteropInterface)ret_TokensOf.Result).GetInterface<IEnumerator>();
            eumerator_TokensOf.MoveNext().Should().BeTrue();
            ((DomainState)(eumerator_TokensOf.Current)).Name.Equals("CC.AA").Should().Be(true);
            ret_TokensOf.State.Should().BeTrue();

            //check_balanceOf
            var ret_BalanceOf = Check_BalanceOf(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_BalanceOf.Result.GetBigInteger().Should().Be(1);
            ret_BalanceOf.State.Should().BeTrue();

            //check_getProperties
            var ret_properties = Check_GetProperties(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_properties.Result.GetString().Should().Be("{}");
            ret_properties.State.Should().BeTrue();

            //check_totalSupply
            var ret_totalSupply = Check_TotalSupply(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_totalSupply.Result.GetBigInteger().Should().Be(4);
            ret_totalSupply.State.Should().BeTrue();

            //check_setText
            var ret_setText = Check_SetText(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), "BBB", 0, true);
            ret_setText.Result.Should().Be(true);
            ret_setText.State.Should().BeTrue();

            //check_setText
            ret_setText = Check_SetText(snapshot, from, System.Text.Encoding.UTF8.GetBytes("BB.AA"), "AA.AA", 1, true);
            ret_setText.Result.Should().Be(true);
            ret_setText.State.Should().BeTrue();

            //check_setText
            ret_setText = Check_SetText(snapshot, from, System.Text.Encoding.UTF8.GetBytes("CC.AA"), "CC.AA", 3, true);
            ret_setText.Result.Should().Be(true);
            ret_setText.State.Should().BeTrue();

            //check_setText witness wrong
            ret_setText = Check_SetText(snapshot, UInt160.Zero, System.Text.Encoding.UTF8.GetBytes("CC.AA"), "CC.AA", 3, true);
            ret_setText.Result.Should().Be(false);
            ret_setText.State.Should().BeTrue();

            //check_setText no token
            ret_setText = Check_SetText(snapshot, UInt160.Zero, System.Text.Encoding.UTF8.GetBytes("EE.AA"), "CC.AA", 3, true);
            ret_setText.Result.Should().Be(false);
            ret_setText.State.Should().BeTrue();

            //check_setOperator
            var ret_setOperator = Check_SetOperator(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_setOperator.Result.Should().Be(true);
            ret_setOperator.State.Should().BeTrue();

            //check_resolve
            var ret_resolve = Check_Resolve(snapshot, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            Struct @struct = (Struct)ret_resolve.Result;
            @struct[0].GetSpan().ToArray().Should().BeEquivalentTo(new byte[] { 0x00 });
            @struct[1].Should().Be((ByteString)"BBB");
            ret_resolve.State.Should().BeTrue();

            //check_resolve
            ret_resolve = Check_Resolve(snapshot, System.Text.Encoding.UTF8.GetBytes("BB.AA"), true);
            @struct = (Struct)ret_resolve.Result;
            @struct[0].GetSpan().ToArray().Should().BeEquivalentTo(new byte[] { 0x00 });
            @struct[1].Should().Be((ByteString)"BBB");
            ret_resolve.State.Should().BeTrue();

            //check_resolve
            ret_resolve = Check_Resolve(snapshot, System.Text.Encoding.UTF8.GetBytes("DD.AA"), true);
            @struct = (Struct)ret_resolve.Result;
            @struct[0].GetSpan().ToArray().Should().BeEquivalentTo(new byte[] { 0x04 });
            @struct[1].Should().Be((ByteString)"Text does not exist");
            ret_resolve.State.Should().BeTrue();
        }

        [TestMethod]
        public void Check_Operator()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var ret = Check_GetAdmin(snapshot);
            var from = ret.Result.GetSpan().AsSerializable<UInt160>();
            snapshot.BlockHashIndex.GetAndChange().Index = 0;

            //check_registerRootName
            var ret_registerRootName = Check_RegisterRootName(snapshot, from.ToArray(), System.Text.Encoding.UTF8.GetBytes("AA"));
            ret_registerRootName.Result.Should().Be(true);
            ret_registerRootName.State.Should().BeTrue();

            //check_getRootName
            var ret_getRootName = Check_GetRootName(snapshot);
            IEnumerator eumerator_RootName = ((InteropInterface)ret_getRootName.Result).GetInterface<IEnumerator>();
            eumerator_RootName.MoveNext().Should().BeTrue();
            eumerator_RootName.Current.Equals("AA").Should().Be(true);

            //check_setOperator
            var ret_setOperator = Check_SetOperator(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA"), true);
            ret_setOperator.Result.Should().Be(true);
            ret_setOperator.State.Should().BeTrue();

            //check_setOperator cross-level
            ret_setOperator = Check_SetOperator(snapshot, from, System.Text.Encoding.UTF8.GetBytes("AA.AA.AA.AA"), true);
            ret_setOperator.Result.Should().Be(false);
            ret_setOperator.State.Should().BeTrue();
        }

        [TestMethod()]
        public void IsDomainTest()
        {
            Assert.IsFalse(NativeContract.NNS.IsDomain(""));
            Assert.IsFalse(NativeContract.NNS.IsDomain(null));
            Assert.IsFalse(NativeContract.NNS.IsDomain("www,neo.org"));
            Assert.IsFalse(NativeContract.NNS.IsDomain("www.hello.world.neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("www.hello.neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("www.neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("bb.aa123"));
        }

        internal static (bool State, StackItem Result) Check_Resolve(StoreView snapshot, byte[] tokenId, bool signAccount)
        {
            var address = NativeContract.NEO.GetCommitteeMultiSigAddress(snapshot);
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);

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
            result.Should().BeOfType(typeof(VM.Types.Struct));

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

            script.EmitPush(text);
            script.EmitPush(recordType);
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

        internal static (bool State, bool Result) Check_RegisterRootName(StoreView snapshot, byte[] account, byte[] tokenId)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(new UInt160(account)), snapshot, 0, true);

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

        internal static (bool State, StackItem Result) Check_OwnerOf(StoreView snapshot, byte[] tokenId, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);

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


        internal static (bool State, StackItem Result) Check_GetRootName(StoreView snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);

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
