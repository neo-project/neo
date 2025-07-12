// Copyright (C) 2015-2025 The Neo Project.
//
// WalletExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Build.Core.Extensions
{
    using Helper = Neo.SmartContract.Helper;

    public static class WalletExtensions
    {
        public static IEnumerable<WalletAccount> GetMultiSigAccounts(this Wallet wallet) =>
            wallet.GetAccounts().Where(static w => Helper.IsMultiSigContract(w.Contract.Script));
    }
}
