using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;

namespace Neo.UnitTests.IO.Json
{
    [TestClass]
    public class UT_JPath
    {
        [TestMethod]
        public void TestRecursiveDescent()
        {
            JObject json = new()
            {
                ["a"] = 1,
                ["b"] = new JObject()
                {
                    ["a"] = 2
                }
            };
            JArray array = json.JsonPath("$..a");
            Assert.AreEqual("[1,2]", array.ToString());
        }

        [TestMethod]
        public void TestJsonPath()
        {
            var json = JObject.Parse(@"
{
    ""store"": {
        ""book"": [
            {
                ""category"": ""reference"",
                ""author"": ""Nigel Rees"",
                ""title"": ""Sayings of the Century"",
                ""price"": 8.95
            },
            {
                ""category"": ""fiction"",
                ""author"": ""Evelyn Waugh"",
                ""title"": ""Sword of Honour"",
                ""price"": 12.99
            },
            {
                ""category"": ""fiction"",
                ""author"": ""Herman Melville"",
                ""title"": ""Moby Dick"",
                ""isbn"": ""0-553-21311-3"",
                ""price"": 8.99
            },
            {
                ""category"": ""fiction"",
                ""author"": ""J. R. R. Tolkien"",
                ""title"": ""The Lord of the Rings"",
                ""isbn"": ""0-395-19395-8"",
                ""price"": 22.99
            }
        ],
        ""bicycle"": {
                ""color"": ""red"",
            ""price"": 19.95
        }
        },
    ""expensive"": 10
}");

            // Test

            Assert.AreEqual(@"[""Nigel Rees"",""Evelyn Waugh"",""Herman Melville"",""J. R. R. Tolkien""]", json.JsonPath("$.store.book[*].author").ToString());
            Assert.AreEqual(@"[[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}],{""color"":""red"",""price"":19.95}]", json.JsonPath("$.store.*").ToString());

            Assert.AreEqual(@"[]", json.JsonPath("$..author").ToString()); // Wrong (All authors)
            Assert.AreEqual(@"[19.95]", json.JsonPath("$.store..price").ToString()); // Wrong (The price of everything)

            // TODO

            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[2]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[-2]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[0,1]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[:2]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[1:2]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[-2:]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..book[2:]").ToString());
            //Assert.AreEqual(@"[1,2]", json.JsonPath("$..*").ToString());
        }
    }
}
