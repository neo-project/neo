// Copyright (C) 2015-2025 The Neo Project.
//
// RestServerPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.Network.P2P;
using System;

namespace Neo.Plugins.RestServer
{
    public partial class RestServerPlugin : Plugin
    {
        public override string Name => "RestServer";
        public override string Description => "Enables REST Web Sevices for the node";

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "RestServer.json");

        #region Globals

        private RestServerSettings? _settings;
        private RestWebServer? _server;

        #endregion

        #region Static Globals

        internal static NeoSystem? NeoSystem { get; private set; }
        internal static LocalNode? LocalNode { get; private set; }

        #endregion

        protected override void Configure()
        {
            RestServerSettings.Load(GetConfiguration());
            _settings = RestServerSettings.Current;
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (_settings is null)
            {
                throw new Exception("'Configure' must be called first");
            }

            if (_settings.EnableCors && _settings.EnableBasicAuthentication && _settings.AllowOrigins.Length == 0)
            {
                ConsoleHelper.Warning("RestServer: CORS is misconfigured!");
                ConsoleHelper.Info($"You have {nameof(_settings.EnableCors)} and {nameof(_settings.EnableBasicAuthentication)} enabled but");
                ConsoleHelper.Info($"{nameof(_settings.AllowOrigins)} is empty in config.json for RestServer.");
                ConsoleHelper.Info("You must add url origins to the list to have CORS work from");
                ConsoleHelper.Info($"browser with basic authentication enabled.");
                ConsoleHelper.Info($"Example: \"AllowOrigins\": [\"http://{_settings.BindAddress}:{_settings.Port}\"]");
            }
            if (system.Settings.Network == _settings.Network)
            {
                NeoSystem = system;
                LocalNode = system.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;
            }
            _server = new RestWebServer();
            _server.Start();
        }
    }
}
