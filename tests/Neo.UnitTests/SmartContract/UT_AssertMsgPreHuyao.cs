// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AssertMsgPreHuyao.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_AssertMsgPreHuyao
    {
        private static readonly ProtocolSettings Settings = TestProtocolSettings.Default with
        {
            Hardforks = TestProtocolSettings.Default.Hardforks
                .Where(p => p.Key < Hardfork.HF_Huyao).ToImmutableDictionary()
        };

        private static ApplicationEngine Run(byte[] script)
        {
            using var system = new TestBlockchain.TestNeoSystem(Settings);
            using var snapshot = system.GetSnapshotCache();
            return ApplicationEngine.Run(script, snapshot, settings: Settings, gas: 1_00000000);
        }

        [TestMethod]
        public void PassingAssertMsgWithNullMessageContinues()
        {
            using var engine = Run([(byte)OpCode.PUSHT, (byte)OpCode.PUSHNULL,
                (byte)OpCode.ASSERTMSG, (byte)OpCode.PUSHINT8, 42, (byte)OpCode.RET]);

            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            Assert.AreEqual(42, (int)engine.ResultStack.Peek().GetInteger());
        }

        [TestMethod]
        public void PassingAssertMsgWithNullMessageInsideTryContinues()
        {
            using var engine = Run([
                (byte)OpCode.TRY, 8, 0,
                (byte)OpCode.PUSHT, (byte)OpCode.PUSHNULL, (byte)OpCode.ASSERTMSG,
                (byte)OpCode.ENDTRY, 6,
                (byte)OpCode.DROP, (byte)OpCode.PUSHF, (byte)OpCode.ENDTRY, 2,
                (byte)OpCode.PUSHT, (byte)OpCode.RET]);

            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            Assert.IsTrue(engine.ResultStack.Peek().GetBoolean());
        }

        [TestMethod]
        public void FailingAssertMsgWithNullMessageFaults()
        {
            using var engine = Run([(byte)OpCode.PUSHF, (byte)OpCode.PUSHNULL,
                (byte)OpCode.ASSERTMSG, (byte)OpCode.RET]);

            Assert.AreEqual(VMState.FAULT, engine.State);
        }
    }
}
