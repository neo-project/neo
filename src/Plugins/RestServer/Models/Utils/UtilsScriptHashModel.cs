// Copyright (C) 2015-2025 The Neo Project.
//
// UtilsScriptHashModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RestServer.Models.Utils
{
    internal class UtilsScriptHashModel
    {
        /// <summary>
        /// Scripthash of the wallet account exported.
        /// </summary>
        /// <example>0xed7cc6f5f2dd842d384f254bc0c2d58fb69a4761</example>
        public UInt160 ScriptHash { get; set; } = UInt160.Zero;
    }
}
