using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;

namespace Neo.UnitTests.IO.Json
{
    [TestClass]
    public class UT_JPath
    {
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
            Assert.AreEqual(@"[{""book"":[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}],""bicycle"":{""color"":""red"",""price"":19.95}},10,[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99}],{""color"":""red"",""price"":19.95},{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":22.99},""red"",19.95,""reference"",""Nigel Rees"",""Sayings of the Century"",8.95,""fiction"",""Evelyn Waugh"",""Sword of Honour"",12.99,""fiction"",""Herman Melville"",""Moby Dick"",""0-553-21311-3"",8.99,""fiction"",""J. R. R. Tolkien"",""The Lord of the Rings"",""0-395-19395-8"",22.99]", json.JsonPath("$..*").ToString());
        }
    }
}
