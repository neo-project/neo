// Copyright (C) 2015-2025 The Neo Project.
//
// UT_HeaderCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_HeaderCache
    {
        [TestMethod]
        public void TestHeaderCache()
        {
            var cache = new HeaderCache();
            var header = new Header
            {
                Index = 1
            };
            cache.Add(header);

            var got = cache[1];
            got.Should().NotBeNull();
            got.Index.Should().Be(1);

            var count = cache.Count;
            count.Should().Be(1);

            var full = cache.Full;
            full.Should().BeFalse();

            var last = cache.Last;
            last.Should().NotBeNull();
            last.Index.Should().Be(1);

            got = cache[2];
            got.Should().BeNull();

            // enumerate
            var enumerator = cache.GetEnumerator();
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Index.Should().Be(1);
            enumerator.MoveNext().Should().BeFalse();

            var removed = cache.TryRemoveFirst(out header);
            removed.Should().BeTrue();

            count = cache.Count;
            count.Should().Be(0);

            full = cache.Full;
            full.Should().BeFalse();

            last = cache.Last;
            last.Should().BeNull();

            got = cache[1];
            got.Should().BeNull();
        }
    }
}
