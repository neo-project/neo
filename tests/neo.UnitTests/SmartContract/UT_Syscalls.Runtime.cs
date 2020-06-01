using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_Syscalls
    {
        [TestMethod]
        public void Runtime_CreateCallback()
        {
            using var script = new ScriptBuilder();

            script.EmitPush(5);
            script.EmitPush(2);
            script.EmitPush(1);
            script.Emit(OpCode.PUSHA, BitConverter.GetBytes(0));
            script.EmitSysCall(ApplicationEngine.CreateCallback);

            // Execute

            var engine = new ApplicationEngine(TriggerType.Application, null, null, 100_000_000, false);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(engine.Execute(), VMState.HALT);

            // Check the results

            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.IsTrue(engine.ResultStack.TryPop<InteropInterface>(out var item));
            Assert.IsNotNull(item);

            Assert.IsTrue(item.TryGetInterface<Callback>(out var callback));
            Assert.AreEqual(2, callback.RVcount);
            Assert.AreEqual(1, callback.Params.Length);
            Assert.AreEqual(5, callback.Params[0].GetBigInteger());
        }
    }
}
