// Copyright (C) 2015-2025 The Neo Project.
//
// GasTestFixture.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System.Collections.Generic;
using System.Numerics;

#nullable enable

namespace Neo.UnitTests.GasTests
{
    public class GasTestFixture
    {
        public class SignatureData
        {
            public bool SignedByCommitee { get; set; } = false;
        }

        public class PreExecutionData
        {
            public Dictionary<string, string> Storage { get; set; } = [];

        }

        public class NeoExecution
        {
            public byte[] Script { get; set; } = [];
            public BigInteger Fee { get; set; } = BigInteger.Zero;
            public VMState State { get; set; } = VMState.HALT;
        }

        public SignatureData? Signature { get; set; } = null;
        public PreExecutionData? PreExecution { get; set; } = null;
        public List<NeoExecution> Execute { get; set; } = [];
    }
}

#nullable disable
