// Copyright (C) 2015-2021 NEO GLOBAL DEVELOPMENT.
// 
// The Neo project is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Wallets.NEP6
{
    internal class WalletLocker : IDisposable
    {
        private readonly NEP6Wallet wallet;

        public WalletLocker(NEP6Wallet wallet)
        {
            this.wallet = wallet;
        }

        public void Dispose()
        {
            wallet.Lock();
        }
    }
}
