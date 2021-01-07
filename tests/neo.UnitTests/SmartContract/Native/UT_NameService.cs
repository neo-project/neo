using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NameService : TestKit
    {
        protected StoreView _snapshot;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            _snapshot = Blockchain.Singleton.GetSnapshot();
        }

        [TestMethod]
        public void TestInfo()
        {
            Assert.AreEqual("NameService", NativeContract.NameService.Name);
            Assert.AreEqual("NNS", NativeContract.NameService.Symbol);
        }

        [TestMethod]
        public void TestRoots()
        {
            var snapshot = _snapshot.Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            var from = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // no match
            var result = Check_AddRoot(snapshot, from, "te_st");
            Assert.IsFalse(result);

            // unsigned
            result = Check_AddRoot(snapshot, UInt160.Zero, "test");
            Assert.IsFalse(result);

            // add root
            result = Check_AddRoot(snapshot, from, "test");
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new string[] { "test" }, NativeContract.NameService.GetRoots(snapshot).ToArray());

            // add root twice
            result = Check_AddRoot(snapshot, from, "test");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestPrice()
        {
            var snapshot = _snapshot.Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            var from = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // unsigned
            var result = Check_SetPrice(snapshot, UInt160.Zero, 1);
            Assert.IsFalse(result);

            // under value
            result = Check_SetPrice(snapshot, from, 0);
            Assert.IsFalse(result);

            // overvalue
            result = Check_SetPrice(snapshot, from, 10000_00000001);
            Assert.IsFalse(result);

            // good
            result = Check_SetPrice(snapshot, from, 55);
            Assert.IsTrue(result);
            Assert.AreEqual(55, NativeContract.NameService.GetPrice(snapshot));
        }

        [TestMethod]
        public void TestRegister()
        {
            var snapshot = _snapshot.Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            var from = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // add root
            var result = Check_AddRoot(snapshot, from, "com");
            Assert.IsTrue(result);

            // no-roots
            result = Check_Register(snapshot, "neo.org", UInt160.Zero);
            Assert.IsFalse(result);

            // more than 2 dots
            result = Check_Register(snapshot, "doc.neo.org", UInt160.Zero);
            Assert.IsFalse(result);

            // regex
            result = Check_Register(snapshot, "neo.org\n", UInt160.Zero);
            Assert.IsFalse(result);

            // good register
            result = Check_IsAvailable(snapshot, "neo.com");
            Assert.IsTrue(result);
            result = Check_Register(snapshot, "neo.com", UInt160.Zero);
            Assert.IsTrue(result);
            result = Check_IsAvailable(snapshot, "neo.com");
            Assert.IsFalse(result);
        }

        internal static bool Check_IsAvailable(StoreView snapshot, string name)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);

            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, "isAvailable", true, new ContractParameter[] {
                new ContractParameter(ContractParameterType.String) { Value = name }
            });
            engine.LoadScript(script.ToArray(), 0, -1, 0);

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(VM.Types.Boolean));

            return result.GetBoolean();
        }

        internal static bool Check_Register(StoreView snapshot, string name, UInt160 owner)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(owner), snapshot);

            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, "register", true, new ContractParameter[] {
                new ContractParameter(ContractParameterType.String) { Value = name },
                new ContractParameter(ContractParameterType.Hash160) { Value = owner }
            });
            engine.LoadScript(script.ToArray(), 0, -1, 0);

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(VM.Types.Boolean));

            return result.GetBoolean();
        }

        internal static bool Check_SetPrice(StoreView snapshot, UInt160 signedBy, long price)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(signedBy), snapshot);

            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, "setPrice", false, new ContractParameter[] { new ContractParameter(ContractParameterType.Integer) { Value = price } });
            engine.LoadScript(script.ToArray(), 0, -1, 0);

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            return true;
        }

        internal static bool Check_AddRoot(StoreView snapshot, UInt160 signedBy, string root)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(signedBy), snapshot);

            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, "addRoot", false, new ContractParameter[] { new ContractParameter(ContractParameterType.String) { Value = root } });
            engine.LoadScript(script.ToArray(), 0, -1, 0);

            if (engine.Execute() == VMState.FAULT)
            {
                return false;
            }

            return true;
        }
    }
}
