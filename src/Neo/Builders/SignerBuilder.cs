// Copyright (C) 2015-2024 The Neo Project.
//
// SignerBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Builders
{
    public sealed class SignerBuilder
    {
        private readonly Signer _signer = new Signer()
        {
            Account = UInt160.Zero,
            AllowedContracts = [],
            AllowedGroups = [],
            Rules = [],
            Scopes = WitnessScope.None,
        };

        private SignerBuilder() { }

        public static SignerBuilder CreateEmpty()
        {
            return new SignerBuilder();
        }

        public SignerBuilder Account(UInt160 scriptHash)
        {
            _signer.Account = scriptHash;
            return this;
        }

        public SignerBuilder AllowContract(UInt160 contractHash)
        {
            _signer.AllowedContracts = [.. _signer.AllowedContracts, contractHash];
            return this;
        }

        public SignerBuilder AllowGroup(ECPoint publicKey)
        {
            _signer.AllowedGroups = [.. _signer.AllowedGroups, publicKey];
            return this;
        }

        public SignerBuilder AddWitnessScope(WitnessScope scope)
        {
            _signer.Scopes |= scope;
            return this;
        }

        public SignerBuilder AddWitnessRule(WitnessRuleAction action, Action<WitnessRuleBuilder> config)
        {
            var rb = WitnessRuleBuilder.Create(action);
            config(rb);
            _signer.Rules = [.. _signer.Rules, rb.Build()];
            return this;
        }

        public Signer Build()
        {
            return _signer;
        }
    }
}
