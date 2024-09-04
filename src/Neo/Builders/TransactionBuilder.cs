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

        private byte _version = 0;
        private uint _nonce = (uint)new Random().Next();
        private uint _systemFee = 0;
        private uint _networkFee = 0;
        private uint _validUntilBlock = 0;
        private byte[] _script = [];
        private TransactionAttribute[] _attributes = [];
        private Signer[] _signers = [];
        private Witness[] _witnesses = [];

        private TransactionBuilder() { }

        public static TransactionBuilder CreateEmpty()
        {
            return new TransactionBuilder();
        }

        public TransactionBuilder Version(byte version)
        {
            _version = version;
            return this;
        }

        public TransactionBuilder Nonce(uint nonce)
        {
            _nonce = nonce;
            return this;
        }

        public TransactionBuilder SystemFee(uint systemFee)
        {
            _systemFee = systemFee;
            return this;
        }

        public TransactionBuilder NetworkFee(uint networkFee)
        {
            _networkFee = networkFee;
            return this;
        }

        public TransactionBuilder ValidUntil(uint blockIndex)
        {
            _validUntilBlock = blockIndex;
            return this;
        }

        public TransactionBuilder AttachSystem(Action<ScriptBuilder> scriptBuilder)
        {
            var sb = new ScriptBuilder();
            scriptBuilder(sb);
            _script = sb.ToArray();
            return this;
        }

        public TransactionBuilder AddAttributes(Action<TransactionAttributesBuilder> transactionAttributeBuilder)
        {
            var ab = TransactionAttributesBuilder.CreateEmpty();
            transactionAttributeBuilder(ab);
            _attributes = ab.Build();
            return this;
        }

        public TransactionBuilder AddWitness(Action<WitnessBuilder> witnessBuilder)
        {
            var wb = WitnessBuilder.CreateEmpty();
            witnessBuilder(wb);
            _witnesses = [.. _witnesses, wb.Build()];
            return this;
        }

        public Transaction Build()
        {
            return new Transaction()
            {
                Version = _version,
                Nonce = _nonce,
                SystemFee = _systemFee,
                NetworkFee = _networkFee,
                ValidUntilBlock = _validUntilBlock,
                Script = _script,
                Attributes = _attributes,
                Signers = _signers,
                Witnesses = _witnesses
            };
        }
    }
}
