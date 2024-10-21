// Copyright (C) 2015-2024 The Neo Project.
//
// DuplexPipe.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.IO.Pipelines;

namespace Neo.CLI.Pipes
{
    internal class DuplexPipe(
        PipeReader reader,
        PipeWriter writer) : IDuplexPipe
    {
        public PipeReader Input => reader;
        public PipeWriter Output => writer;

        public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
        {
            var input = new Pipe(inputOptions);
            var output = new Pipe(outputOptions);

            // Use Transport for Input and Output for Application
            var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
            var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

            return new(applicationToTransport, transportToApplication);
        }

        public readonly struct DuplexPipePair(
            IDuplexPipe transport,
            IDuplexPipe application)
        {
            /// <summary>
            /// Client to server.
            /// </summary>
            public IDuplexPipe Transport { get; } = transport;

            /// <summary>
            /// Server to client.
            /// </summary>
            public IDuplexPipe Application { get; } = application;
        }
    }
}
