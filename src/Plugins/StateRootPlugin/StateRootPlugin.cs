// Copyright (C) 2015-2025 The Neo Project.
//
// StateRootPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.IEventHandlers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.StateRootPlugin.Storage;
using Neo.Plugins.StateRootPlugin.Verification;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.StateRootPlugin
{
    public class StateRootPlugin : Plugin, ICommittingHandler, ICommittedHandler, IWalletChangedHandler, IServiceAddedHandler
    {
        public const string StatePayloadCategory = "StateService";
        public override string Name => "StateRootPlugin";
        public override string Description => "Enables MPT state root calculation for the node";
        public override string ConfigFile => System.IO.Path.Combine(RootPath, "StateRootPlugin.json");

        protected override UnhandledExceptionPolicy ExceptionPolicy => StateRootSettings.Default.ExceptionPolicy;

        internal IActorRef Store;
        internal IActorRef Verifier;

        private static NeoSystem _system;

        internal static NeoSystem NeoSystem => _system;

        private IWalletProvider walletProvider;

        public StateRootPlugin()
        {
            Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed += ((ICommittedHandler)this).Blockchain_Committed_Handler;
        }

        protected override void Configure()
        {
            StateRootSettings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Network != StateRootSettings.Default.Network) return;
            _system = system;
            Store = _system.ActorSystem.ActorOf(StateStore.Props(this, string.Format(StateRootSettings.Default.Path, system.Settings.Network.ToString("X8"))));
            _system.ServiceAdded += ((IServiceAddedHandler)this).NeoSystem_ServiceAdded_Handler;
        }

        void IServiceAddedHandler.NeoSystem_ServiceAdded_Handler(object sender, object service)
        {
            if (service is IWalletProvider)
            {
                walletProvider = service as IWalletProvider;
                _system.ServiceAdded -= ((IServiceAddedHandler)this).NeoSystem_ServiceAdded_Handler;
                if (StateRootSettings.Default.AutoVerify)
                {
                    walletProvider.WalletChanged += ((IWalletChangedHandler)this).IWalletProvider_WalletChanged_Handler;
                }
            }
        }

        void IWalletChangedHandler.IWalletProvider_WalletChanged_Handler(object sender, Wallet wallet)
        {
            walletProvider.WalletChanged -= ((IWalletChangedHandler)this).IWalletProvider_WalletChanged_Handler;
            Start(wallet);
        }

        public override void Dispose()
        {
            base.Dispose();
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed -= ((ICommittedHandler)this).Blockchain_Committed_Handler;
            if (Store is not null) _system.EnsureStopped(Store);
            if (Verifier is not null) _system.EnsureStopped(Verifier);
        }

        void ICommittingHandler.Blockchain_Committing_Handler(NeoSystem system, Block block, DataCache snapshot,
            IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network != StateRootSettings.Default.Network) return;
            StateStore.Singleton.UpdateLocalStateRootSnapshot(block.Index,
                snapshot.GetChangeSet()
                    .Where(p => p.Value.State != TrackState.None && p.Key.Id != NativeContract.Ledger.Id)
                    .ToList());
        }

        void ICommittedHandler.Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (system.Settings.Network != StateRootSettings.Default.Network) return;
            StateStore.Singleton.UpdateLocalStateRoot(block.Index);
        }

        private void CheckNetwork()
        {
            var network = StateRootSettings.Default.Network;
            if (_system is null || _system.Settings.Network != network)
                throw new InvalidOperationException($"Network doesn't match: {_system?.Settings.Network} != {network}");
        }

        [ConsoleCommand("start states", Category = "StateRoot", Description = "Start as a state verifier if wallet is open")]
        private void OnStartVerifyingState()
        {
            CheckNetwork();
            Start(walletProvider.GetWallet());
        }

        public void Start(Wallet wallet)
        {
            if (Verifier is not null)
            {
                ConsoleHelper.Warning("Already started!");
                return;
            }
            if (wallet is null)
            {
                ConsoleHelper.Warning("Please open wallet first!");
                return;
            }
            Verifier = _system.ActorSystem.ActorOf(VerificationService.Props(wallet));
        }

        [ConsoleCommand("state root", Category = "StateRoot", Description = "Get state root by index")]
        private void OnGetStateRoot(uint index)
        {
            CheckNetwork();

            using var snapshot = StateStore.Singleton.GetSnapshot();
            var stateRoot = snapshot.GetStateRoot(index);
            if (stateRoot is null)
                ConsoleHelper.Warning("Unknown state root");
            else
                ConsoleHelper.Info(stateRoot.ToJson().ToString());
        }

        [ConsoleCommand("state height", Category = "StateRoot", Description = "Get current state root index")]
        private void OnGetStateHeight()
        {
            CheckNetwork();

            ConsoleHelper.Info("LocalRootIndex: ",
                $"{StateStore.Singleton.LocalRootIndex}",
                " ValidatedRootIndex: ",
                $"{StateStore.Singleton.ValidatedRootIndex}");
        }
    }
}
