// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes
{
    internal sealed class NamedPipeService : IDisposable
    {
        private static readonly ConcurrentDictionary<PipeMethodAttribute, Func<ISerializable, ISerializable?>> s_methods = new();

        public static Version Version => NodeUtilities.GetApplicationVersion();
        public static string PipeName => $"neo.node\\{Version.ToString(3)}\\CommandShell";

        private readonly ILogger<PipeServer> _pipeServerLogger;
        private readonly ILogger<NamedPipeService> _logger;
        private readonly List<PipeServer> _pipeServers;
        private readonly PeriodicTimer _periodicTimer;

        public NamedPipeService(
            ILoggerFactory loggerFactory)
        {
            _pipeServers = new();
            _pipeServerLogger = loggerFactory.CreateLogger<PipeServer>();
            _logger = loggerFactory.CreateLogger<NamedPipeService>();
            _periodicTimer = new(TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            _periodicTimer.Dispose();
            ShutdownServers();
        }

        public static void RegisterMethods(object handler)
        {
            var handlerType = handler.GetType();
            var methods = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var pipeAttr = method.GetCustomAttribute<PipeMethodAttribute>();
                if (pipeAttr == null) continue;
                if (s_methods.ContainsKey(pipeAttr) && pipeAttr.Overwrite == false)
                    throw new AmbiguousMatchException($"{handlerType.FullName}.{method.Name}: Command {pipeAttr.Command} already exists.");
                s_methods[pipeAttr] = method.CreateDelegate<Func<ISerializable, ISerializable?>>(handler);
            }
        }

        public async Task StartAsync(int maxAllowConnections, CancellationToken cancellationToken)
        {
            for (var i = 0; i < maxAllowConnections; i++)
                CreateNewServer();

            _logger.LogInformation("Created {Connections} instances.", maxAllowConnections);

            await WaitAsync(cancellationToken);
        }

        private void CreateNewServer()
        {
            var server = new PipeServer(_pipeServerLogger);
            _pipeServers.Add(server);
            _ = Task.Factory.StartNew(server.StartAndListen);
            _logger.LogInformation("Created new instance.");
        }

        private void ShutdownServers()
        {
            foreach (var server in _pipeServers)
                server.Dispose();

            _pipeServers.Clear();
            _logger.LogInformation("Shutdown complete.");
        }

        private async Task WaitAsync(CancellationToken cancellationToken)
        {
            while (await _periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                for (var i = 0; i < _pipeServers.Count; i++)
                {
                    if (_pipeServers[i].IsShutdown == false) continue;

                    _logger.LogError("Restarting instance {Instance}.", i);

                    _pipeServers.RemoveAt(i);
                    CreateNewServer();
                }
            }

            _logger.LogDebug("Shutting down...");

            ShutdownServers();
        }
    }
}
