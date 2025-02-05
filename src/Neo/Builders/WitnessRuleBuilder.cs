// Copyright (C) 2015-2025 The Neo Project.
//
// WitnessRuleBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Builders
{
    public sealed class WitnessRuleBuilder
    {
        private readonly WitnessRule _rule = new();

        private WitnessRuleBuilder(WitnessRuleAction action)
        {
            _rule.Action = action;
        }

        public static WitnessRuleBuilder Create(WitnessRuleAction action)
        {
            return new WitnessRuleBuilder(action);
        }

        public WitnessRuleBuilder AddCondition(Action<WitnessConditionBuilder> config)
        {
            var cb = WitnessConditionBuilder.Create();
            config(cb);
            _rule.Condition = cb.Build();
            return this;
        }

        public WitnessRule Build()
        {
            return _rule;
        }
    }
}
