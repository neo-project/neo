// Copyright (C) 2015-2024 The Neo Project.
//
// ContractCommandHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.CommandLine.Handlers
{
    internal static class ContractCommandHandler
    {
        public static void OnDeploy(FileInfo nefFileInfo, string? dataParameter = null)
        {
            Console.WriteLine("Exec OnContractDeploy");
        }

        public static void OnUpdate(string scriptHash, FileInfo nefFileInfo, string? txSender = null, string? txSigner = null, string? dataParameter = null)
        {
            Console.WriteLine("Exec OnContractUpdate");
        }

        public static void OnInvoke(string scriptHash, string methodName, IEnumerable<string>? methodParameters = null, string? txSender = null, string? txSigner = null)
        {
            Console.WriteLine("Exec OnContractInvoke");
        }
    }
}
