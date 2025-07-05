// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildErrorCodes.Contracts.cs file belongs to the neo project and is free
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
        public sealed class Contracts
        {
            public const int ModuleId = 3;
            private const int ModuleBaseErrorCode = BuildModuleBaseErrorCode * ModuleId;

            public const int InternalException = ModuleBaseErrorCode;

            public const int ContractNotFound = ModuleBaseErrorCode + 1;
            public const int MissingScriptHashAttribute = ModuleBaseErrorCode + 2;

            private Contracts() { }
        }
    }
}
