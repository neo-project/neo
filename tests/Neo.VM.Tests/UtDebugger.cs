// Copyright (C) 2015-2024 The Neo Project.
//
// UtDebugger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtDebugger
    {
        [TestMethod]
        public void TestBreakPoint()
        {
            using ExecutionEngine engine = new();
            using ScriptBuilder script = new();
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);

            engine.LoadScript(script.ToArray());

            Debugger debugger = new(engine);

            Assert.IsFalse(debugger.RemoveBreakPoint(engine.CurrentContext.Script, 3));

            Assert.AreEqual(OpCode.NOP, engine.CurrentContext.NextInstruction.OpCode);

            debugger.AddBreakPoint(engine.CurrentContext.Script, 2);
            debugger.AddBreakPoint(engine.CurrentContext.Script, 3);
            debugger.Execute();
            Assert.AreEqual(OpCode.NOP, engine.CurrentContext.NextInstruction.OpCode);
            Assert.AreEqual(2, engine.CurrentContext.InstructionPointer);
            Assert.AreEqual(VMState.BREAK, engine.State);

            Assert.IsTrue(debugger.RemoveBreakPoint(engine.CurrentContext.Script, 2));
            Assert.IsFalse(debugger.RemoveBreakPoint(engine.CurrentContext.Script, 2));
            Assert.IsTrue(debugger.RemoveBreakPoint(engine.CurrentContext.Script, 3));
            Assert.IsFalse(debugger.RemoveBreakPoint(engine.CurrentContext.Script, 3));
            debugger.Execute();
            Assert.AreEqual(VMState.HALT, engine.State);
        }

        [TestMethod]
        public void TestWithoutBreakPoints()
        {
            using ExecutionEngine engine = new();
            using ScriptBuilder script = new();
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);

            engine.LoadScript(script.ToArray());

            Debugger debugger = new(engine);

            Assert.AreEqual(OpCode.NOP, engine.CurrentContext.NextInstruction.OpCode);

            debugger.Execute();

            Assert.IsNull(engine.CurrentContext);
            Assert.AreEqual(VMState.HALT, engine.State);
        }

        [TestMethod]
        public void TestWithoutDebugger()
        {
            using ExecutionEngine engine = new();
            using ScriptBuilder script = new();
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);
            script.Emit(OpCode.NOP);

            engine.LoadScript(script.ToArray());

            Assert.AreEqual(OpCode.NOP, engine.CurrentContext.NextInstruction.OpCode);

            engine.Execute();

            Assert.IsNull(engine.CurrentContext);
            Assert.AreEqual(VMState.HALT, engine.State);
        }

        [TestMethod]
        public void TestStepOver()
        {
            using ExecutionEngine engine = new();
            using ScriptBuilder script = new();
            /* ┌     CALL 
               │  ┌> NOT
               │  │  RET
               └> │  PUSH0  
                └─┘  RET */
            script.EmitCall(4);
            script.Emit(OpCode.NOT);
            script.Emit(OpCode.RET);
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.RET);

            engine.LoadScript(script.ToArray());

            Debugger debugger = new(engine);

            Assert.AreEqual(OpCode.NOT, engine.CurrentContext.NextInstruction.OpCode);
            Assert.AreEqual(VMState.BREAK, debugger.StepOver());
            Assert.AreEqual(2, engine.CurrentContext.InstructionPointer);
            Assert.AreEqual(VMState.BREAK, engine.State);
            Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);

            debugger.Execute();

            Assert.AreEqual(true, engine.ResultStack.Pop().GetBoolean());
            Assert.AreEqual(VMState.HALT, engine.State);

            // Test step over again

            Assert.AreEqual(VMState.HALT, debugger.StepOver());
            Assert.AreEqual(VMState.HALT, engine.State);
        }

        [TestMethod]
        public void TestStepInto()
        {
            using ExecutionEngine engine = new();
            using ScriptBuilder script = new();
            /* ┌     CALL
               │  ┌> NOT 
               │  │  RET
               └> │  PUSH0
                └─┘  RET */
            script.EmitCall(4);
            script.Emit(OpCode.NOT);
            script.Emit(OpCode.RET);
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.RET);

            engine.LoadScript(script.ToArray());

            Debugger debugger = new(engine);

            var context = engine.CurrentContext;

            Assert.AreEqual(context, engine.CurrentContext);
            Assert.AreEqual(context, engine.EntryContext);
            Assert.AreEqual(OpCode.NOT, engine.CurrentContext.NextInstruction.OpCode);

            Assert.AreEqual(VMState.BREAK, debugger.StepInto());

            Assert.AreNotEqual(context, engine.CurrentContext);
            Assert.AreEqual(context, engine.EntryContext);
            Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);

            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());

            Assert.AreEqual(context, engine.CurrentContext);
            Assert.AreEqual(context, engine.EntryContext);
            Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);

            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(VMState.HALT, debugger.StepInto());

            Assert.AreEqual(true, engine.ResultStack.Pop().GetBoolean());
            Assert.AreEqual(VMState.HALT, engine.State);

            // Test step into again

            Assert.AreEqual(VMState.HALT, debugger.StepInto());
            Assert.AreEqual(VMState.HALT, engine.State);
        }

        [TestMethod]
        public void TestBreakPointStepOver()
        {
            using ExecutionEngine engine = new();
            using ScriptBuilder script = new();
            /* ┌     CALL 
               │  ┌> NOT
               │  │  RET
               └>X│  PUSH0
                 └┘  RET */
            script.EmitCall(4);
            script.Emit(OpCode.NOT);
            script.Emit(OpCode.RET);
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.RET);

            engine.LoadScript(script.ToArray());

            Debugger debugger = new(engine);

            Assert.AreEqual(OpCode.NOT, engine.CurrentContext.NextInstruction.OpCode);

            debugger.AddBreakPoint(engine.CurrentContext.Script, 5);
            Assert.AreEqual(VMState.BREAK, debugger.StepOver());

            Assert.IsNull(engine.CurrentContext.NextInstruction);
            Assert.AreEqual(5, engine.CurrentContext.InstructionPointer);
            Assert.AreEqual(VMState.BREAK, engine.State);

            debugger.Execute();

            Assert.AreEqual(true, engine.ResultStack.Pop().GetBoolean());
            Assert.AreEqual(VMState.HALT, engine.State);
        }
    }
}
