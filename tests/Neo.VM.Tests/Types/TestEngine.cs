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
    class TestEngine : ExecutionEngine
    {
        public Exception FaultException { get; private set; }

        protected override void OnSysCall(uint method)
        {
            if (method == 0x77777777)
            {
                CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new object()));
                return;
            }

            if (method == 0xaddeadde)
            {
                ExecuteThrow("error");
                return;
            }

            throw new System.Exception();
        }

        protected override void OnFault(Exception ex)
        {
            FaultException = ex;
            base.OnFault(ex);
        }
    }
}
