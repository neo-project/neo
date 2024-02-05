// Copyright (C) 2015-2024 The Neo Project.
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
            NativeContract.Contracts.ToList().ForEach(p => ConsoleHelper.Info($"\t{p.Name,-20}", $"{p.Hash}"));
        }
    }
}
