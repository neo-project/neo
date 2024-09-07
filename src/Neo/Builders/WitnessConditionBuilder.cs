// Copyright (C) 2015-2024 The Neo Project.
//
// WitnessConditionBuilder.cs file belongs to the neo project and is free
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
    public sealed class WitnessConditionBuilder
    {
        WitnessCondition _condition;

        private WitnessConditionBuilder() { }

        private WitnessConditionBuilder(WitnessCondition condition)
        {
            _condition = condition;
        }

        public static WitnessConditionBuilder Create()
        {
            return new WitnessConditionBuilder();
        }

        public WitnessConditionBuilder And(Action<AndConditionBuilder> config)
        {
            var acb = AndConditionBuilder.CreateEmpty();
            config(acb);

            _condition = acb.Build();

            return this;
        }

        public WitnessConditionBuilder Boolean(bool expression)
        {
            var condition = new BooleanCondition() { Expression = expression };

            _condition = condition;

            return this;
        }

        public WitnessConditionBuilder CalledByContract(UInt160 hash)
        {
            var condition = new CalledByContractCondition() { Hash = hash };

            _condition = condition;

            return this;
        }

        public WitnessConditionBuilder CalledByEntry()
        {
            var condition = new CalledByEntryCondition();

            _condition = condition;

            return this;
        }

        public WitnessConditionBuilder CalledByGroup(ECPoint publicKey)
        {
            var condition = new CalledByGroupCondition() { Group = publicKey };

            _condition = condition;

            return this;
        }

        public WitnessConditionBuilder Group(ECPoint publicKey)
        {
            var condition = new GroupCondition() { Group = publicKey };

            _condition = condition;

            return this;
        }

        public WitnessConditionBuilder Not(Action<WitnessConditionBuilder> config)
        {
            var wcb = new WitnessConditionBuilder();
            config(wcb);

            var condition = new NotCondition()
            {
                Expression = wcb.Build()
            };

            _condition = condition;

            return this;
        }

        public WitnessConditionBuilder Or(Action<OrConditionBuilder> config)
        {
            var ocb = OrConditionBuilder.CreateEmpty();
            config(ocb);

            _condition = ocb.Build();

            return this;
        }

        public WitnessConditionBuilder ScriptHash(UInt160 scriptHash)
        {
            var condition = new ScriptHashCondition() { Hash = scriptHash };

            _condition = condition;

            return this;
        }

        public WitnessCondition Build()
        {
            if (_condition is null)
                return new BooleanCondition() { Expression = true };

            return _condition;
        }
    }
}
