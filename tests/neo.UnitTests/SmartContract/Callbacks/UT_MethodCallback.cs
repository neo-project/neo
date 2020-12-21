using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Callbacks;
using Neo.SmartContract.Manifest;
using Neo.UnitTests.Extensions;
using System;

namespace Neo.UnitTests.SmartContract.Callbacks
{
    [TestClass]
    public class UT_MethodCallback
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void GetHashData()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);

            Assert.ThrowsException<ArgumentException>(() => new MethodCallback(engine, UInt160.Zero, "_test"));

            var contract = new ContractState()
            {
                Manifest = new ContractManifest()
                {
                    Permissions = new ContractPermission[0],
                    Groups = new ContractGroup[0],
                    Trusts = WildcardContainer<UInt160>.Create(),
                    Abi = new ContractAbi()
                    {
                        Methods = new ContractMethodDescriptor[]
                        {
                            new ContractMethodDescriptor(){ Name="test", Parameters=new ContractParameterDefinition[0]}
                        },
                        Events = new ContractEventDescriptor[0],
                    },
                },
                Script = new byte[] { 1, 2, 3 },
                Hash = new byte[] { 1, 2, 3 }.ToScriptHash()
            };
            engine.LoadScript(contract.Script);
            engine.Snapshot.AddContract(contract.Hash, contract);

            Assert.ThrowsException<InvalidOperationException>(() => new MethodCallback(engine, contract.Hash, "test"));

            contract.Manifest.Permissions = new ContractPermission[] {
                new ContractPermission() { Contract = ContractPermissionDescriptor.Create(contract.Hash),
                Methods= WildcardContainer<string>.Create("test") } };
            var data = new MethodCallback(engine, contract.Hash, "test");

            Assert.AreEqual(0, engine.CurrentContext.EvaluationStack.Count);
            var array = new VM.Types.Array();

            data.LoadContext(engine, array);

            Assert.AreEqual(4, engine.CurrentContext.EvaluationStack.Count);
            Assert.AreEqual("9bc4860bb936abf262d7a51f74b4304833fee3b2", engine.Pop<VM.Types.ByteString>().GetSpan().ToHexString());
            Assert.AreEqual("test", engine.Pop<VM.Types.ByteString>().GetString());
            Assert.IsTrue(engine.Pop() == array);
        }
    }
}
