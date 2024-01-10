// Copyright (C) 2015-2024 The Neo Project.
//
// ExecutionContext.SharedStates.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.VM
{
    partial class ExecutionContext
    {
        private class SharedStates
        {
            public readonly Script Script;
            public readonly EvaluationStack EvaluationStack;
            public Slot? StaticFields;
            public readonly Dictionary<Type, object> States;

            public SharedStates(Script script, ReferenceCounter referenceCounter)
            {
                this.Script = script;
                this.EvaluationStack = new EvaluationStack(referenceCounter);
                this.States = new Dictionary<Type, object>();
            }
        }
    }
}
