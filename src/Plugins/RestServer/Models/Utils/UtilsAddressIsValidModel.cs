// Copyright (C) 2015-2025 The Neo Project.
//
// UtilsAddressIsValidModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RestServer.Models.Utils
{
    internal class UtilsAddressIsValidModel : UtilsAddressModel
    {
        /// <summary>
        /// Indicates if address can be converted to ScriptHash or Neo Address.
        /// </summary>
        /// <example>true</example>
        public bool IsValid { get; set; }
    }
}
