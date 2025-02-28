// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildErrorCodes.VirtualMachine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Core
{
    public partial class NeoBuildErrorCodes
    {
        public sealed class VirtualMachine
        {
            public const int ModuleId = 3;
            private const int ModuleBaseErrorCode = BuildModuleBaseErrorCode * ModuleId;

            public const int InternalException = ModuleBaseErrorCode;

            public const int CaughtException = ModuleBaseErrorCode + 1;
            public const int BadScriptException = ModuleBaseErrorCode + 2;

            public const int InvalidOpCodeException = ModuleBaseErrorCode + 100;
            public const int InvalidOperandException = ModuleBaseErrorCode + 101;
            public const int InvalidParameterCountException = ModuleBaseErrorCode + 102;
            public const int InvalidParameterTypeException = ModuleBaseErrorCode + 103;
            public const int InvalidSystemCallException = ModuleBaseErrorCode + 104;
            public const int InvalidStateException = ModuleBaseErrorCode + 105;

            private VirtualMachine() { }
        }
    }
}
