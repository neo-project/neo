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

using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Serilog;
using System;
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
        private static readonly Dictionary<uint, RpcServer> servers = new();
        private static readonly Dictionary<uint, List<object>> handlers = new();

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "RpcServer.json");
        protected override UnhandledExceptionPolicy ExceptionPolicy => settings.ExceptionPolicy;

        protected override void Configure()
        {
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
            foreach (var server in servers)
            {
                Serilog.Log.Information("Disposing RpcServer for network {Network}", server.Key);
                server.Value.Dispose();
            }
            base.Dispose();
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            RpcServerSettings s = settings.Servers.FirstOrDefault(p => p.Network == system.Settings.Network);
            if (s is null) return;

            if (s.EnableCors && string.IsNullOrEmpty(s.RpcUser) == false && s.AllowOrigins.Length == 0)
            {
                _log.Warning("RcpServer: CORS is misconfigured!");
                _log.Information($"You have {nameof(s.EnableCors)} and Basic Authentication enabled but " +
                $"{nameof(s.AllowOrigins)} is empty in config.json for RcpServer. " +
                "You must add url origins to the list to have CORS work from " +
                $"browser with basic authentication enabled. " +
                $"Example: \"AllowOrigins\": [\"http://{s.BindAddress}:{s.Port}\"]", LogLevel.Info);
            }

            RpcServer rpcRpcServer = new(system, s);

            if (handlers.Remove(s.Network, out var list))
            {
                foreach (var handler in list)
                {
                    rpcRpcServer.RegisterMethods(handler);
                }
            }

            rpcRpcServer.StartRpcServer();
            servers.TryAdd(s.Network, rpcRpcServer);
        }

        public static void RegisterMethods(object handler, uint network)
        {
            if (!handlers.TryGetValue(network, out var list))
            {
                list = new List<object>();
                handlers.Add(network, list);
            }

            if (servers.TryGetValue(network, out var server))
            {
                Serilog.Log.Information("RpcServer for network {Network} loading RpcMethods from {HandlerType}", network, handler.GetType().FullName);
                server.RegisterMethods(handler);
            }

            list.Add(handler);
        }
    }
}
