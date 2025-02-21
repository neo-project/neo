// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet
{
    internal class Program
    {
        private static Task<int> Main(string[] args)
        {
            Console.WriteLine("hello World!");
            return Task.FromResult(0);
        }
    }

}
