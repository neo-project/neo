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

namespace Neo.Builders
{
    public sealed class WitnessConditionBuilder
    {
        WitnessCondition _condition;

        private WitnessConditionBuilder() { }

        public static WitnessConditionBuilder Create()
        {
            return new WitnessConditionBuilder();
        }

        public WitnessConditionBuilder And()
        {
            var condition = new AndCondition() { Expressions = [] };

            if (_condition is NotCondition notCondition)
                notCondition.Expression = condition;
            else
                _condition = condition;

            return this;
        }

        public WitnessConditionBuilder Boolean(bool expression)
        {
            var condition = new BooleanCondition() { Expression = expression };

            SetConditionWithOtherConditions(condition);

            return this;
        }

        public WitnessConditionBuilder CalledByContract(UInt160 hash)
        {
            var condition = new CalledByContractCondition() { Hash = hash };

            SetConditionWithOtherConditions(condition);

            return this;
        }

        public WitnessConditionBuilder CalledByEntry()
        {
            var condition = new CalledByEntryCondition();

            SetConditionWithOtherConditions(condition);

            return this;
        }

        public WitnessConditionBuilder CalledByGroup(ECPoint publicKey)
        {
            var condition = new CalledByGroupCondition() { Group = publicKey };

            SetConditionWithOtherConditions(condition);

            return this;
        }

        public WitnessConditionBuilder Group(ECPoint publicKey)
        {
            var condition = new GroupCondition() { Group = publicKey };

            SetConditionWithOtherConditions(condition);

            return this;
        }

        public WitnessConditionBuilder Not()
        {
            var condition = new NotCondition();

            if (_condition is AndCondition andCondition)
                andCondition.Expressions = [.. andCondition.Expressions, condition];
            else
                _condition = condition;

            return this;
        }

        public WitnessConditionBuilder Or()
        {
            var condition = new OrCondition() { Expressions = [] };

            if (_condition is NotCondition notCondition)
                notCondition.Expression = condition;
            else
                _condition = condition;

            return this;
        }

        public WitnessConditionBuilder ScriptHash(UInt160 scriptHash)
        {
            var condition = new ScriptHashCondition() { Hash = scriptHash };

            SetConditionWithOtherConditions(condition);

            return this;
        }

        public WitnessCondition Build()
        {
            return _condition;
        }

        private void SetConditionWithOtherConditions(WitnessCondition condition)
        {
            if (_condition is AndCondition andCondition)
                andCondition.Expressions = [.. andCondition.Expressions, condition];
            else if (_condition is OrCondition orCondition)
                orCondition.Expressions = [.. orCondition.Expressions, condition];
            else if (_condition is NotCondition notCondition)
            {
                if (notCondition.Expression is AndCondition a)
                    a.Expressions = [.. a.Expressions, condition];
                else if (notCondition.Expression is OrCondition o)
                    o.Expressions = [.. o.Expressions, condition];
            }
            else
                _condition = condition;
        }
    }
}
