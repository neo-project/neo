// Copyright (C) 2015-2024 The Neo Project.
//
// VMUTExecutionContextState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Test.Converters;
using Neo.VM;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTExecutionContextState
    {
        [JsonProperty]
        public int InstructionPointer { get; set; }

        [JsonProperty, JsonConverter(typeof(UppercaseEnum))]
        public OpCode NextInstruction { get; set; }

        // Stacks

        [JsonProperty]
        public VMUTStackItem[] EvaluationStack { get; set; }

        // Slots

        [JsonProperty]
        public VMUTStackItem[] StaticFields { get; set; }

        [JsonProperty]
        public VMUTStackItem[] Arguments { get; set; }

        [JsonProperty]
        public VMUTStackItem[] LocalVariables { get; set; }
    }
}
