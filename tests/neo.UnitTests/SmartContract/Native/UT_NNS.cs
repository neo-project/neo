using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NNS
    {
        private DataCache _snapshot;
        private Block _persistingBlock;

        private const byte Prefix_Roots = 10;
        private const byte Prefix_DomainPrice = 22;
        private const byte Prefix_Expiration = 20;
        private const byte Prefix_Record = 12;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            _snapshot = Blockchain.Singleton.GetSnapshot();
            _persistingBlock = new Block() { Index = 0, Transactions = Array.Empty<Transaction>(), ConsensusData = new ConsensusData() };
        }

        [TestMethod]
        public void Check_Name() => NativeContract.NameService.Name.Should().Be(nameof(NameService));

        [TestMethod]
        public void Check_Symbol() => NativeContract.NameService.Symbol(_snapshot).Should().Be("NNS");

        [TestMethod]
        public void Test_SetRecord_A_AAAA()
        {
            var snapshot = _snapshot.CreateSnapshot();
            // Fake blockchain

            var persistingBlock = new Block() { Index = 1000 };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // committee member,add a new root and then register, setrecord
            string validroot = "testroot";
            var ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);
            ret.Should().BeTrue();

            string name = "testname";
            string domain = name + "." + validroot;

            //before register 
            var checkAvail_ret = Check_IsAvailable(snapshot, UInt160.Zero, domain, persistingBlock);
            checkAvail_ret.Result.Should().BeTrue();
            checkAvail_ret.State.Should().BeTrue();

            byte[] from = Contract.GetBFTAddress(Blockchain.StandbyValidators).ToArray();
            var register_ret = Check_Register(snapshot, domain, from, persistingBlock);
            register_ret.Result.Should().BeTrue();
            register_ret.State.Should().BeTrue();

            //check NFT token
            Assert.AreEqual(NativeContract.NameService.BalanceOf(snapshot, from), (BigInteger)1);

            //after register
            checkAvail_ret = Check_IsAvailable(snapshot, UInt160.Zero, domain, persistingBlock);
            checkAvail_ret.Result.Should().BeTrue();
            checkAvail_ret.State.Should().BeFalse();

            //test As IPv4 Address
            bool setRecord_ret;
            (bool State, string Result) getRecord_ret;

            foreach (var testTrue in new string[] {
                "0.0.0.0", "10.10.10.10", "255.255.255.255","192.168.1.1"
            })
            {
                setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.A, testTrue, from, persistingBlock);
                setRecord_ret.Should().BeTrue();
                getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.A, persistingBlock);
                getRecord_ret.State.Should().BeTrue();
                getRecord_ret.Result.Should().Equals(testTrue);
            }

            //test As IPv6 Address
            foreach (var testTrue in new string[] {
                "2001:DB8::8:800:200C:417A", "FF01::101", "fF01::101", "::1", "::", "::13.1.68.3",
                "2001:DB8:0:0:8:800:200C:417A", "FF01:0:0:0:0:0:0:101", "0:0:0:0:0:0:0:1", "0:0:0:0:0:0:0:0"
            })
            {
                setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, testTrue, from, persistingBlock);
                setRecord_ret.Should().BeTrue();
                getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.AAAA, persistingBlock);
                getRecord_ret.State.Should().BeTrue();
                getRecord_ret.Result.Should().Equals(testTrue);
            }

            //invalid A case
            foreach (var testFalse in new string[] {
                "1a", "256.0.0.0", "01.01.01.01", "00.0.0.0", "0.0.0.-1",
                "0.0.0.0.1", "11111111.11111111.11111111.11111111",
                "11111111.11111111.11111111.11111111", "ff.ff.ff.ff", "0.0.256",
                "0.0.0", "0.257", "1.1", "257",
                "1"
            })
            {
                setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.A, testFalse, from, persistingBlock);
                setRecord_ret.Should().BeFalse();
            }

            //invalid AAAA case
            foreach (var testFalse in new string[] {
                "2001:DB8:0:0:8:800:200C:4Z"
            })
            {
                setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, testFalse, from, persistingBlock);
                setRecord_ret.Should().BeFalse();
            }
        }

        [TestMethod]
        public void Test_SetRecord_CNAME()
        {
            var snapshot = _snapshot.CreateSnapshot();
            // Fake blockchain

            var persistingBlock = new Block() { Index = 1000 };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // committee member,add a new root and then register, setrecord
            string validroot = "testroot";
            var ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);

            string name = "testname";

            string domain = name + "." + validroot;

            byte[] from = Contract.GetBFTAddress(Blockchain.StandbyValidators).ToArray();
            var register_ret = Check_Register(snapshot, domain, from, persistingBlock);
            register_ret.Result.Should().BeTrue();
            register_ret.State.Should().BeTrue();

            //set as CNAME
            string testCName = "a1.b1";
            var setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, testCName, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            var getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.CNAME, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testCName);

            testCName = "a1.b1.c1";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, testCName, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.CNAME, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testCName);

            testCName = "1a.b1";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, testCName, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.CNAME, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testCName);

            //invalid case
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "a1", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "a1.", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "..", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "\n.", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "  ", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "A1.b1", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "a1.B1", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.CNAME, "1a.1b", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

        }

        [TestMethod]
        public void Test_SetRecord_TxT()
        {
            var snapshot = _snapshot.CreateSnapshot();
            // Fake blockchain

            var persistingBlock = new Block() { Index = 1000 };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // committee member,add a new root and then register, setrecord
            string validroot = "testroot";
            var ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);

            string name = "testname";
            string domain = name + "." + validroot;

            //before register 
            var checkAvail_ret = Check_IsAvailable(snapshot, UInt160.Zero, domain, persistingBlock);
            checkAvail_ret.Result.Should().BeTrue();
            checkAvail_ret.State.Should().BeTrue();

            byte[] from = Contract.GetBFTAddress(Blockchain.StandbyValidators).ToArray();
            var register_ret = Check_Register(snapshot, domain, from, persistingBlock);
            register_ret.Result.Should().BeTrue();
            register_ret.State.Should().BeTrue();

            //set as txt
            string testTxt = "testtxt";
            var setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            var getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            testTxt = ".";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            testTxt = "\n";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            testTxt = "10.10.10.10";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            testTxt = "2001:0000:1F1F:0000:0000:0100:11A0:ADDF";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            testTxt = "";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            testTxt = "   ";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.TXT, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testTxt);

            //invalid case

            testTxt = "a";
            for (int i = 0; i < 8; i++) testTxt = testTxt + testTxt;
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.TXT, testTxt, from, persistingBlock);
            setRecord_ret.Should().BeFalse();
        }

        [TestMethod]
        public void Test_SetRecord_IPV6()
        {
            var snapshot = _snapshot.CreateSnapshot();
            // Fake blockchain

            var persistingBlock = new Block() { Index = 1000 };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // committee member,add a new root and then register, setrecord
            string validroot = "testroot";
            var ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);

            string name = "testname";
            string domain = name + "." + validroot;

            //before register 
            var checkAvail_ret = Check_IsAvailable(snapshot, UInt160.Zero, domain, persistingBlock);
            checkAvail_ret.Result.Should().BeTrue();
            checkAvail_ret.State.Should().BeTrue();

            byte[] from = Contract.GetBFTAddress(Blockchain.StandbyValidators).ToArray();
            var register_ret = Check_Register(snapshot, domain, from, persistingBlock);
            register_ret.Result.Should().BeTrue();
            register_ret.State.Should().BeTrue();

            //set as IPV6 address
            string testAAAA = "2001:0000:1F1F:0000:0000:0100:11A0:ADDF";
            var setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, testAAAA, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            var getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.AAAA, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testAAAA);

            testAAAA = "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff";
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, testAAAA, from, persistingBlock);
            setRecord_ret.Should().BeTrue();

            getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.AAAA, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testAAAA);

            //invalid case
            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, "10.10.10.10", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, "", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, "\n", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, ": : : : : : :", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.AAAA, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffg", from, persistingBlock);
            setRecord_ret.Should().BeFalse();

        }


        [TestMethod]
        public void Test_AddRootValid()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = _persistingBlock;

            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            var ret = Check_AddRoot(snapshot, committeeAddress, "a", persistingBlock);
            ret.Should().BeTrue();

            ret = Check_AddRoot(snapshot, committeeAddress, "a1", persistingBlock);
            ret.Should().BeTrue();

            ret = Check_AddRoot(snapshot, committeeAddress, "aw", persistingBlock);
            ret.Should().BeTrue();

            ret = Check_AddRoot(snapshot, committeeAddress, "a123456789123456", persistingBlock);
            ret.Should().BeTrue();

            ret = Check_AddRoot(snapshot, committeeAddress, "abcdefg", persistingBlock);
            ret.Should().BeTrue();

        }

        [TestMethod]
        public void Test_AddRootInvalid()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = _persistingBlock;
            //non-committee member
            string validroot = "testroot";
            byte[] from = Contract.GetBFTAddress(Blockchain.StandbyValidators).ToArray();
            var ret = Check_AddRoot(snapshot, new UInt160(from), validroot, persistingBlock);
            ret.Should().BeFalse();

            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // committee member,add a existing root
            ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);
            ret.Should().BeTrue();

            ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);
            ret.Should().BeFalse();

            //invalid root string
            ret = Check_AddRoot(snapshot, committeeAddress, "", persistingBlock);
            ret.Should().BeFalse();

            ret = Check_AddRoot(snapshot, committeeAddress, "\n", persistingBlock);
            ret.Should().BeFalse();

            ret = Check_AddRoot(snapshot, committeeAddress, ".", persistingBlock);
            ret.Should().BeFalse();

            //first character is not a-z
            ret = Check_AddRoot(snapshot, committeeAddress, "1a", persistingBlock);
            ret.Should().BeFalse();

            ret = Check_AddRoot(snapshot, committeeAddress, "A1", persistingBlock);
            ret.Should().BeFalse();

            ret = Check_AddRoot(snapshot, committeeAddress, "a1-2", persistingBlock);
            ret.Should().BeFalse();

            ret = Check_AddRoot(snapshot, committeeAddress, "a1234567891234567", persistingBlock);
            ret.Should().BeFalse();

        }

        [TestMethod]
        public void Test_SetAdmin()
        {
            var snapshot = _snapshot.CreateSnapshot();
            // Fake blockchain

            var persistingBlock = new Block() { Index = 1000 };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // committee member,add a new root and then register, setrecord
            string validroot = "testroot";
            var ret = Check_AddRoot(snapshot, committeeAddress, validroot, persistingBlock);

            string name = "testname";
            string domain = name + "." + validroot;

            //before register 
            var checkAvail_ret = Check_IsAvailable(snapshot, UInt160.Zero, domain, persistingBlock);
            checkAvail_ret.Result.Should().BeTrue();
            checkAvail_ret.State.Should().BeTrue();

            byte[] from = Contract.GetBFTAddress(Blockchain.StandbyValidators).ToArray();
            var register_ret = Check_Register(snapshot, domain, from, persistingBlock);
            register_ret.Result.Should().BeTrue();
            register_ret.State.Should().BeTrue();

            //check NFT token
            Assert.AreEqual(NativeContract.NameService.BalanceOf(snapshot, from), (BigInteger)1);

            //after register
            checkAvail_ret = Check_IsAvailable(snapshot, UInt160.Zero, domain, persistingBlock);
            checkAvail_ret.Result.Should().BeTrue();
            checkAvail_ret.State.Should().BeFalse();

            UInt160 admin = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash;
            var checkadmin_ret = Check_SetAdmin(snapshot, domain, admin, from, persistingBlock);
            checkadmin_ret.Should().BeTrue();

            //setrecord with admin account
            string testA = "10.10.10.10";
            var setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.A, testA, admin.ToArray(), persistingBlock);
            setRecord_ret.Should().BeTrue();

            var getRecord_ret = Check_GetRecord(snapshot, domain, RecordType.A, persistingBlock);
            getRecord_ret.State.Should().BeTrue();
            getRecord_ret.Result.Should().Equals(testA);

            //transfer the NFT token, admin set to null
            byte[] to = UInt160.Zero.ToArray();
            var transfer_ret = Check_Transfer(snapshot, UInt160.Zero, domain, new UInt160(from), persistingBlock);
            transfer_ret.Should().BeTrue();
            Assert.AreEqual(NativeContract.NameService.BalanceOf(snapshot, from), (BigInteger)0);

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.A, testA, admin.ToArray(), persistingBlock);
            setRecord_ret.Should().BeFalse();

            setRecord_ret = Check_SetRecord(snapshot, domain, RecordType.A, testA, from, persistingBlock);
            setRecord_ret.Should().BeFalse();

            checkadmin_ret = Check_SetAdmin(snapshot, domain, admin, from, persistingBlock);
            checkadmin_ret.Should().BeFalse();
        }

        internal static bool Check_AddRoot(DataCache snapshot, UInt160 account, string root, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(account), snapshot, persistingBlock);
            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            var script = new ScriptBuilder();
            script.EmitPush(root);
            script.EmitPush("addRoot");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            return true;
        }

        internal static bool Check_Transfer(DataCache snapshot, UInt160 to, string domain, UInt160 owner, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(owner), snapshot, persistingBlock);
            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            var script = new ScriptBuilder();
            script.EmitPush(domain);
            script.EmitPush(to);
            script.EmitPush("transfer");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            return true;
        }

        internal static (bool State, bool Result) Check_IsAvailable(DataCache snapshot, UInt160 account, string name, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(account), snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            var script = new ScriptBuilder();
            script.EmitPush(name);
            script.EmitPush("isAvailable");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (((VM.Types.Boolean)result).GetBoolean(), true);
        }

        internal static (bool State, bool Result) Check_SetPrice(DataCache snapshot, byte[] pubkey, long price, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            using var script = new ScriptBuilder();
            script.EmitPush(price);
            script.EmitPush("setPrice");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.GetBoolean());
        }

        internal static BigDecimal Check_GetPrice(DataCache snapshot, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            using var script = new ScriptBuilder();
            script.EmitPush("getPrice");
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(long));

            return new BigDecimal(((VM.Types.PrimitiveType)result).GetInteger(), NativeContract.GAS.Decimals);
        }

        internal static (bool State, bool Result) Check_Register(DataCache snapshot, string name, byte[] owner, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(new UInt160(owner)), snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            using var script = new ScriptBuilder();
            script.EmitPush(new UInt160(owner));
            script.EmitPush(name);
            script.EmitPush("register");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.GetBoolean());
        }

        internal static bool Check_SetRecord(DataCache snapshot, string name, RecordType type, string data, byte[] pubkey, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(new UInt160(pubkey)), snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            using var script = new ScriptBuilder();
            script.EmitPush(data);
            script.EmitPush(type);
            script.EmitPush(name);
            script.EmitPush("setRecord");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            return true;
        }

        internal static (bool State, string Result) Check_GetRecord(DataCache snapshot, string name, RecordType type, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            using var script = new ScriptBuilder();
            script.EmitPush(type);
            script.EmitPush(name);
            script.EmitPush("getRecord");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, null);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(Neo.VM.Types.ByteString));

            return (true, result.ToString());
        }

        internal static bool Check_SetAdmin(DataCache snapshot, string name, UInt160 admin, byte[] pubkey, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(new UInt160[] { admin, new UInt160(pubkey) }), snapshot, persistingBlock);

            engine.LoadScript(NativeContract.NameService.Script, configureState: p => p.ScriptHash = NativeContract.NameService.Hash);

            using var script = new ScriptBuilder();
            script.EmitPush(admin);
            script.EmitPush(name);
            script.EmitPush("setAdmin");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            return true;
        }
    }
}
