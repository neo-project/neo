// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Compiler.CSharp is free software distributed under the MIT
// software license, see the accompanying file LICENSE in the main directory
// of the project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

namespace Neo.CLI
{
    public class CommandLineOptions
    {
        public string? Config { get; init; }
        public string? Network { get; init; }
        public string? Wallet { get; init; }
        public string? Password { get; init; }
        public string[]? Plugins { get; set; }
        public string? DBEngine { get; init; }
        public string? DBPath { get; init; }
        public bool? NoVerify { get; init; }
    }
}
