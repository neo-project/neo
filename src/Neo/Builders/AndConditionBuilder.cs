// Copyright (C) 2015-2025 The Neo Project.
//
// AndConditionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads.Conditions;
using System;

namespace Neo.Builders
{
    public sealed class AndConditionBuilder
    {
        private readonly AndCondition _condition = new() { Expressions = [] };

        private AndConditionBuilder() { }

        public static AndConditionBuilder CreateEmpty()
        {
            return new AndConditionBuilder();
        }

        public AndConditionBuilder And(Action<AndConditionBuilder> config)
        {
            var acb = new AndConditionBuilder();
            config(acb);

            _condition.Expressions = [.. _condition.Expressions, acb.Build()];

            return this;
        }

        public AndConditionBuilder Or(Action<OrConditionBuilder> config)
        {
            var ocb = OrConditionBuilder.CreateEmpty();
            config(ocb);

            _condition.Expressions = [.. _condition.Expressions, ocb.Build()];

            return this;
        }

        public AndConditionBuilder Boolean(bool expression)
        {
            _condition.Expressions = [.. _condition.Expressions, new BooleanCondition { Expression = expression }];
            return this;
        }

        public AndConditionBuilder CalledByContract(UInt160 hash)
        {
            _condition.Expressions = [.. _condition.Expressions, new CalledByContractCondition { Hash = hash }];
            return this;
        }

        public AndConditionBuilder CalledByEntry()
        {
            _condition.Expressions = [.. _condition.Expressions, new CalledByEntryCondition()];
            return this;
        }

        public AndConditionBuilder CalledByGroup(ECPoint publicKey)
        {
            _condition.Expressions = [.. _condition.Expressions, new CalledByGroupCondition { Group = publicKey }];
            return this;
        }

        public AndConditionBuilder Group(ECPoint publicKey)
        {
            _condition.Expressions = [.. _condition.Expressions, new GroupCondition() { Group = publicKey }];
            return this;
        }

        public AndConditionBuilder ScriptHash(UInt160 scriptHash)
        {
            _condition.Expressions = [.. _condition.Expressions, new ScriptHashCondition() { Hash = scriptHash }];
            return this;
        }

        public AndCondition Build()
        {
            return _condition;
        }
    }
}
