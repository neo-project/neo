// Copyright (C) 2015-2025 The Neo Project.
//
// WitnessBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.VM;

namespace Neo.Builders;

public sealed class WitnessBuilder
{
    private byte[] _invocationScript = [];
    private byte[] _verificationScript = [];

    private WitnessBuilder() { }

    public static WitnessBuilder CreateEmpty()
    {
        return new WitnessBuilder();
    }

    public WitnessBuilder AddInvocation(Action<ScriptBuilder> config)
    {
        if (_invocationScript.Length > 0)
            throw new InvalidOperationException("Invocation script already exists in the witness builder. Only one invocation script can be added per witness.");

        using var sb = new ScriptBuilder();
        config(sb);
        _invocationScript = sb.ToArray();
        return this;
    }

    public WitnessBuilder AddInvocation(byte[] bytes)
    {
        if (_invocationScript.Length > 0)
            throw new InvalidOperationException("Invocation script already exists in the witness builder. Only one invocation script can be added per witness.");

        _invocationScript = bytes;
        return this;
    }

    public WitnessBuilder AddVerification(Action<ScriptBuilder> config)
    {
        if (_verificationScript.Length > 0)
            throw new InvalidOperationException("Verification script already exists in the witness builder. Only one verification script can be added per witness.");

        using var sb = new ScriptBuilder();
        config(sb);
        _verificationScript = sb.ToArray();
        return this;
    }

    public WitnessBuilder AddVerification(byte[] bytes)
    {
        if (_verificationScript.Length > 0)
            throw new InvalidOperationException("Verification script already exists in the witness builder. Only one verification script can be added per witness.");

        _verificationScript = bytes;
        return this;
    }

    public Witness Build()
    {
        return new Witness()
        {
            InvocationScript = _invocationScript,
            VerificationScript = _verificationScript,
        };
    }
}
