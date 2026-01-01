// Copyright (C) 2015-2026 The Neo Project.
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
using Neo.Network.P2P.Payloads.Conditions;

namespace Neo.Builders;

public sealed class WitnessRuleBuilder
{
    private readonly WitnessRuleAction _action;
    private WitnessCondition? _condition;

    private WitnessRuleBuilder(WitnessRuleAction action)
    {
        _action = action;
    }

    public static WitnessRuleBuilder Create(WitnessRuleAction action)
    {
        return new WitnessRuleBuilder(action);
    }

    public WitnessRuleBuilder AddCondition(Action<WitnessConditionBuilder> config)
    {
        var cb = WitnessConditionBuilder.Create();
        config(cb);
        _condition = cb.Build();
        return this;
    }

    public WitnessRule Build()
    {
        return new()
        {
            Action = _action,
            Condition = _condition ?? throw new InvalidOperationException("Condition is not set."),
        };
    }
}
