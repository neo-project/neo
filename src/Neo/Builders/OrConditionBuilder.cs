// Copyright (C) 2015-2025 The Neo Project.
//
// OrConditionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads.Conditions;
using System;

namespace Neo.Builders
{
    public sealed class OrConditionBuilder
    {
        private readonly OrCondition _condition = new() { Expressions = [] };

        private OrConditionBuilder() { }

        public static OrConditionBuilder CreateEmpty()
        {
            return new OrConditionBuilder();
        }

        public OrConditionBuilder And(Action<AndConditionBuilder> config)
        {
            var acb = AndConditionBuilder.CreateEmpty();
            config(acb);

            _condition.Expressions = [.. _condition.Expressions, acb.Build()];

            return this;
        }

        public OrConditionBuilder Or(Action<OrConditionBuilder> config)
        {
            var acb = new OrConditionBuilder();
            config(acb);

            _condition.Expressions = [.. _condition.Expressions, acb.Build()];

            return this;
        }

        public OrConditionBuilder Boolean(bool expression)
        {
            _condition.Expressions = [.. _condition.Expressions, new BooleanCondition { Expression = expression }];
            return this;
        }

        public OrConditionBuilder CalledByContract(UInt160 hash)
        {
            _condition.Expressions = [.. _condition.Expressions, new CalledByContractCondition { Hash = hash }];
            return this;
        }

        public OrConditionBuilder CalledByEntry()
        {
            _condition.Expressions = [.. _condition.Expressions, new CalledByEntryCondition()];
            return this;
        }

        public OrConditionBuilder CalledByGroup(ECPoint publicKey)
        {
            _condition.Expressions = [.. _condition.Expressions, new CalledByGroupCondition { Group = publicKey }];
            return this;
        }

        public OrConditionBuilder Group(ECPoint publicKey)
        {
            _condition.Expressions = [.. _condition.Expressions, new GroupCondition() { Group = publicKey }];
            return this;
        }

        public OrConditionBuilder ScriptHash(UInt160 scriptHash)
        {
            _condition.Expressions = [.. _condition.Expressions, new ScriptHashCondition() { Hash = scriptHash }];
            return this;
        }

        public OrCondition Build()
        {
            return _condition;
        }
    }
}
