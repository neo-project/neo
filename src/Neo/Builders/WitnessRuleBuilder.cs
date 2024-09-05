// Copyright (C) 2015-2024 The Neo Project.
//
// WitnessRuleBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;

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

        public WitnessRuleBuilder AddCondition(UInt160 contractHash)
        {
            return this;
        }
    }
}
