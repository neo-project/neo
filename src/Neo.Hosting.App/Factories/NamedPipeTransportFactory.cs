// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeTransportFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.NamedPipes;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Factories
{
    internal sealed class NamedPipeTransportFactory
    {
        private const string LocalComputerServerName = ".";

        private readonly ILoggerFactory _loggerFactory;
        private readonly ObjectPoolProvider _objectPoolProvider;
        private readonly NamedPipeTransportOptions _options;

        public NamedPipeTransportFactory(
            ILoggerFactory loggerFactory,
            IOptions<NamedPipeTransportOptions> options,
            ObjectPoolProvider objectPoolProvider)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _loggerFactory = loggerFactory;
            _objectPoolProvider = objectPoolProvider;
            _options = options.Value;
        }

        public ValueTask<NamedPipeConnectionListener> BindAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(endPoint);

            if (endPoint is not NamedPipeEndPoint namedPipeEndPoint)
                throw new NotSupportedException($"{endPoint.GetType()} is not supported.");
            if (namedPipeEndPoint.ServerName != LocalComputerServerName)
                throw new NotSupportedException($"Server name '{namedPipeEndPoint.ServerName}' is invalid. The Server name must be \"{LocalComputerServerName}\".");

            var listener = new NamedPipeConnectionListener(namedPipeEndPoint, _options, _loggerFactory, _objectPoolProvider);

            try
            {
                listener.Start();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException($"Named pipe '{namedPipeEndPoint.PipeName}' is already in use.", ex);
            }

            return new ValueTask<NamedPipeConnectionListener>(listener);
        }

        public bool CanBind(EndPoint endPoint)
        {
            ArgumentNullException.ThrowIfNull(endPoint);

            return endPoint is NamedPipeEndPoint;
        }

        public static string GetUniquePipeName() => Path.GetRandomFileName();

        public static NamedPipeTransportFactory Create(
            ILoggerFactory? loggerFactory = null,
            NamedPipeTransportOptions? options = null,
            ObjectPoolProvider? objectPoolProvider = null)
        {
            options ??= new NamedPipeTransportOptions();
            return new(loggerFactory ?? NullLoggerFactory.Instance, Options.Create(options), objectPoolProvider ?? new DefaultObjectPoolProvider());
        }

        public static async Task<NamedPipeConnectionListener> CreateConnectionListener(
            ILoggerFactory? loggerFactory = null,
            string? pipeName = null,
            NamedPipeTransportOptions? options = null,
            ObjectPoolProvider? objectPoolProvider = null)
        {
            var transportFactory = Create(loggerFactory, options, objectPoolProvider);
            var endPoint = new NamedPipeEndPoint(pipeName ?? GetUniquePipeName());
            return await transportFactory.BindAsync(endPoint, CancellationToken.None);
        }

        public static NamedPipeClientStream CreateClientStream(EndPoint remoteEndPoint, TokenImpersonationLevel? impersonationLevel = null)
        {
            var namedPipeEndPoint = (NamedPipeEndPoint)remoteEndPoint;
            return new(namedPipeEndPoint.ServerName, namedPipeEndPoint.PipeName,
                PipeDirection.InOut, PipeOptions.WriteThrough | PipeOptions.Asynchronous,
                impersonationLevel ?? TokenImpersonationLevel.Anonymous);
        }
    }
}
