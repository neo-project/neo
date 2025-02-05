// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ReflectionCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            Assert.AreEqual(0, ReflectionCache<MyEmptyEnum>.Count);
        }

        [TestMethod]
        public void TestCreateInstance()
        {
            object item1 = ReflectionCache<MyTestEnum>.CreateInstance(MyTestEnum.Item1, null);
            Assert.IsTrue(item1 is TestItem1);

            object item2 = ReflectionCache<MyTestEnum>.CreateInstance(MyTestEnum.Item2, null);
            Assert.IsTrue(item2 is TestItem2);

            object item3 = ReflectionCache<MyTestEnum>.CreateInstance((MyTestEnum)0x02, null);
            Assert.IsNull(item3);
        }

        [TestMethod]
        public void TestCreateSerializable()
        {
            object item1 = ReflectionCache<MyTestEnum>.CreateSerializable(MyTestEnum.Item1, new byte[0]);
            Assert.IsTrue(item1 is TestItem1);

            object item2 = ReflectionCache<MyTestEnum>.CreateSerializable(MyTestEnum.Item2, new byte[0]);
            Assert.IsTrue(item2 is TestItem2);

            object item3 = ReflectionCache<MyTestEnum>.CreateSerializable((MyTestEnum)0x02, new byte[0]);
            Assert.IsNull(item3);
        }

        [TestMethod]
        public void TestCreateInstance2()
        {
            TestItem defaultItem = new TestItem1();
            object item2 = ReflectionCache<MyTestEnum>.CreateInstance(MyTestEnum.Item2, defaultItem);
            Assert.IsTrue(item2 is TestItem2);

            object item1 = ReflectionCache<MyTestEnum>.CreateInstance((MyTestEnum)0x02, new TestItem1());
            Assert.IsTrue(item1 is TestItem1);
        }
    }
}
