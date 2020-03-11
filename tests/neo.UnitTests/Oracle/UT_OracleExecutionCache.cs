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

        private OracleResult OracleLogic(OracleRequest arg)
        {
            var http = (CounterRequest)arg;
            http.Counter++;
            return OracleResult.CreateResult(_txHash, arg.Hash, BitConverter.GetBytes(http.Counter));
        }

        UInt256 _txHash;

        [TestInitialize]
        public void Init()
        {
            var rand = new Random();
            var data = new byte[32];
            rand.NextBytes(data);

            _txHash = new UInt256(data);
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
                Filter = "Filter",
                Method = HttpMethod.GET
            };
            Assert.IsTrue(cache.TryGet(req, out var ret));

            Assert.AreEqual(2, req.Counter);
            Assert.AreEqual(1, cache.Count);
            Assert.AreEqual(OracleResultError.None, ret.Error);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }, ret.Result);

            // Test cached

            Assert.IsTrue(cache.TryGet(req, out ret));

            Assert.AreEqual(2, req.Counter);
            Assert.AreEqual(1, cache.Count);
            Assert.AreEqual(OracleResultError.None, ret.Error);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }, ret.Result);

            // Check collection

            var array = cache.ToArray();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(req.Hash, array[0].Key);
            Assert.AreEqual(_txHash, array[0].Value.TransactionHash);
            Assert.AreEqual(OracleResultError.None, array[0].Value.Error);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }, array[0].Value.Result);
        }

        [TestMethod]
        public void TestWithoutOracle()
        {
            var initReq = new OracleHttpsRequest()
            {
                URL = new Uri("https://google.es"),
                Filter = "Filter",
                Method = HttpMethod.GET
            };

            var initRes = OracleResult.CreateError(_txHash, initReq.Hash, OracleResultError.ServerError);
            var cache = new OracleExecutionCache(initRes);

            Assert.AreEqual(1, cache.Count);

            // Check collection

            var array = cache.ToArray();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(initReq.Hash, array[0].Key);
            Assert.AreEqual(_txHash, array[0].Value.TransactionHash);
            Assert.AreEqual(OracleResultError.ServerError, array[0].Value.Error);
            CollectionAssert.AreEqual(Array.Empty<byte>(), array[0].Value.Result);

            // Test without cache

            Assert.IsFalse(cache.TryGet(new OracleHttpsRequest()
            {
                URL = new Uri("https://google.es/?p=1"),
                Filter = "Filter",
                Method = HttpMethod.GET
            }
            , out var ret));

            Assert.IsNull(ret);

            // Test cached

            Assert.IsTrue(cache.TryGet(initReq, out ret));
            Assert.IsNotNull(ret);
            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(ReferenceEquals(ret, initRes));
        }
    }
}
