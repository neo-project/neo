// Copyright (C) 2015-2025 The Neo Project.
//
// TokenBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Builders;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System.Numerics;

namespace Neo.Build.Core.Tokens
{
    public abstract class TokenBase
    {
        public UInt160 ScriptHash { get; }
        public string Symbol { get; }
        public byte Decimals { get; }

        private readonly ProtocolSettings _protocolSettings;
        private readonly DataCache _snapshot;

        protected TokenBase(
            ProtocolSettings protocolSettings,
            DataCache snapshot,
            UInt160 scriptHash)
        {
            _protocolSettings = protocolSettings;
            _snapshot = snapshot;
            ScriptHash = scriptHash;

            var tb = TransactionBuilder
                .CreateEmpty()
                .AttachSystem(sb =>
                {
                    sb.EmitDynamicCall(ScriptHash, TokenMethodNames.Base.Decimals);
                    sb.EmitDynamicCall(ScriptHash, TokenMethodNames.Base.Symbol);
                });

            using var app = InvokeTransaction(tb.Build());

            var results = app.ResultStack;
            Symbol = results.Pop().GetString()!;
            Decimals = checked((byte)results.Pop().GetInteger());
        }

        public BigInteger TotalSupply()
        {
            var tb = TransactionBuilder
                .CreateEmpty()
                .AttachSystem(sb =>
                {
                    sb.EmitDynamicCall(ScriptHash, TokenMethodNames.Base.TotalSupply);
                });

            using var app = InvokeTransaction(tb.Build());

            return app.ResultStack.Pop().GetInteger();
        }

        public BigInteger BalanceOf(UInt160 accountHash)
        {
            var tb = TransactionBuilder
                .CreateEmpty()
                .AttachSystem(sb =>
                {
                    sb.EmitDynamicCall(ScriptHash, TokenMethodNames.Base.BalanceOf, accountHash);
                });

            using var app = InvokeTransaction(tb.Build());

            return app.ResultStack.Pop().GetInteger();
        }

        private ApplicationEngine InvokeTransaction(Transaction tx)
        {
            var app = ApplicationEngine.Run(tx.Script, _snapshot, settings: _protocolSettings);

            if (app.State != VMState.HALT)
                // TODO: make custom VM exception class
                throw new NeoBuildException(app.FaultException?.InnerException ?? app.FaultException!);

            return app;
        }
    }
}
