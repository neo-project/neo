
// Copyright (C) 2015-2024 The Neo Project.
//
// TestBlockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.UnitTests;
using Neo.VM;
using System;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    public class TestNeoSystem : NeoSystem
    {
        public TestNeoSystem() : base(ProtocolSettings.Default)
        {
        }
    }

    public class TestTimeProvider
    {

        private static readonly TestTimeProvider Default = new TestTimeProvider();

        /// <summary>
        /// The currently used <see cref="T:Neo.TimeProvider" /> instance.
        /// </summary>
        public static TestTimeProvider Current { get; internal set; } = TestTimeProvider.Default;

        /// <summary>
        /// Gets the current time expressed as the Coordinated Universal Time (UTC).
        /// </summary>
        public virtual DateTime UtcNow => DateTime.UtcNow;

        internal static void ResetToDefault() => TestTimeProvider.Current = TestTimeProvider.Default;
    }


}

public static class TestUtilsConsensus
{

    public static void SetupHeaderWithValues(Header header, UInt256 val256, out UInt256 merkRootVal, out UInt160 val160, out ulong timestampVal, out ulong nonceVal, out uint indexVal, out Witness scriptVal)
    {
        header.PrevHash = val256;
        header.MerkleRoot = merkRootVal = UInt256.Parse("0x6226416a0e5aca42b5566f5a19ab467692688ba9d47986f6981a7f747bba2772");
        header.Timestamp = timestampVal = new DateTime(1980, 06, 01, 0, 0, 1, 001, DateTimeKind.Utc).ToTimestampMS(); // GMT: Sunday, June 1, 1980 12:00:01.001 AM
        header.Index = indexVal = 0;
        header.Nonce = nonceVal = 0;
        header.NextConsensus = val160 = UInt160.Zero;
        header.Witness = scriptVal = new Witness
        {
            InvocationScript = Array.Empty<byte>(),
            VerificationScript = new[] { (byte)OpCode.PUSH1 }
        };
    }
}
