// Copyright (C) 2015-2024 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Manifest;
using System;

namespace Neo.CLI
{
    internal static class Helper
    {
        public static bool IsYes(this string input)
        {
            if (input == null) return false;

            input = input.ToLowerInvariant();

            return input == "yes" || input == "y";
        }

        public static string ToBase64String(this byte[] input) => System.Convert.ToBase64String(input);

        public static void IsScriptValid(this ReadOnlyMemory<byte> script, ContractAbi abi)
        {
            try
            {
                SmartContract.Helper.Check(script.ToArray(), abi);
            }
            catch (Exception e)
            {
                throw new FormatException($"Bad Script or Manifest Format: {e.Message}");
            }
        }
    }
}
