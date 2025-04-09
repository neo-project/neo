// Copyright (C) 2015-2025 The Neo Project.
//
// RpcServerPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.RpcServer
{
    public class RpcServerPlugin : Plugin
    {
        private static readonly ILogger _log = Log.ForContext<RpcServerPlugin>();

        public override string Name => "RpcServer";
        public override string Description => "Enables RPC for the node";

        private Settings settings;
        private static readonly ConcurrentDictionary<uint, RpcServer> servers = new();
        private static readonly ConcurrentDictionary<uint, List<object>> handlers = new();

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "RpcServer.json");
        protected override UnhandledExceptionPolicy ExceptionPolicy => settings.ExceptionPolicy;

        protected override void Configure()
        {
            _log.Information("Configuring RpcServerPlugin...");
            settings = new Settings(GetConfiguration());
            foreach (RpcServerSettings s in settings.Servers)
            {
                if (s.EnableCors && string.IsNullOrEmpty(s.RpcUser) == false && s.AllowOrigins.Length == 0)
                {
                    _log.Warning("RpcServer CORS misconfiguration: Basic Auth enabled but AllowOrigins is empty for Network {Network}", s.Network);
                    _log.Information("CORS with Basic Authentication requires specifying origins in AllowOrigins. Example: \"AllowOrigins\": [\"http://{BindAddress}:{Port}\"]", s.BindAddress, s.Port);
                }
                if (servers.TryGetValue(s.Network, out RpcServer server))
                {
                    _log.Information("Updating settings for existing RpcServer on network {Network}", s.Network);
                    server.UpdateSettings(s);
                }
            }
        }

        public override void Dispose()
        {
            _log.Information("Disposing RpcServerPlugin...");
            foreach (var server in servers)
            {
                _log.Information("Disposing RpcServer for network {Network}", server.Key);
                server.Value.Dispose();
            }
            base.Dispose();
            _log.Information("RpcServerPlugin disposed.");
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            RpcServerSettings s = settings.Servers.FirstOrDefault(p => p.Network == system.Settings.Network);
            if (s is null)
            {
                _log.Warning("No RpcServer configuration found for network {Network}", system.Settings.Network);
                return;
            }

            if (s.EnableCors && string.IsNullOrEmpty(s.RpcUser) == false && s.AllowOrigins.Length == 0)
            {
                _log.Warning("RcpServer: CORS is misconfigured!");
                _log.Information($"You have {nameof(s.EnableCors)} and Basic Authentication enabled but " +
                $"{nameof(s.AllowOrigins)} is empty in config.json for RcpServer. " +
                "You must add url origins to the list to have CORS work from " +
                $"browser with basic authentication enabled. " +
                $"Example: \"AllowOrigins\": [\"http://{s.BindAddress}:{s.Port}\"]");
            }

            RpcServer rpcRpcServer = new(system, s);

            if (handlers.TryRemove(s.Network, out var list))
            {
                foreach (var handler in list)
                {
                    _log.Debug("Registering RPC methods from handler {HandlerType} for network {Network}", handler.GetType().FullName, s.Network);
                    rpcRpcServer.RegisterMethods(handler);
                }
            }

            rpcRpcServer.StartRpcServer();
            servers.TryAdd(s.Network, rpcRpcServer);
            _log.Information("RpcServer started for network {Network} on {BindAddress}:{Port}", s.Network, s.BindAddress, s.Port);
            base.OnSystemLoaded(system);
        }

        public static void RegisterMethods(object handler, uint network = 0u)
        {
            if (servers.TryGetValue(network, out RpcServer server))
            {
                _log.Debug("Registering methods from {HandlerType} immediately for network {Network}", handler.GetType().FullName, network);
                server.RegisterMethods(handler);
            }
            else
            {
                if (!handlers.TryGetValue(network, out List<object> list))
                {
                    list = [];
                    handlers.TryAdd(network, list);
                }
                _log.Debug("Queueing methods from {HandlerType} for later registration for network {Network}", handler.GetType().FullName, network);
                list.Add(handler);
            }
        }
    }
}
