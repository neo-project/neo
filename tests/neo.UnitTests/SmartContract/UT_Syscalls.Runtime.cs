using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_Syscalls
    {
        [TestMethod]
        public void Runtime_CreateCallback()
        {
            using var script = new ScriptBuilder();

            script.EmitPush(5); // Callback argument
            script.EmitPush(0); // RVcount
            script.EmitPush(1); // ParamCount
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
            Assert.AreEqual(0, callback.RVcount);
            Assert.AreEqual(1, callback.Params.Length);
            Assert.AreEqual(5, callback.Params[0].GetBigInteger());
        }

        [TestMethod]
        public void Runtime_InvokeCallback()
        {
            using var script = new ScriptBuilder();

            script.EmitPush(5); // Callback argument 1
            script.EmitPush(1); // Callback argument 2
            script.EmitPush(0); // RVcount
            script.EmitPush(2); // ParamCount
            script.Emit(OpCode.PUSHA, BitConverter.GetBytes(200)); // -> Nop area
            script.EmitSysCall(ApplicationEngine.CreateCallback);
            script.EmitSysCall(ApplicationEngine.InvokeCallback);
            script.Emit(OpCode.RET);

            for (int x = 0; x < 250; x++) script.Emit(OpCode.NOP);

            script.Emit(OpCode.ADD); // Should return 6
            script.Emit(OpCode.RET);

            // Execute

            var engine = new ApplicationEngine(TriggerType.Application, null, null, 100_000_000, false);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(engine.Execute(), VMState.HALT);

            // Check the results

            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.IsTrue(engine.ResultStack.TryPop<PrimitiveType>(out var item));
            Assert.IsNotNull(item);

            Assert.AreEqual(6, item.GetBigInteger());
        }
    }
}
