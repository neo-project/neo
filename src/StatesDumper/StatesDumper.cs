// Copyright (C) 2015-2024 The Neo Project.
//
// StatesDumper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins
{
    public class StatesDumper : Plugin
    {
        private readonly Dictionary<uint, JArray> bs_cache = new Dictionary<uint, JArray>();
        private readonly Dictionary<uint, NeoSystem> systems = new Dictionary<uint, NeoSystem>();

        public override string Description => "Exports Neo-CLI status data";

        public StatesDumper()
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
            Settings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            systems.Add(system.Settings.Network, system);
        }

        /// <summary>
        /// Process "dump storage" command
        /// </summary>
        [ConsoleCommand("dump storage", Category = "Storage", Description = "You can specify the contract script hash or use null to get the corresponding information from the storage")]
        private void OnDumpStorage(uint network, UInt160 contractHash = null)
        {
            if (!systems.ContainsKey(network)) throw new InvalidOperationException("invalid network");
            string path = $"dump_{network:x8}.json";
            byte[] prefix = null;
            if (contractHash is not null)
            {
                var contract = NativeContract.ContractManagement.GetContract(systems[network].StoreView, contractHash);
                if (contract is null) throw new InvalidOperationException("contract not found");
                prefix = BitConverter.GetBytes(contract.Id);
            }
            var states = systems[network].StoreView.Find(prefix);
            JArray array = new JArray(states.Where(p => !Settings.Default.Exclude.Contains(p.Key.Id)).Select(p => new JObject
            {
                ["key"] = Convert.ToBase64String(p.Key.ToArray()),
                ["value"] = Convert.ToBase64String(p.Value.ToArray())
            }));
            File.WriteAllText(path, array.ToString());
            ConsoleHelper.Info("States",
                $"({array.Count})",
                " have been dumped into file ",
                $"{path}");
        }

        private void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (Settings.Default.PersistAction.HasFlag(PersistActions.StorageChanges))
                OnPersistStorage(system.Settings.Network, snapshot);
        }

        private void OnPersistStorage(uint network, DataCache snapshot)
        {
            uint blockIndex = NativeContract.Ledger.CurrentIndex(snapshot);
            if (blockIndex >= Settings.Default.HeightToBegin)
            {
                JArray array = new JArray();

                foreach (var trackable in snapshot.GetChangeSet())
                {
                    if (Settings.Default.Exclude.Contains(trackable.Key.Id))
                        continue;
                    JObject state = new JObject();
                    switch (trackable.State)
                    {
                        case TrackState.Added:
                            state["state"] = "Added";
                            state["key"] = Convert.ToBase64String(trackable.Key.ToArray());
                            state["value"] = Convert.ToBase64String(trackable.Item.ToArray());
                            // Here we have a new trackable.Key and trackable.Item
                            break;
                        case TrackState.Changed:
                            state["state"] = "Changed";
                            state["key"] = Convert.ToBase64String(trackable.Key.ToArray());
                            state["value"] = Convert.ToBase64String(trackable.Item.ToArray());
                            break;
                        case TrackState.Deleted:
                            state["state"] = "Deleted";
                            state["key"] = Convert.ToBase64String(trackable.Key.ToArray());
                            break;
                    }
                    array.Add(state);
                }

                JObject bs_item = new JObject();
                bs_item["block"] = blockIndex;
                bs_item["size"] = array.Count;
                bs_item["storage"] = array;
                if (!bs_cache.TryGetValue(network, out JArray cache))
                {
                    cache = new JArray();
                }
                cache.Add(bs_item);
                bs_cache[network] = cache;
            }
        }

        private void OnCommitted(NeoSystem system, Block block)
        {
            if (Settings.Default.PersistAction.HasFlag(PersistActions.StorageChanges))
                OnCommitStorage(system.Settings.Network, system.StoreView);
        }

        void OnCommitStorage(uint network, DataCache snapshot)
        {
            if (!bs_cache.TryGetValue(network, out JArray cache)) return;
            if (cache.Count == 0) return;
            uint blockIndex = NativeContract.Ledger.CurrentIndex(snapshot);
            if ((blockIndex % Settings.Default.BlockCacheSize == 0) || (Settings.Default.HeightToStartRealTimeSyncing != -1 && blockIndex >= Settings.Default.HeightToStartRealTimeSyncing))
            {
                string path = HandlePaths(network, blockIndex);
                path = $"{path}/dump-block-{blockIndex}.json";
                File.WriteAllText(path, cache.ToString());
                cache.Clear();
            }
        }

        private static string HandlePaths(uint network, uint blockIndex)
        {
            //Default Parameter
            uint storagePerFolder = 100000;
            uint folder = (((blockIndex - 1) / storagePerFolder) + 1) * storagePerFolder;
            if (blockIndex == 0)
                folder = 0;
            string dirPathWithBlock = $"./Storage_{network:x8}/BlockStorage_{folder}";
            Directory.CreateDirectory(dirPathWithBlock);
            return dirPathWithBlock;
        }
    }
}
