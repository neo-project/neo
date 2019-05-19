using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.UnitTests.Extensions
{
    public static class NativeContractExtensions
    {
        public static StackItem Call(this NativeContract contract, Persistence.Snapshot snapshot, string method)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(contract.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
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