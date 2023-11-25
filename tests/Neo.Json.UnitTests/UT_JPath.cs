namespace Neo.Json.UnitTests
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
                        ["price"] = null
                    }
                },
                ["bicycle"] = new JObject
                {
                    ["color"] = "red",
                    ["price"] = 19.95
                }
            },
            ["expensive"] = 10,
            ["data"] = null,
        };

        [TestMethod]
        public void TestOOM()
        {
            var filter = "$" + string.Concat(Enumerable.Repeat("[0" + string.Concat(Enumerable.Repeat(",0", 64)) + "]", 6));
            Assert.ThrowsException<InvalidOperationException>(() => JObject.Parse("[[[[[[{}]]]]]]")!.JsonPath(filter));
        }

        [TestMethod]
        public void TestJsonPath()
        {
            Assert.AreEqual(@"[""Nigel Rees"",""Evelyn Waugh"",""Herman Melville"",""J. R. R. Tolkien""]", json.JsonPath("$.store.book[*].author").ToString());
            Assert.AreEqual(@"[""Nigel Rees"",""Evelyn Waugh"",""Herman Melville"",""J. R. R. Tolkien""]", json.JsonPath("$..author").ToString());
            Assert.AreEqual(@"[[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":null}],{""color"":""red"",""price"":19.95}]", json.JsonPath("$.store.*").ToString());
            Assert.AreEqual(@"[19.95,8.95,12.99,8.99,null]", json.JsonPath("$.store..price").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99}]", json.JsonPath("$..book[2]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99}]", json.JsonPath("$..book[-2]").ToString());
            Assert.AreEqual(@"[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99}]", json.JsonPath("$..book[0,1]").ToString());
            Assert.AreEqual(@"[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99}]", json.JsonPath("$..book[:2]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99}]", json.JsonPath("$..book[1:2]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":null}]", json.JsonPath("$..book[-2:]").ToString());
            Assert.AreEqual(@"[{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":null}]", json.JsonPath("$..book[2:]").ToString());
            Assert.AreEqual(@"[{""store"":{""book"":[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":null}],""bicycle"":{""color"":""red"",""price"":19.95}},""expensive"":10,""data"":null}]", json.JsonPath("").ToString());
            Assert.AreEqual(@"[{""book"":[{""category"":""reference"",""author"":""Nigel Rees"",""title"":""Sayings of the Century"",""price"":8.95},{""category"":""fiction"",""author"":""Evelyn Waugh"",""title"":""Sword of Honour"",""price"":12.99},{""category"":""fiction"",""author"":""Herman Melville"",""title"":""Moby Dick"",""isbn"":""0-553-21311-3"",""price"":8.99},{""category"":""fiction"",""author"":""J. R. R. Tolkien"",""title"":""The Lord of the Rings"",""isbn"":""0-395-19395-8"",""price"":null}],""bicycle"":{""color"":""red"",""price"":19.95}},10,null]", json.JsonPath("$.*").ToString());
            Assert.AreEqual(@"[]", json.JsonPath("$..invalidfield").ToString());
        }

        [TestMethod]
        public void TestMaxDepth()
        {
            Assert.ThrowsException<InvalidOperationException>(() => json.JsonPath("$..book[*].author"));
        }

        [TestMethod]
        public void TestInvalidFormat()
        {
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$..*"));
            Assert.ThrowsException<FormatException>(() => json.JsonPath("..book"));
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.."));

            // Test with an empty JSON Path
            // Assert.ThrowsException<FormatException>(() => json.JsonPath(""));

            // Test with only special characters
            Assert.ThrowsException<FormatException>(() => json.JsonPath("@#$%^&*()"));

            // Test with unmatched brackets
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book["));
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book)]"));

            // Test with invalid operators
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book=>2"));

            // Test with incorrect field syntax
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.'book'"));
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.[book]"));

            // Test with unexpected end of expression
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[?(@.price<"));

            // Test with invalid array indexing
            // Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book['one']"));
            // Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[999]"));

            // Test with invalid recursive descent
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$..*..author"));

            // Test with nonexistent functions
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book.length()"));

            // Test with incorrect use of wildcards
            // Assert.ThrowsException<FormatException>(() => json.JsonPath("$.*.store"));

            // Test with improper use of filters
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[?(@.price)]"));

            // Test with mixing of valid and invalid syntax
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[*],$.invalid"));

            // Test with invalid escape sequences
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[\\]"));

            // Test with incorrect property access
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.'b?ook'"));

            // Test with invalid use of wildcard in array index
            // Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[*]"));

            // Test with missing operators in filter expressions
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[?(@.price)]"));

            // Test with incorrect boolean logic in filters
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[?(@.price AND @.title)]"));

            // Test with nested filters without proper closure
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[?(@.price[?(@ < 10)])]"));

            // Test with misplaced recursive descent operator
            // Assert.ThrowsException<FormatException>(() => json.JsonPath("$..store..book"));

            // Test with using JSONPath reserved keywords incorrectly
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$..@.book"));

            // Test with incorrect combinations of valid operators
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book..[0]"));

            // Test with invalid script expressions (if supported)
            Assert.ThrowsException<FormatException>(() => json.JsonPath("$.store.book[(@.length-1)]"));
        }
    }
}
