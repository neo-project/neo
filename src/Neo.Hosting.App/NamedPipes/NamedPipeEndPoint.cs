// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeEndPoint.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Neo.Hosting.App.NamedPipes
{
    internal sealed class NamedPipeEndPoint(
        string pipeName,
        string serverName) : EndPoint
    {
        internal const string LocalComputerServerName = ".";

        public string ServerName { get; } = serverName;
        public string PipeName { get; } = pipeName;

        public NamedPipeEndPoint(string pipeName) : this(pipeName, LocalComputerServerName) { }
        public NamedPipeEndPoint(IOptions<NeoOptions> options) : this(options.Value.NamedPipe.Name, LocalComputerServerName) { }
        public NamedPipeEndPoint(NamedPipeOptions options) : this(options.Name, LocalComputerServerName) { }

        public override string ToString() =>
            $@"\\{ServerName}\pipe\{PipeName}";

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is NamedPipeEndPoint other &&
            other.ServerName == ServerName &&
            other.PipeName == PipeName;

        public override int GetHashCode() =>
            ServerName.GetHashCode() ^ PipeName.GetHashCode();
    }
}
