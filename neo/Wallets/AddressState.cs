using System;

namespace Neo.Wallets
{
    [Flags]
    internal enum AddressState : byte
    {
        None = 0,

        InWallet = 1 << 0,
        WatchOnly = 1 << 1
    }
}
