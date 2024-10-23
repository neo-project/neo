// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RandomExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using System;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_RandomExtensions
    {
        [TestMethod]
        public void TestNextBigIntegerForRandom()
        {
            Random ran = new();
            Action action1 = () => ran.NextBigInteger(-1);
            action1.Should().Throw<ArgumentException>();

            ran.NextBigInteger(0).Should().Be(0);
            ran.NextBigInteger(8).Should().NotBeNull();
            ran.NextBigInteger(9).Should().NotBeNull();
        }
    }
}
