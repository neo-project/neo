using FluentAssertions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.IO;
using System.Numerics;

namespace Neo.UnitTests.Extensions
{
    public static class Nep17NativeContractExtensions
    {
        internal class ManualWitness : IVerifiable
        {
            private readonly UInt160[] _hashForVerify;

            public int Size => 0;

            public Witness[] Witnesses { get; set; }

            public ManualWitness(params UInt160[] hashForVerify)
            {
                _hashForVerify = hashForVerify ?? System.Array.Empty<UInt160>();
            }

            public void Deserialize(BinaryReader reader) { }

            public void DeserializeUnsigned(BinaryReader reader) { }

            public UInt160[] GetScriptHashesForVerifying(DataCache snapshot) => _hashForVerify;

            public void Serialize(BinaryWriter writer) { }

            public void SerializeUnsigned(BinaryWriter writer) { }
        }

        public static bool Transfer(this NativeContract contract, DataCache snapshot, byte[] from, byte[] to, BigInteger amount, bool signFrom, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new ManualWitness(signFrom ? new UInt160(from) : null), snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contract.Hash, "transfer", from, to, amount, null);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                throw engine.FaultException;
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return result.GetBoolean();
        }

        public static BigInteger TotalSupply(this NativeContract contract, DataCache snapshot)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contract.Hash, "totalSupply");
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return result.GetInteger();
        }

        public static BigInteger BalanceOf(this NativeContract contract, DataCache snapshot, byte[] account)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contract.Hash, "balanceOf", account);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return result.GetInteger();
        }

        public static BigInteger Decimals(this NativeContract contract, DataCache snapshot)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contract.Hash, "decimals");
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return result.GetInteger();
        }

        public static string Symbol(this NativeContract contract, DataCache snapshot)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contract.Hash, "symbol");
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteString));

            return result.GetString();
        }
    }
}
