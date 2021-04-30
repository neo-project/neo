using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;

namespace Neo.UnitTests.IO.Json
{
    [TestClass]
    public class UT_JPath
    {
        private static readonly JObject json = new()
        {
            ["store"] = new JObject
            {
                ["book"] = new JArray
                {
                    new JObject
                    {
                        ["category"] = "reference",
                        ["author"] = "Nigel Rees",
                        ["title"] = "Sayings of the Century",
                        ["price"] = 8.95
                    },
                    new JObject
                    {
                        ["category"] = "fiction",
                        ["author"] = "Evelyn Waugh",
                        ["title"] = "Sword of Honour",
                        ["price"] = 12.99
                    },
                    new JObject
                    {
                        ["category"] = "fiction",
                        ["author"] = "Herman Melville",
                        ["title"] = "Moby Dick",
                        ["isbn"] = "0-553-21311-3",
                        ["price"] = 8.99
                    },
                    new JObject
                    {
                        ["category"] = "fiction",
                        ["author"] = "J. R. R. Tolkien",
                        ["title"] = "The Lord of the Rings",
                        ["isbn"] = "0-395-19395-8",
                        ["price"] = 22.99
                    }
                },
                ["bicycle"] = new JObject
                {
                    ["color"] = "red",
                    ["price"] = 19.95
                }
            },
            ["expensive"] = 10
        };

        [TestMethod]
        public void TestJsonPath()
        {
            Assert.AreEqual(@"[""Nigel Rees"",""Evelyn Waugh"",""Herman Melville"",""J. R. R. Tolkien""]", json.JsonPath("$.store.book[*].author").ToString());
            Assert.AreEqual(@"[""Nigel Rees"",""Evelyn Waugh"",""Herman Melville"",""J. R. R. Tolkien""]", json.JsonPath("$..author").ToString());
            Assert.AreEqual(@"[[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}],{""color"":""red"",""price"":19.95}]", json.JsonPath("$.store.*").ToString());
            Assert.AreEqual(@"[19.95,8.95,12.99,8.99,22.99]", json.JsonPath("$.store..price").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99}]", json.JsonPath("$..book[2]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99}]", json.JsonPath("$..book[-2]").ToString());
            Assert.AreEqual(@"[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99}]", json.JsonPath("$..book[0,1]").ToString());
            Assert.AreEqual(@"[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99}]", json.JsonPath("$..book[:2]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99}]", json.JsonPath("$..book[1:2]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}]", json.JsonPath("$..book[-2:]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}]", json.JsonPath("$..book[2:]").ToString());
        }
    }
}
