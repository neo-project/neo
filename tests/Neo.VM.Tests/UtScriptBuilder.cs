// Copyright (C) 2015-2024 The Neo Project.
//
// UtScriptBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Helpers;
using Neo.VM;
using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.Test
{
    [TestClass]
    public class UtScriptBuilder
    {
        [TestMethod]
        public void TestEmit()
        {
            using (ScriptBuilder script = new())
            {
                Assert.AreEqual(0, script.Length);
                script.Emit(OpCode.NOP);
                Assert.AreEqual(1, script.Length);

                CollectionAssert.AreEqual(new byte[] { 0x21 }, script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                script.Emit(OpCode.NOP, new byte[] { 0x66 });
                CollectionAssert.AreEqual(new byte[] { 0x21, 0x66 }, script.ToArray());
            }
        }

        [TestMethod]
        public void TestBigInteger()
        {
            using (ScriptBuilder script = new())
            {
                Assert.AreEqual(0, script.Length);
                script.EmitPush(-100000);
                Assert.AreEqual(5, script.Length);

                CollectionAssert.AreEqual(new byte[] { 2, 96, 121, 254, 255 }, script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                Assert.AreEqual(0, script.Length);
                script.EmitPush(100000);
                Assert.AreEqual(5, script.Length);

                CollectionAssert.AreEqual(new byte[] { 2, 160, 134, 1, 0 }, script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitSysCall()
        {
            using ScriptBuilder script = new();
            script.EmitSysCall(0xE393C875);
            CollectionAssert.AreEqual(new byte[] { (byte)OpCode.SYSCALL, 0x75, 0xC8, 0x93, 0xE3 }.ToArray(), script.ToArray());
        }

        [TestMethod]
        public void TestEmitCall()
        {
            using (ScriptBuilder script = new())
            {
                script.EmitCall(0);
                CollectionAssert.AreEqual(new[] { (byte)OpCode.CALL, (byte)0 }, script.ToArray());
            }
            using (ScriptBuilder script = new())
            {
                script.EmitCall(12345);
                CollectionAssert.AreEqual(new[] { (byte)OpCode.CALL_L }.Concat(BitConverter.GetBytes(12345)).ToArray(), script.ToArray());
            }
            using (ScriptBuilder script = new())
            {
                script.EmitCall(-12345);
                CollectionAssert.AreEqual(new[] { (byte)OpCode.CALL_L }.Concat(BitConverter.GetBytes(-12345)).ToArray(), script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitJump()
        {
            var offset_i8 = sbyte.MaxValue;
            var offset_i32 = int.MaxValue;

            foreach (OpCode op in Enum.GetValues(typeof(OpCode)))
            {
                using ScriptBuilder script = new();
                if (op < OpCode.JMP || op > OpCode.JMPLE_L)
                {
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => script.EmitJump(op, offset_i8));
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => script.EmitJump(op, offset_i32));
                }
                else
                {
                    script.EmitJump(op, offset_i8);
                    script.EmitJump(op, offset_i32);
                    if ((int)op % 2 == 0)
                        CollectionAssert.AreEqual(new[] { (byte)op, (byte)offset_i8, (byte)(op + 1) }.Concat(BitConverter.GetBytes(offset_i32)).ToArray(), script.ToArray());
                    else
                        CollectionAssert.AreEqual(new[] { (byte)op }.Concat(BitConverter.GetBytes((int)offset_i8)).Concat(new[] { (byte)op }).Concat(BitConverter.GetBytes(offset_i32)).ToArray(), script.ToArray());
                }
            }

            offset_i8 = sbyte.MinValue;
            offset_i32 = int.MinValue;

            foreach (OpCode op in Enum.GetValues(typeof(OpCode)))
            {
                using ScriptBuilder script = new();
                if (op < OpCode.JMP || op > OpCode.JMPLE_L)
                {
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => script.EmitJump(op, offset_i8));
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => script.EmitJump(op, offset_i32));
                }
                else
                {
                    script.EmitJump(op, offset_i8);
                    script.EmitJump(op, offset_i32);
                    if ((int)op % 2 == 0)
                        CollectionAssert.AreEqual(new[] { (byte)op, (byte)offset_i8, (byte)(op + 1) }.Concat(BitConverter.GetBytes(offset_i32)).ToArray(), script.ToArray());
                    else
                        CollectionAssert.AreEqual(new[] { (byte)op }.Concat(BitConverter.GetBytes((int)offset_i8)).Concat(new[] { (byte)op }).Concat(BitConverter.GetBytes(offset_i32)).ToArray(), script.ToArray());
                }
            }
        }

        [TestMethod]
        public void TestEmitPushBigInteger()
        {
            using (ScriptBuilder script = new())
            {
                script.EmitPush(BigInteger.MinusOne);
                CollectionAssert.AreEqual(new byte[] { 0x0F }, script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                script.EmitPush(BigInteger.Zero);
                CollectionAssert.AreEqual(new byte[] { 0x10 }, script.ToArray());
            }

            for (byte x = 1; x <= 16; x++)
            {
                using ScriptBuilder script = new();
                script.EmitPush(new BigInteger(x));
                CollectionAssert.AreEqual(new byte[] { (byte)(OpCode.PUSH0 + x) }, script.ToArray());
            }

            CollectionAssert.AreEqual("0080".FromHexString(), new ScriptBuilder().EmitPush(sbyte.MinValue).ToArray());
            CollectionAssert.AreEqual("007f".FromHexString(), new ScriptBuilder().EmitPush(sbyte.MaxValue).ToArray());
            CollectionAssert.AreEqual("01ff00".FromHexString(), new ScriptBuilder().EmitPush(byte.MaxValue).ToArray());
            CollectionAssert.AreEqual("010080".FromHexString(), new ScriptBuilder().EmitPush(short.MinValue).ToArray());
            CollectionAssert.AreEqual("01ff7f".FromHexString(), new ScriptBuilder().EmitPush(short.MaxValue).ToArray());
            CollectionAssert.AreEqual("02ffff0000".FromHexString(), new ScriptBuilder().EmitPush(ushort.MaxValue).ToArray());
            CollectionAssert.AreEqual("0200000080".FromHexString(), new ScriptBuilder().EmitPush(int.MinValue).ToArray());
            CollectionAssert.AreEqual("02ffffff7f".FromHexString(), new ScriptBuilder().EmitPush(int.MaxValue).ToArray());
            CollectionAssert.AreEqual("03ffffffff00000000".FromHexString(), new ScriptBuilder().EmitPush(uint.MaxValue).ToArray());
            CollectionAssert.AreEqual("030000000000000080".FromHexString(), new ScriptBuilder().EmitPush(long.MinValue).ToArray());
            CollectionAssert.AreEqual("03ffffffffffffff7f".FromHexString(), new ScriptBuilder().EmitPush(long.MaxValue).ToArray());
            CollectionAssert.AreEqual("04ffffffffffffffff0000000000000000".FromHexString(), new ScriptBuilder().EmitPush(ulong.MaxValue).ToArray());
            CollectionAssert.AreEqual("050100000000000000feffffffffffffff00000000000000000000000000000000".FromHexString(), new ScriptBuilder().EmitPush(new BigInteger(ulong.MaxValue) * new BigInteger(ulong.MaxValue)).ToArray());

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ScriptBuilder().EmitPush(
                new BigInteger("050100000000000000feffffffffffffff0100000000000000feffffffffffffff00000000000000000000000000000000".FromHexString())));
        }

        [TestMethod]
        public void TestEmitPushBool()
        {
            using (ScriptBuilder script = new())
            {
                script.EmitPush(true);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHT }, script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                script.EmitPush(false);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHF }, script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitPushReadOnlySpan()
        {
            using ScriptBuilder script = new();
            var data = new byte[] { 0x01, 0x02 };
            script.EmitPush(new ReadOnlySpan<byte>(data));

            CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, (byte)data.Length }.Concat(data).ToArray(), script.ToArray());
        }

        [TestMethod]
        public void TestEmitPushByteArray()
        {
            using (ScriptBuilder script = new())
            {
                Assert.ThrowsException<ArgumentNullException>(() => script.EmitPush((byte[])null));
            }

            using (ScriptBuilder script = new())
            {
                var data = RandomHelper.RandBuffer(0x4C);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, (byte)data.Length }.Concat(data).ToArray(), script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                var data = RandomHelper.RandBuffer(0x100);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA2 }.Concat(BitConverter.GetBytes((short)data.Length)).Concat(data).ToArray(), script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                var data = RandomHelper.RandBuffer(0x10000);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA4 }.Concat(BitConverter.GetBytes(data.Length)).Concat(data).ToArray(), script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitPushString()
        {
            using (ScriptBuilder script = new())
            {
                Assert.ThrowsException<ArgumentNullException>(() => script.EmitPush((string)null));
            }

            using (ScriptBuilder script = new())
            {
                var data = RandomHelper.RandString(0x4C);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, (byte)data.Length }.Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                var data = RandomHelper.RandString(0x100);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA2 }.Concat(BitConverter.GetBytes((short)data.Length)).Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
            }

            using (ScriptBuilder script = new())
            {
                var data = RandomHelper.RandString(0x10000);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA4 }.Concat(BitConverter.GetBytes(data.Length)).Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
            }
        }
    }
}
