// Copyright (C) 2015-2024 The Neo Project.
//
// TokensTracker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Neo.Plugins.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.IO.Path;

namespace Neo.Plugins
{
    public class TokensTracker : Plugin
    {
        private string _dbPath;
        private bool _shouldTrackHistory;
        private uint _maxResults;
        private uint _network;
        private string[] _enabledTrackers;
        private IStore _db;
        private UnhandledExceptionPolicy _exceptionPolicy;
        private NeoSystem neoSystem;
        private readonly List<TrackerBase> trackers = new();
        protected override UnhandledExceptionPolicy ExceptionPolicy => _exceptionPolicy;

        public override string Description => "Enquiries balances and transaction history of accounts through RPC";

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "TokensTracker.json");

        public TokensTracker()
        {
            Blockchain.Committing += OnCommitting;
            Blockchain.Committed += OnCommitted;
        }

        public override void Dispose()
        {
            Blockchain.Committing -= OnCommitting;
            Blockchain.Committed -= OnCommitted;
        }

        protected override void Configure()
        {
            IConfigurationSection config = GetConfiguration();
            _dbPath = config.GetValue("DBPath", "TokensBalanceData");
            _shouldTrackHistory = config.GetValue("TrackHistory", true);
            _maxResults = config.GetValue("MaxResults", 1000u);
            _network = config.GetValue("Network", 860833102u);
            _enabledTrackers = config.GetSection("EnabledTrackers").GetChildren().Select(p => p.Value).ToArray();
            var policyString = config.GetValue(nameof(UnhandledExceptionPolicy), nameof(UnhandledExceptionPolicy.StopNode));
            if (Enum.TryParse(policyString, out UnhandledExceptionPolicy policy))
            {
                _exceptionPolicy = policy;
            }
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Network != _network) return;
            neoSystem = system;
            string path = string.Format(_dbPath, neoSystem.Settings.Network.ToString("X8"));
            _db = neoSystem.LoadStore(GetFullPath(path));
            if (_enabledTrackers.Contains("NEP-11"))
                trackers.Add(new Trackers.NEP_11.Nep11Tracker(_db, _maxResults, _shouldTrackHistory, neoSystem));
            if (_enabledTrackers.Contains("NEP-17"))
                trackers.Add(new Trackers.NEP_17.Nep17Tracker(_db, _maxResults, _shouldTrackHistory, neoSystem));
            foreach (TrackerBase tracker in trackers)
                RpcServerPlugin.RegisterMethods(tracker, _network);
        }

        private void ResetBatch()
        {
            foreach (var tracker in trackers)
            {
                tracker.ResetBatch();
            }
        }

        private void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network != _network) return;
            // Start freshly with a new DBCache for each block.
            ResetBatch();
            foreach (var tracker in trackers)
            {
                tracker.OnPersist(system, block, snapshot, applicationExecutedList);
            }
        }

        private void OnCommitted(NeoSystem system, Block block)
        {
            if (system.Settings.Network != _network) return;
            foreach (var tracker in trackers)
            {
                tracker.Commit();
            }
        }
    }
}
