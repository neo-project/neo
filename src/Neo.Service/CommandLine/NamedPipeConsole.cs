// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeConsole.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine;
using System.CommandLine.IO;
using System.IO;

namespace Neo.Service.CommandLine
{
    internal class NamedPipeConsole(
        StreamReader input,
        StreamWriter output) : IConsole, IStandardStreamWriter
    {
        public IStandardStreamWriter Out => this;
        public IStandardStreamWriter Error => this;

        public bool IsOutputRedirected => false;
        public bool IsErrorRedirected => false;
        public bool IsInputRedirected => false;

        public void Write(string? value)
        {
            output.Write(value);
        }

        public string? ReadLine()
        {
            return input.ReadLine();
        }
    }
}
