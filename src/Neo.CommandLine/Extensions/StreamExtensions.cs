// Copyright (C) 2015-2024 The Neo Project.
//
// StreamExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CommandLine.Services.Messages;
using Neo.IO;
using System.IO;
using System.Text;

namespace Neo.CommandLine.Extensions
{
    internal static class StreamExtensions
    {
        public static void Write(this Stream s, PipeMessage message)
        {
            using var bw = new BinaryWriter(s, Encoding.UTF8, true);
            bw.Write(message);
            bw.Flush();
        }

        public static PipeMessage? ReadMessage(this Stream s) =>
            PipeMessage.ReadFromStream(s);
    }
}
