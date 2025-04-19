// Copyright (C) 2015-2025 The Neo Project.
//
// LedgerContractExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using Neo.Plugins.RestServer.Models.Blockchain;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.RestServer.Extensions
{
    internal static class LedgerContractExtensions
    {
        public static IEnumerable<AccountDetails> ListAccounts(this GasToken gasToken, DataCache snapshot, ProtocolSettings protocolSettings) =>
            gasToken
                .GetAccounts(snapshot)
                .Select(s =>
                    new AccountDetails
                    {
                        ScriptHash = s.Address,
                        Address = s.Address.ToAddress(protocolSettings.AddressVersion),
                        Balance = s.Balance,
                        Decimals = gasToken.Decimals,
                    });

        public static IEnumerable<AccountDetails> ListAccounts(this NeoToken neoToken, DataCache snapshot, ProtocolSettings protocolSettings) =>
            neoToken
                .GetAccounts(snapshot)
                .Select(s =>
                    new AccountDetails
                    {
                        ScriptHash = s.Address,
                        Address = s.Address.ToAddress(protocolSettings.AddressVersion),
                        Balance = s.Balance,
                        Decimals = neoToken.Decimals,
                    });
    }
}
