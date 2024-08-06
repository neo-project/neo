// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using NamedPipeService;
using Neo.Plugins.Configuration;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net;

namespace Neo.Plugins.NamedPipeService.Tests
{
    internal static class NamedPipeFactory
    {
        public const string LocalComputerServerName = ".";

        public static NamedPipeEndPoint GetUniquePipeName() =>
            new(Path.GetRandomFileName());

        public static bool CanBind(EndPoint endPoint) =>
            endPoint is NamedPipeEndPoint;

        public static NamedPipeClientStream CreateClientStream(NamedPipeEndPoint remoteEndPoint) =>
            new(remoteEndPoint.ServerName, remoteEndPoint.PipeName, PipeDirection.InOut,
                PipeOptions.WriteThrough | PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        public static NamedPipeEndPoint CreateEndPoint(string pipeName) =>
            new(pipeName, LocalComputerServerName);

        public static NamedPipeServerListener CreateListener(
            NamedPipeEndPoint endPoint,
            NamedPipeServerTransportOptions? options = null)
        {
            if (endPoint.ServerName != LocalComputerServerName)
                throw new NotSupportedException($"Server name '{endPoint.ServerName}' is invalid. The Server name must be \"{LocalComputerServerName}\".");

            var listener = new NamedPipeServerListener(endPoint, options ?? new());

            return listener;
        }
    }
}
