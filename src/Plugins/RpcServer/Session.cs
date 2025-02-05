// Copyright (C) 2015-2025 The Neo Project.
//
// Session.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.RpcServer
{
    class Session : IDisposable
    {
        public readonly SnapshotCache Snapshot;
        public readonly ApplicationEngine Engine;
        public readonly Dictionary<Guid, IIterator> Iterators = new();
        public DateTime StartTime;

        public Session(NeoSystem system, byte[] script, Signer[] signers, Witness[] witnesses, long datoshi, Diagnostic diagnostic)
        {
            Random random = new();
            Snapshot = system.GetSnapshotCache();
            Transaction tx = signers == null ? null : new Transaction
            {
                Version = 0,
                Nonce = (uint)random.Next(),
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(Snapshot) + system.Settings.MaxValidUntilBlockIncrement,
                Signers = signers,
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = script,
                Witnesses = witnesses
            };
            Engine = ApplicationEngine.Run(script, Snapshot, container: tx, settings: system.Settings, gas: datoshi, diagnostic: diagnostic);
            ResetExpiration();
        }

        public void ResetExpiration()
        {
            StartTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            Engine.Dispose();
            Snapshot.Dispose();
        }
    }
}
