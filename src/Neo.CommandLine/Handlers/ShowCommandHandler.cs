// Copyright (C) 2015-2024 The Neo Project.
//
// ShowCommandHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.CommandLine.Handlers
{
    internal static class ShowCommandHandler
    {
        public static void OnBlock(string blockIndexOrHash)
        {
            Console.WriteLine("Exec OnShowBlock");
        }

        public static void OnTransaction(string txHash)
        {
            Console.WriteLine("Exec OnShowTransaction");
        }

        public static void OnContract(string contractIdOrHash)
        {
            Console.WriteLine("Exec OnShowContract");
        }

        public static void OnVersion(string serverName, uint taskTimeout)
        {
            Console.WriteLine("Exec OnShowVersion");
        }
    }
}
