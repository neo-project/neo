// Copyright (C) 2015-2024 The Neo Project.
//
// PipeStreamExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipe
{
    internal static class PipeStreamExtensions
    {
        public static async Task WriteLineAsync(this PipeStream steam, string value)
        {
            using var sw = new StreamWriter(steam, encoding: Encoding.UTF8, leaveOpen: true);
            sw.AutoFlush = true;
            await sw.WriteLineAsync(value);
        }

        public static async ValueTask<string?> ReadLineAsync(this PipeStream stream, CancellationToken cancellationToken)
        {
            using var sr = new StreamReader(stream, encoding: Encoding.UTF8, leaveOpen: true);
            return await sr.ReadLineAsync(cancellationToken);
        }
    }
}
