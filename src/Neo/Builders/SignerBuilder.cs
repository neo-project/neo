// Copyright (C) 2015-2025 The Neo Project.
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
using System.Collections.Generic;

namespace Neo.Builders
{
    public sealed class SignerBuilder
    {
        private readonly UInt160 _account;
        private WitnessScope _scopes = WitnessScope.None;
        private readonly List<UInt160> _allowedContracts = [];
        private readonly List<ECPoint> _allowedGroups = [];
        private readonly List<WitnessRule> _rules = [];

        private SignerBuilder(UInt160 account)
        {
            _account = account;
        }

        public static SignerBuilder Create(UInt160 account)
        {
            return new SignerBuilder(account);
        }

        public SignerBuilder AllowContract(UInt160 contractHash)
        {
            _allowedContracts.Add(contractHash);
            return AddWitnessScope(WitnessScope.CustomContracts);
        }

        public SignerBuilder AllowGroup(ECPoint publicKey)
        {
            _allowedGroups.Add(publicKey);
            return AddWitnessScope(WitnessScope.CustomGroups);
        }

        public SignerBuilder AddWitnessScope(WitnessScope scope)
        {
            _scopes |= scope;
            return this;
        }

        public SignerBuilder AddWitnessRule(WitnessRuleAction action, Action<WitnessRuleBuilder> config)
        {
            var rb = WitnessRuleBuilder.Create(action);
            config(rb);
            _rules.Add(rb.Build());
            return AddWitnessScope(WitnessScope.WitnessRules);
        }

        public Signer Build()
        {
            return new()
            {
                Account = _account,
                Scopes = _scopes,
                AllowedContracts = _scopes.HasFlag(WitnessScope.CustomContracts) ? _allowedContracts.ToArray() : null,
                AllowedGroups = _scopes.HasFlag(WitnessScope.CustomGroups) ? _allowedGroups.ToArray() : null,
                Rules = _scopes.HasFlag(WitnessScope.WitnessRules) ? _rules.ToArray() : null
            };
        }
    }
}
