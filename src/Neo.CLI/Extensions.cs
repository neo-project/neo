// Copyright (C) 2015-2024 The Neo Project.
//
// Extensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Linq;
using System.Reflection;

namespace Neo
{
    /// <summary>
    /// Extension methods
    /// </summary>
    internal static class Extensions
    {
        public static string GetVersion(this Assembly assembly)
        {
            CustomAttributeData? attribute = assembly.CustomAttributes.FirstOrDefault(p => p.AttributeType == typeof(AssemblyInformationalVersionAttribute));
            if (attribute == null) return assembly.GetName().Version?.ToString(3) ?? string.Empty;
            return (string)attribute.ConstructorArguments[0].Value!;
        }
    }
}
