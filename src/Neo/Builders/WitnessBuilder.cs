// Copyright (C) 2015-2024 The Neo Project.
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

        public WitnessBuilder AddInvocation(Action<ScriptBuilder> scriptBuilder)
        {
            var sb = new ScriptBuilder();
            scriptBuilder(sb);
            _invocationScript = sb.ToArray();
            return this;
        }

        public WitnessBuilder AddInvocation(byte[] bytes)
        {
            _invocationScript = bytes;
            return this;
        }

        public WitnessBuilder AddVerification(Action<ScriptBuilder> scriptBuilder)
        {
            var sb = new ScriptBuilder();
            scriptBuilder(sb);
            _verificationScript = sb.ToArray();
            return this;
        }

        public WitnessBuilder AddVerification(byte[] bytes)
        {
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
