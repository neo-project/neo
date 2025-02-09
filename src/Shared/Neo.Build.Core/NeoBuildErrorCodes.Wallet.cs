// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildErrorCodes.Wallet.cs file belongs to the neo project and is free
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
        public sealed class Wallet
        {
            public const int ModuleId = 2000;
            private const int ModuleBaseErrorCode = BuildModuleBaseErrorCode * ModuleId;

            public const int InternalException = ModuleBaseErrorCode;
            public const int VersionException = ModuleBaseErrorCode + 1;
            public const int AccountNotFoundException = ModuleBaseErrorCode + 2;
            public const int AccountLockedException = ModuleBaseErrorCode + 3;
            public const int PrivateKeyNotFoundException = ModuleBaseErrorCode + 4;
            public const int PasswordException = ModuleBaseErrorCode + 5;

            private Wallet() { }
        }
    }
}
