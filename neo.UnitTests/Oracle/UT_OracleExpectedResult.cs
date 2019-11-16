using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Oracle;
using System.Linq;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleExpectedResult
    {
        [TestMethod]
        public void TestSerialization()
        {
            var a = new OracleExpectedResult();
            var b = new OracleExpectedResult();

            CollectionAssert.AreEqual(((ISerializable)a).ToArray(), ((ISerializable)b).ToArray());
            Assert.AreEqual(0, a.Count);

            b = new OracleExpectedResult(new OracleResultsCache(
                OracleResult.CreateError(UInt256.Zero, UInt160.Zero, OracleResultError.FilterError)));

            CollectionAssert.AreNotEqual(((ISerializable)a).ToArray(), ((ISerializable)b).ToArray());

            a = ((ISerializable)b).ToArray().AsSerializable<OracleExpectedResult>();
            CollectionAssert.AreEqual(((ISerializable)a).ToArray(), ((ISerializable)b).ToArray());
            Assert.AreEqual(1, a.Count);

            CollectionAssert.AreEqual(a.Select(u => u.Key).ToArray(), b.Select(u => u.Key).ToArray());
            CollectionAssert.AreEqual(a.Select(u => u.Value).ToArray(), b.Select(u => u.Value).ToArray());
        }

        [TestMethod]
        public void TestMatch()
        {
            var attrib = new OracleExpectedResult();

            // Empty

            var cache = new OracleResultsCache();
            Assert.IsTrue(attrib.Match(cache));

            // Distinct length

            cache = new OracleResultsCache(OracleResult.CreateError(UInt256.Zero, UInt160.Zero, OracleResultError.FilterError));
            Assert.IsFalse(attrib.Match(cache));

            // Equal length, but distinct

            attrib = new OracleExpectedResult(new OracleResultsCache(OracleResult.CreateError(UInt256.Zero, UInt160.Zero, OracleResultError.PolicyError)));
            Assert.IsFalse(attrib.Match(cache));

            // Equal all (with expected hash)

            attrib = new OracleExpectedResult(new OracleResultsCache(OracleResult.CreateError(UInt256.Zero, UInt160.Zero, OracleResultError.FilterError)), true);
            Assert.IsTrue(attrib.Match(cache));

            // Equal all (without expected hash)

            attrib = new OracleExpectedResult(new OracleResultsCache(OracleResult.CreateError(UInt256.Zero, UInt160.Zero, OracleResultError.FilterError)), false);
            Assert.IsTrue(attrib.Match(cache));
        }
    }
}
