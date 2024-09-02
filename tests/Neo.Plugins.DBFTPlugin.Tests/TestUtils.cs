
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
using Neo.Ledger;
using Neo.Persistence;
using Neo.UnitTests;
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


