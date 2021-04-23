using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;

namespace Neo.UnitTests.IO.Json
{
    [TestClass]
    public class UT_JPath
    {
        private static readonly JObject json = new()
        {
            ["a"] = 1,
            ["b"] = new JObject()
            {
                ["a"] = 2
            }
        };

        [TestMethod]
        public void TestRecursiveDescent()
        {
            JArray array = json.JsonPath("$..a");
            Assert.AreEqual("[1,2]", array.ToString());
        }
    }
}
