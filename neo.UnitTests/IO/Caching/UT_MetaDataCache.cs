using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Caching;
using System;

namespace Neo.UnitTests.IO.Caching
{
    public class MyMetaCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        public T Value;

        public MyMetaCache(Func<T> factory) : base(factory) { }

        protected override void AddInternal(T item)
        {
            Value = item;
        }

        protected override T TryGetInternal()
        {
            return Value;
        }

        protected override void UpdateInternal(T item)
        {
            Value = item;
        }
    }

    [TestClass]
    public class UT_MetaDataCache
    {
        [TestMethod]
        public void TestContructor()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            myMetaCache.Should().NotBeNull();
        }

        [TestMethod]
        public void TestCommitAndAddInternal()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());

            MyValue value = myMetaCache.Get();
            value.Should().NotBeNull();
            value.Value.Should().BeNull();

            myMetaCache.Commit();
            myMetaCache.Value.Should().Be(value);
        }

        public void TestCommitAndUpdateInternal()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            MyValue value = myMetaCache.GetAndChange();
            value.Value = "value1";

            myMetaCache.Commit();
            myMetaCache.Value.Should().Be(value);
            myMetaCache.Value.Value.Should().Be("value1");
        }

        [TestMethod]
        public void TestCreateSnapshot()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            myMetaCache.CreateSnapshot().Should().NotBeNull();
        }
    }
}
