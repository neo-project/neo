// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildAttributeHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Attributes;
using System;
using System.ComponentModel;

namespace Neo.Build.Core.Helpers
{
    internal static class NeoBuildAttributeHelper
    {
        public static UInt160 ExtractContractScriptHash(Type type)
        {
            var scriptHashAttr = Attribute.GetCustomAttribute(type, typeof(ContractScriptHashAttribute)) as ContractScriptHashAttribute;
            if (scriptHashAttr is not null)
                return scriptHashAttr.ScriptHash;

            return UInt160.Zero;
        }

        public static string ExtractContractName(Type type)
        {
            var displayNameAttr = Attribute.GetCustomAttribute(type, typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            if (displayNameAttr is not null)
                return displayNameAttr.DisplayName;

            return type.Name; // Class Name
        }
    }
}
