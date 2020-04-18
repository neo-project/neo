using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
using System;
using System.Linq;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleExecutionCache
    {
        class CounterRequest : OracleHttpsRequest
        {
            public int Counter = 0;
        }

        private OracleResponse OracleLogic(OracleRequest arg)
        {
            var http = (CounterRequest)arg;
            http.Counter++;
            return OracleResponse.CreateResult(arg.Hash, BitConverter.GetBytes(http.Counter), 0);
        }

        [TestMethod]
        public void TestEnumerator()
        {
            var copy = UT_OracleResponse.CreateDefault();
            var entry = new OracleExecutionCache(UT_OracleResponse.CreateDefault());
            var entries = entry.ToArray();

            Assert.AreEqual(entries[0].Value.Hash, copy.Hash);
        }

        [TestMethod]
        public void TestSerialization()
        {
            var entry = new OracleExecutionCache(UT_OracleResponse.CreateDefault());
            var data = Neo.IO.Helper.ToArray(entry);

            Assert.AreEqual(entry.Size, data.Length);

            var copy = Neo.IO.Helper.AsSerializable<OracleExecutionCache>(data);

            Assert.AreEqual(entry.Count, copy.Count);
            Assert.AreEqual(entry.FilterCost, copy.FilterCost);
            Assert.AreEqual(entry.First().Value.Hash, copy.First().Value.Hash);
        }

        [TestMethod]
        public void TestWithOracle()
        {
            var cache = new OracleExecutionCache(OracleLogic);

            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.GetEnumerator().MoveNext());

            // Test without cache

            var req = new CounterRequest()
            {
                Counter = 1,
                URL = new Uri("https://google.es"),
                Method = HttpMethod.GET
            };
            Assert.IsTrue(cache.TryGet(req, out var ret, out var cached));

            Assert.AreEqual(2, req.Counter);
            Assert.AreEqual(1, cache.Count);
            Assert.IsFalse(ret.Error);
            Assert.IsFalse(cached);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }, ret.Result);

            // Test cached

            Assert.IsTrue(cache.TryGet(req, out ret, out cached));

            Assert.AreEqual(2, req.Counter);
            Assert.AreEqual(1, cache.Count);
            Assert.IsFalse(ret.Error);
            Assert.IsTrue(cached);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }, ret.Result);

            // Check collection

            var array = cache.ToArray();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(req.Hash, array[0].Key);
            Assert.IsFalse(array[0].Value.Error);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }, array[0].Value.Result);
        }

        [TestMethod]
        public void TestWithoutOracle()
        {
            var initReq = new OracleHttpsRequest()
            {
                URL = new Uri("https://google.es"),
                Method = HttpMethod.GET
            };

            var initRes = OracleResponse.CreateError(initReq.Hash);
            var cache = new OracleExecutionCache(initRes);

            Assert.AreEqual(1, cache.Count);

            // Check collection

            var array = cache.ToArray();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(initReq.Hash, array[0].Key);
            Assert.IsTrue(array[0].Value.Error);
            Assert.AreEqual(null, array[0].Value.Result);

            // Test without cache

            Assert.IsFalse(cache.TryGet(new OracleHttpsRequest()
            {
                URL = new Uri("https://google.es/?p=1"),
                Method = HttpMethod.GET
            }
            , out var ret, out var cached));

            Assert.IsFalse(cached);
            Assert.IsNull(ret);

            // Test cached

            Assert.IsTrue(cache.TryGet(initReq, out ret, out cached));
            Assert.IsNotNull(ret);
            Assert.IsTrue(cached);
            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(ReferenceEquals(ret, initRes));
        }
    }
}
