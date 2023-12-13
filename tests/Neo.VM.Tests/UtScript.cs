using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtScript
    {
        [TestMethod]
        public void Conversion()
        {
            byte[] rawScript;
            using (var builder = new ScriptBuilder())
            {
                builder.Emit(OpCode.PUSH0);
                builder.Emit(OpCode.CALL, new byte[] { 0x00, 0x01 });
                builder.EmitSysCall(123);

                rawScript = builder.ToArray();
            }

            var script = new Script(rawScript);

            ReadOnlyMemory<byte> scriptConversion = script;
            Assert.AreEqual(rawScript, scriptConversion);
        }

        [TestMethod]
        public void StrictMode()
        {
            var rawScript = new byte[] { (byte)OpCode.PUSH0, 0xFF };
            Assert.ThrowsException<BadScriptException>(() => new Script(rawScript, true));

            var script = new Script(rawScript, false);
            Assert.AreEqual(2, script.Length);

            rawScript = new byte[] { (byte)OpCode.PUSHDATA1 };
            Assert.ThrowsException<BadScriptException>(() => new Script(rawScript, true));

            rawScript = new byte[] { (byte)OpCode.PUSHDATA2 };
            Assert.ThrowsException<BadScriptException>(() => new Script(rawScript, true));

            rawScript = new byte[] { (byte)OpCode.PUSHDATA4 };
            Assert.ThrowsException<BadScriptException>(() => new Script(rawScript, true));
        }

        [TestMethod]
        public void Parse()
        {
            Script script;

            using (var builder = new ScriptBuilder())
            {
                builder.Emit(OpCode.PUSH0);
                builder.Emit(OpCode.CALL_L, new byte[] { 0x00, 0x01, 0x00, 0x00 });
                builder.EmitSysCall(123);

                script = new Script(builder.ToArray());
            }

            Assert.AreEqual(11, script.Length);

            var ins = script.GetInstruction(0);

            Assert.AreEqual(OpCode.PUSH0, ins.OpCode);
            Assert.IsTrue(ins.Operand.IsEmpty);
            Assert.AreEqual(1, ins.Size);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var x = ins.TokenI16; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var x = ins.TokenU32; });

            ins = script.GetInstruction(1);

            Assert.AreEqual(OpCode.CALL_L, ins.OpCode);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x00, 0x00 }, ins.Operand.ToArray());
            Assert.AreEqual(5, ins.Size);
            Assert.AreEqual(256, ins.TokenI32);
            Assert.AreEqual(Encoding.ASCII.GetString(new byte[] { 0x00, 0x01, 0x00, 0x00 }), ins.TokenString);

            ins = script.GetInstruction(6);

            Assert.AreEqual(OpCode.SYSCALL, ins.OpCode);
            CollectionAssert.AreEqual(new byte[] { 123, 0x00, 0x00, 0x00 }, ins.Operand.ToArray());
            Assert.AreEqual(5, ins.Size);
            Assert.AreEqual(123, ins.TokenI16);
            Assert.AreEqual(Encoding.ASCII.GetString(new byte[] { 123, 0x00, 0x00, 0x00 }), ins.TokenString);
            Assert.AreEqual(123U, ins.TokenU32);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => script.GetInstruction(100));
        }
    }
}
