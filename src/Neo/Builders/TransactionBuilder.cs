// Copyright (C) 2015-2024 The Neo Project.
//
// TransactionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.VM;
using System;

namespace Neo.Builders
{
    public sealed class TransactionBuilder
    {

        private readonly Transaction _tx = new()
        {
            Script = new[] { (byte)OpCode.RET },
            Attributes = [],
            Signers = [],
            Witnesses = [],
        };

        private TransactionBuilder() { }

        public static TransactionBuilder CreateEmpty()
        {
            return new TransactionBuilder();
        }

        public TransactionBuilder Version(byte version)
        {
            _tx.Version = version;
            return this;
        }

        public TransactionBuilder Nonce(uint nonce)
        {
            _tx.Nonce = nonce;
            return this;
        }

        public TransactionBuilder SystemFee(uint systemFee)
        {
            _tx.SystemFee = systemFee;
            return this;
        }

        public TransactionBuilder NetworkFee(uint networkFee)
        {
            _tx.NetworkFee = networkFee;
            return this;
        }

        public TransactionBuilder ValidUntil(uint blockIndex)
        {
            _tx.ValidUntilBlock = blockIndex;
            return this;
        }

        public TransactionBuilder AttachSystem(Action<ScriptBuilder> action)
        {
            var sb = new ScriptBuilder();
            action(sb);
            _tx.Script = sb.ToArray();
            return this;
        }

        public TransactionBuilder AddAttributes(Action<TransactionAttributesBuilder> action)
        {
            var ab = TransactionAttributesBuilder.CreateEmpty();
            action(ab);
            _tx.Attributes = ab.Build();
            return this;
        }

        public TransactionBuilder AddWitness(Action<WitnessBuilder> action)
        {
            var wb = WitnessBuilder.CreateEmpty();
            action(wb);
            _tx.Witnesses = [.. _tx.Witnesses, wb.Build()];
            return this;
        }

        public TransactionBuilder AddWitness(Action<WitnessBuilder, Transaction> action)
        {
            var wb = WitnessBuilder.CreateEmpty();
            action(wb, _tx);
            _tx.Witnesses = [.. _tx.Witnesses, wb.Build()];
            return this;
        }

        public Transaction Build()
        {
            return _tx;
        }
    }
}
