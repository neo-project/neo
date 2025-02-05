// Copyright (C) 2015-2025 The Neo Project.
//
// WitnessBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.VM;
using System;

namespace Neo.Builders
{
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
                throw new InvalidOperationException("Invocation script already exists.");

            using var sb = new ScriptBuilder();
            config(sb);
            _invocationScript = sb.ToArray();
            return this;
        }

        public WitnessBuilder AddInvocation(byte[] bytes)
        {
            if (_invocationScript.Length > 0)
                throw new InvalidOperationException("Invocation script already exists.");

            _invocationScript = bytes;
            return this;
        }

        public WitnessBuilder AddVerification(Action<ScriptBuilder> config)
        {
            if (_verificationScript.Length > 0)
                throw new InvalidOperationException("Verification script already exists.");

            using var sb = new ScriptBuilder();
            config(sb);
            _verificationScript = sb.ToArray();
            return this;
        }

        public WitnessBuilder AddVerification(byte[] bytes)
        {
            if (_verificationScript.Length > 0)
                throw new InvalidOperationException("Verification script already exists.");

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
}
