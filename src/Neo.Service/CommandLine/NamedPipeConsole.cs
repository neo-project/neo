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

using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace Neo.Service.CommandLine
{
    internal class NamedPipeConsole : IConsole, IStandardStreamWriter
    {
        public IStandardStreamWriter Out => this;

        public bool IsOutputRedirected => true;

        public IStandardStreamWriter Error => this;

        public bool IsErrorRedirected => true;

        public bool IsInputRedirected => true;

        public void Write(string? value)
        {
            throw new NotImplementedException();
        }
    }
}
