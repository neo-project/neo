// Copyright (C) 2015-2025 The Neo Project.
//
// DebugApplicationEngine.Common.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;

namespace Neo.Build.Core.SmartContract
{
    public partial class DebugApplicationEngine
    {
        private void CheckBreakPointsAndBreak()
        {
            if (State == VMState.NONE && InvocationStack.Count > 0 && BreakPoints.Count > 0)
            {
                if (CurrentContext is null) return;
                if (_breakPoints.TryGetValue(CurrentContext.Script, out var positionTable) &&
                    positionTable.Contains((uint)CurrentContext.InstructionPointer))
                    State = VMState.BREAK;
            }
        }
    }
}
