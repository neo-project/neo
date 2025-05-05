// Copyright (C) 2015-2025 The Neo Project.
//
// MainService.Native.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.SmartContract.Native;
using System.Linq;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "list nativecontract" command
        /// </summary>
        [ConsoleCommand("list nativecontract", Category = "Native Contract")]
        private void OnListNativeContract()
        {
            var currentIndex = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            NativeContract.Contracts.ToList().ForEach(contract =>
            {
                var active = contract.IsActive(NeoSystem.Settings, currentIndex) ? "" : "not active yet";
                ConsoleHelper.Info($"\t{contract.Name,-20}", $"{contract.Hash} {active}");
            });
        }
    }
}
