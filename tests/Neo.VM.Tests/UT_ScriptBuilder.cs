// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ScriptBuilder.cs file belongs to the neo project and is free
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
    public class UT_ScriptBuilder
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
        public void TestNullAndEmpty()
        {
            using (ScriptBuilder script = new())
            {
                ReadOnlySpan<byte> span = null;
                script.EmitPush(span);

                span = [];
                script.EmitPush(span);

                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, 0, (byte)OpCode.PUSHDATA1, 0 }, script.ToArray());
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
                    Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = script.EmitJump(op, offset_i8));
                    Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = script.EmitJump(op, offset_i32));
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
                    Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = script.EmitJump(op, offset_i8));
                    Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = script.EmitJump(op, offset_i32));
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
            // Test small integers (-1 to 16)
            for (var i = -1; i <= 16; i++)
            {
                using ScriptBuilder script = new();
                script.EmitPush(new BigInteger(i));
                CollectionAssert.AreEqual(new[] { (byte)(OpCode.PUSH0 + (byte)i) }, script.ToArray());
            }

            // Test -1
            Assert.AreEqual("0x0f", new ScriptBuilder().EmitPush(BigInteger.MinusOne).ToArray().ToHexString());

            // Test edge cases for different sizes
            // PUSHINT8
            Assert.AreEqual("0x0080", new ScriptBuilder().EmitPush(sbyte.MinValue).ToArray().ToHexString());
            Assert.AreEqual("0x007f", new ScriptBuilder().EmitPush(sbyte.MaxValue).ToArray().ToHexString());

            // PUSHINT16
            Assert.AreEqual("0x010080", new ScriptBuilder().EmitPush(short.MinValue).ToArray().ToHexString());
            Assert.AreEqual("0x01ff7f", new ScriptBuilder().EmitPush(short.MaxValue).ToArray().ToHexString());

            // PUSHINT32
            Assert.AreEqual("0x0200000080", new ScriptBuilder().EmitPush(int.MinValue).ToArray().ToHexString());
            Assert.AreEqual("0x02ffffff7f", new ScriptBuilder().EmitPush(int.MaxValue).ToArray().ToHexString());

            // PUSHINT64
            Assert.AreEqual("0x030000000000000080", new ScriptBuilder().EmitPush(long.MinValue).ToArray().ToHexString());
            Assert.AreEqual("0x03ffffffffffffff7f", new ScriptBuilder().EmitPush(long.MaxValue).ToArray().ToHexString());

            // PUSHINT128
            Assert.AreEqual("0x04ffffffffffffffff0000000000000000", new ScriptBuilder().EmitPush(new BigInteger(ulong.MaxValue)).ToArray().ToHexString());
            Assert.AreEqual("0x0400000000000000000100000000000000", new ScriptBuilder().EmitPush(new BigInteger(ulong.MaxValue) + 1).ToArray().ToHexString());

            // PUSHINT256, case from https://en.wikipedia.org/wiki/256-bit_computing#:~:text=The%20range%20of%20a%20signed,%2C%E2%80%8B819%2C%E2%80%8B967.
            Assert.AreEqual("0x050000000000000000000000000000000000000000000000000000000000000080",
                new ScriptBuilder().EmitPush(BigInteger.Parse("-57896044618658097711785492504343953926634992332820282019728792003956564819968")).ToArray().ToHexString());

            Assert.AreEqual("0x05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f",
                new ScriptBuilder().EmitPush(BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819967")).ToArray().ToHexString());

            // Test exceeding 256-bit value (2^256)
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new ScriptBuilder().EmitPush(BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639936")));

            // Test negative numbers
            Assert.AreEqual("0x00fe", new ScriptBuilder().EmitPush(new BigInteger(-2)).ToArray().ToHexString());
            Assert.AreEqual("0x0100ff", new ScriptBuilder().EmitPush(new BigInteger(-256)).ToArray().ToHexString());

            // Test numbers that are exactly at the boundary
            Assert.AreEqual("0x04ffffffffffffffff0000000000000000", new ScriptBuilder().EmitPush(BigInteger.Parse("18446744073709551615")).ToArray().ToHexString());
            Assert.AreEqual("0x0400000000000000000100000000000000", new ScriptBuilder().EmitPush(BigInteger.Parse("18446744073709551616")).ToArray().ToHexString());

            // Test very large negative number
            Assert.AreEqual("0x040000000000000000ffffffffffffffff", new ScriptBuilder().EmitPush(BigInteger.Parse("-18446744073709551616")).ToArray().ToHexString());

            // Test exception for too large BigInteger
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new ScriptBuilder().EmitPush(
                BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639937")));
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
                script.EmitPush((byte[])null);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, 0 }, script.ToArray());
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
                Assert.ThrowsExactly<ArgumentNullException>(() => _ = script.EmitPush((string)null));
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
