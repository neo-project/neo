// Copyright (C) 2015-2026 The Neo Project.
//
// ScriptExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;

namespace Neo.Extensions
{
    public static class ScriptExtensions
    {
        /// <summary>
        /// Gets the size of the script encoded as variable-length bytes.
        /// </summary>
        /// <param name="script">The script to measure.</param>
        /// <returns>The size of the length prefix and script bytes.</returns>
        public static int GetVarSize(this Script script)
        {
            var length = script.Length;
            return length.GetVarSize() + length;
        }
    }
}
