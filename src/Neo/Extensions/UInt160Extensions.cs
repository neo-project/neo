// Copyright (C) 2015-2025 The Neo Project.
//
// UInt160Extensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;

namespace Neo.Extensions
{
    public static class UInt160Extensions
    {
        /// <summary>
        /// Generates the script for calling a contract dynamically.
        /// </summary>
        /// <param name="scriptHash">The hash of the contract to be called.</param>
        /// <param name="method">The method to be called in the contract.</param>
        /// <param name="args">The arguments for calling the contract.</param>
        /// <returns>The generated script.</returns>
        public static byte[] MakeScript(this UInt160 scriptHash, string method, params object[] args)
        {
            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(scriptHash, method, args);
            return sb.ToArray();
        }
    }
}
