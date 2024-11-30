// Copyright (C) 2015-2024 The Neo Project.
//
// TestEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.Test.Types
{
    public class TestEngine : ExecutionEngine
    {
        public Exception FaultException { get; private set; }

        public TestEngine() : base(new ReferenceCounterV2(), ComposeJumpTable()) { }

        private static JumpTable ComposeJumpTable()
        {
            JumpTable jumpTable = new JumpTable();
            jumpTable[OpCode.SYSCALL] = OnSysCall;
            return jumpTable;
        }

        private static void OnSysCall(ExecutionEngine engine, Instruction instruction)
        {
            uint method = instruction.TokenU32;

            if (method == 0x77777777)
            {
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new object()));
                return;
            }

            if (method == 0xaddeadde)
            {
                engine.JumpTable.ExecuteThrow(engine, "error");
                return;
            }

            throw new Exception();
        }

        protected override void OnFault(Exception ex)
        {
            FaultException = ex;
            base.OnFault(ex);
        }
    }
}
