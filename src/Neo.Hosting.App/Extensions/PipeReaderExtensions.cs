// Copyright (C) 2015-2024 The Neo Project.
//
// PipeReaderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Extensions
{
    internal static class PipeReaderExtensions
    {
        public static async Task<PipeMessage> ReadPipeMessage(this PipeReader reader)
        {
            var readResult = await reader.ReadAsync();

            var result = PipeMessage.Create(PipeCommand.Version, new PipeVersion());

            var buffer = readResult.Buffer;
            if (buffer.IsSingleSegment)
                result.FromArray(buffer.FirstSpan.ToArray());
            else
            {
                byte[] tmpBuffer = [];

                foreach (var segment in buffer)
                    tmpBuffer = [.. tmpBuffer, .. segment.ToArray()];

                result.FromArray(tmpBuffer);
            }

            reader.AdvanceTo(buffer.End);

            return result;
        }
    }
}
