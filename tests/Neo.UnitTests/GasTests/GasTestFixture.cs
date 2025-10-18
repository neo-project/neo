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

using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Numerics;

#nullable enable

namespace Neo.UnitTests.GasTests
{
    public class GasTestFixture
    {
        public class SignatureData
        {
            public bool SignedByCommittee { get; set; } = false;
        }

        public class PolicyValues
        {
            public BigInteger ExecutionFee { get; set; } = PolicyContract.DefaultExecFeeFactor * ApplicationEngine.FeeFactor;
            public BigInteger StorageFee { get; set; } = PolicyContract.DefaultStoragePrice * ApplicationEngine.FeeFactor;
            public BigInteger FeePerByte { get; set; } = PolicyContract.DefaultFeePerByte * ApplicationEngine.FeeFactor;
        }

        public class EnvironmentState
        {
            public PolicyValues? Policy { get; set; }
            public Dictionary<string, string>? Storage { get; set; }
        }

        public class NeoExecution
        {
            public byte[] Script { get; set; } = [];
            public BigInteger Fee { get; set; } = BigInteger.Zero;

            [JsonConverter(typeof(StringEnumConverter))]
            public VMState State { get; set; } = VMState.HALT;
        }

        public string? Name { get; set; }
        public SignatureData? Signature { get; set; } = null;
        public EnvironmentState? Environment { get; set; } = null;
        public List<NeoExecution> Execute { get; set; } = [];
    }
}

#nullable disable
