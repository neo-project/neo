using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtReferenceCounter
    {
        [TestMethod]
        public void TestCircularReferences()
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.INITSSLOT, new byte[] { 1 }); //{}|{null}:1
            sb.EmitPush(0); //{0}|{null}:2
            sb.Emit(OpCode.NEWARRAY); //{A[]}|{null}:2
            sb.Emit(OpCode.DUP); //{A[],A[]}|{null}:3
            sb.Emit(OpCode.DUP); //{A[],A[],A[]}|{null}:4
            sb.Emit(OpCode.APPEND); //{A[A]}|{null}:3
            sb.Emit(OpCode.DUP); //{A[A],A[A]}|{null}:4
            sb.EmitPush(0); //{A[A],A[A],0}|{null}:5
            sb.Emit(OpCode.NEWARRAY); //{A[A],A[A],B[]}|{null}:5
            sb.Emit(OpCode.STSFLD0); //{A[A],A[A]}|{B[]}:4
            sb.Emit(OpCode.LDSFLD0); //{A[A],A[A],B[]}|{B[]}:5
            sb.Emit(OpCode.APPEND); //{A[A,B]}|{B[]}:4
            sb.Emit(OpCode.LDSFLD0); //{A[A,B],B[]}|{B[]}:5
            sb.EmitPush(0); //{A[A,B],B[],0}|{B[]}:6
            sb.Emit(OpCode.NEWARRAY); //{A[A,B],B[],C[]}|{B[]}:6
            sb.Emit(OpCode.TUCK); //{A[A,B],C[],B[],C[]}|{B[]}:7
            sb.Emit(OpCode.APPEND); //{A[A,B],C[]}|{B[C]}:6
            sb.EmitPush(0); //{A[A,B],C[],0}|{B[C]}:7
            sb.Emit(OpCode.NEWARRAY); //{A[A,B],C[],D[]}|{B[C]}:7
            sb.Emit(OpCode.TUCK); //{A[A,B],D[],C[],D[]}|{B[C]}:8
            sb.Emit(OpCode.APPEND); //{A[A,B],D[]}|{B[C[D]]}:7
            sb.Emit(OpCode.LDSFLD0); //{A[A,B],D[],B[C]}|{B[C[D]]}:8
            sb.Emit(OpCode.APPEND); //{A[A,B]}|{B[C[D[B]]]}:7
            sb.Emit(OpCode.PUSHNULL); //{A[A,B],null}|{B[C[D[B]]]}:8
            sb.Emit(OpCode.STSFLD0); //{A[A,B[C[D[B]]]]}|{null}:7
            sb.Emit(OpCode.DUP); //{A[A,B[C[D[B]]]],A[A,B]}|{null}:8
            sb.EmitPush(1); //{A[A,B[C[D[B]]]],A[A,B],1}|{null}:9
            sb.Emit(OpCode.REMOVE); //{A[A]}|{null}:3
            sb.Emit(OpCode.STSFLD0); //{}|{A[A]}:2
            sb.Emit(OpCode.RET); //{}:0

            using ExecutionEngine engine = new();
            Debugger debugger = new(engine);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(5, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(5, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(5, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(5, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(9, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, debugger.Execute());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }

        [TestMethod]
        public void TestRemoveReferrer()
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.INITSSLOT, new byte[] { 1 }); //{}|{null}:1
            sb.EmitPush(0); //{0}|{null}:2
            sb.Emit(OpCode.NEWARRAY); //{A[]}|{null}:2
            sb.Emit(OpCode.DUP); //{A[],A[]}|{null}:3
            sb.EmitPush(0); //{A[],A[],0}|{null}:4
            sb.Emit(OpCode.NEWARRAY); //{A[],A[],B[]}|{null}:4
            sb.Emit(OpCode.STSFLD0); //{A[],A[]}|{B[]}:3
            sb.Emit(OpCode.LDSFLD0); //{A[],A[],B[]}|{B[]}:4
            sb.Emit(OpCode.APPEND); //{A[B]}|{B[]}:3
            sb.Emit(OpCode.DROP); //{}|{B[]}:1
            sb.Emit(OpCode.RET); //{}:0

            using ExecutionEngine engine = new();
            Debugger debugger = new(engine);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, debugger.Execute());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }

        [TestMethod]
        public void TestArrayNoPush()
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.RET);
            using ExecutionEngine engine = new();
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            Array array = new(engine.ReferenceCounter, new StackItem[] { 1, 2, 3, 4 });
            Assert.AreEqual(array.Count, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }
}
