// Copyright (C) 2015-2024 The Neo Project.
//
// RpcServerPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins
{
    public class RpcServerPlugin : Plugin
    {
        public override string Name => "RpcServer";
        public override string Description => "Enables RPC for the node";

        private Settings settings;
        private static readonly Dictionary<uint, RpcServer> servers = new();
        private static readonly Dictionary<uint, List<object>> handlers = new();

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "RpcServer.json");

        protected override void Configure()
        {
            settings = new Settings(GetConfiguration());
            foreach (RpcServerSettings s in settings.Servers)
                if (servers.TryGetValue(s.Network, out RpcServer server))
                    server.UpdateSettings(s);
        }

        public override void Dispose()
        {
            foreach (var (_, server) in servers)
                server.Dispose();
            base.Dispose();
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            RpcServerSettings s = settings.Servers.FirstOrDefault(p => p.Network == system.Settings.Network);
            if (s is null) return;

            if (s.EnableCors && string.IsNullOrEmpty(s.RpcUser) == false && s.AllowOrigins.Length == 0)
            {
                Log("RcpServer: CORS is misconfigured!", LogLevel.Warning);
                Log($"You have {nameof(s.EnableCors)} and Basic Authentication enabled but " +
                $"{nameof(s.AllowOrigins)} is empty in config.json for RcpServer. " +
                "You must add url origins to the list to have CORS work from " +
                $"browser with basic authentication enabled. " +
                $"Example: \"AllowOrigins\": [\"http://{s.BindAddress}:{s.Port}\"]", LogLevel.Info);
            }

            RpcServer server = new(system, s);

            if (handlers.Remove(s.Network, out var list))
            {
                foreach (var handler in list)
                {
                    server.RegisterMethods(handler);
                }
            }

            server.StartRpcServer();
            servers.TryAdd(s.Network, server);
        }

        public static void RegisterMethods(object handler, uint network)
        {
            if (servers.TryGetValue(network, out RpcServer server))
            {
                server.RegisterMethods(handler);
                return;
            }
            if (!handlers.TryGetValue(network, out var list))
            {
                list = new List<object>();
                handlers.Add(network, list);
            }
            list.Add(handler);
        }
    }
}
