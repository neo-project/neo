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

namespace Neo.Build
{
    internal static partial class NeoBuildErrorCodes
    {
        /// <summary>
        /// All error codes caused by wallet related processing.
        /// </summary>
        public sealed class Wallet
        {
            private const int Base = General.Base * 2;

            private Wallet() { }

            public static int FileNotFound => Base + 1;
        }
    }
}
