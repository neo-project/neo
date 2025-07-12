// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildInvalidWalletVersionException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Build.Core.Exceptions.Wallet
{
    public class NeoBuildWalletInvalidVersionException(
        Version version) : NeoBuildException($"Wrong wallet version '{version.ToString(2)}'.", NeoBuildErrorCodes.Wallet.InvalidVersion)
    {
        public NeoBuildWalletInvalidVersionException(string version) : this(new Version(version)) { }

        public Version WalletVersion => version;
    }
}
