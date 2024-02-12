// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ReflectionCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Caching;
using System.IO;

namespace Neo.UnitTests.IO.Caching
{
    public class TestItem : ISerializable
    {
        public int Size => 0;
        public void Deserialize(ref MemoryReader reader) { }
        public void Serialize(BinaryWriter writer) { }
    }

    public class TestItem1 : TestItem { }

    public class TestItem2 : TestItem { }

    public enum MyTestEnum : byte
    {
        [ReflectionCache(typeof(TestItem1))]
        Item1 = 0x00,

        [ReflectionCache(typeof(TestItem2))]
        Item2 = 0x01,
    }

    public enum MyEmptyEnum : byte { }

    [TestClass]
    public class UT_ReflectionCache
    {
        [TestMethod]
        public void TestCreateFromEmptyEnum()
        {
            ReflectionCache<MyEmptyEnum>.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestCreateInstance()
        {
            object item1 = ReflectionCache<MyTestEnum>.CreateInstance(MyTestEnum.Item1, null);
            (item1 is TestItem1).Should().BeTrue();

            object item2 = ReflectionCache<MyTestEnum>.CreateInstance(MyTestEnum.Item2, null);
            (item2 is TestItem2).Should().BeTrue();

            object item3 = ReflectionCache<MyTestEnum>.CreateInstance((MyTestEnum)0x02, null);
            item3.Should().BeNull();
        }

        [TestMethod]
        public void TestCreateSerializable()
        {
            object item1 = ReflectionCache<MyTestEnum>.CreateSerializable(MyTestEnum.Item1, new byte[0]);
            (item1 is TestItem1).Should().BeTrue();

            object item2 = ReflectionCache<MyTestEnum>.CreateSerializable(MyTestEnum.Item2, new byte[0]);
            (item2 is TestItem2).Should().BeTrue();

            object item3 = ReflectionCache<MyTestEnum>.CreateSerializable((MyTestEnum)0x02, new byte[0]);
            item3.Should().BeNull();
        }

        [TestMethod]
        public void TestCreateInstance2()
        {
            TestItem defaultItem = new TestItem1();
            object item2 = ReflectionCache<MyTestEnum>.CreateInstance(MyTestEnum.Item2, defaultItem);
            (item2 is TestItem2).Should().BeTrue();

            object item1 = ReflectionCache<MyTestEnum>.CreateInstance((MyTestEnum)0x02, new TestItem1());
            (item1 is TestItem1).Should().BeTrue();
        }
    }
}
