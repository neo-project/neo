// Copyright (C) 2015-2025 The Neo Project.
//
// WitnessWrapper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System.ComponentModel;
using System.Drawing.Design;

namespace Neo.GUI.Wrappers
{
    internal class WitnessWrapper
    {
        [Editor(typeof(ScriptEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(HexConverter))]
        public byte[] InvocationScript { get; set; }
        [Editor(typeof(ScriptEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(HexConverter))]
        public byte[] VerificationScript { get; set; }

        public Witness Unwrap()
        {
            return new Witness
            {
                InvocationScript = InvocationScript,
                VerificationScript = VerificationScript
            };
        }
    }
}
