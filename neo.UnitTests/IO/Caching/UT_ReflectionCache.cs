using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;

namespace Neo.UnitTests.IO.Caching
{
    public class TestItem { }

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
        ReflectionCache<byte> reflectionCache;

        [TestInitialize]
        public void SetUp()
        {
            reflectionCache = ReflectionCache<byte>.CreateFromEnum<MyTestEnum>();
        }

        [TestMethod]
        public void TestCreateFromEnum()
        {
            reflectionCache.Should().NotBeNull();
        }

        [TestMethod]
        public void TestCreateFromObjectNotEnum()
        {
            Action action = () => ReflectionCache<byte>.CreateFromEnum<int>();
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestCreateFromEmptyEnum()
        {
            reflectionCache = ReflectionCache<byte>.CreateFromEnum<MyEmptyEnum>();
            reflectionCache.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestCreateInstance()
        {
            object item1 = reflectionCache.CreateInstance((byte)MyTestEnum.Item1, null);
            (item1 is TestItem1).Should().BeTrue();

            object item2 = reflectionCache.CreateInstance((byte)MyTestEnum.Item2, null);
            (item2 is TestItem2).Should().BeTrue();

            object item3 = reflectionCache.CreateInstance(0x02, null);
            item3.Should().BeNull();
        }

        [TestMethod]
        public void TestCreateInstance2()
        {
            TestItem defaultItem = new TestItem1();
            object item2 = reflectionCache.CreateInstance((byte)MyTestEnum.Item2, defaultItem);
            (item2 is TestItem2).Should().BeTrue();

            object item1 = reflectionCache.CreateInstance(0x02, new TestItem1());
            (item1 is TestItem1).Should().BeTrue();
        }
    }
}
