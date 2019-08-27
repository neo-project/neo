using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.UnitTests.Extensions
{
    public static class NativeContractExtensions
    {
        public static StackItem Call(this NativeContract contract, Neo.Persistence.Snapshot snapshot, string method, params ContractParameter[] args)
        {
            return Call(contract, snapshot, null, method, args);
        }

        public static StackItem Call(this NativeContract contract, Neo.Persistence.Snapshot snapshot, IVerifiable container, string method, params ContractParameter[] args)
        {
            var engine = new ApplicationEngine(TriggerType.Application, container, snapshot, 0, true);

            engine.LoadScript(contract.Script);

            var script = new ScriptBuilder();

            for (var i = args.Length - 1; i >= 0; i--)
                script.EmitPush(args[i]);

            script.EmitPush(args.Length);
            script.Emit(OpCode.PACK);
            script.EmitPush(method);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() != VMState.HALT)
            {
                throw new InvalidOperationException();
            }

            return engine.ResultStack.Pop();
        }
    }
}
