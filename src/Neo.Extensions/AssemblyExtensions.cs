// Copyright (C) 2015-2025 The Neo Project.
//
// AssemblyExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Reflection;

namespace Neo.Extensions
{
    public static class AssemblyExtensions
    {
        public static string GetVersion(this Assembly assembly)
        {
            return assembly.GetName().Version!.ToString(3);
        }
    }
}
