// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JArray.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;
using System.Text.Json.Nodes;

namespace Neo.Json.UnitTests
{
    enum Foo
    {
        male,
        female
    }

    [TestClass]
    public class UT_JArray
    {
        private JsonObject alice;
        private JsonObject bob;

        [TestInitialize]
        public void SetUp()
        {
            alice = new JsonObject()
            {
                ["name"] = "alice",
                ["age"] = 30,
                ["score"] = 100.001,
                ["gender"] = Foo.female.ToString(),
                ["isMarried"] = true,
            };

            var pet1 = new JsonObject()
            {
                ["name"] = "Tom",
                ["type"] = "cat",
            };
            alice["pet"] = pet1;

            bob = new JsonObject()
            {
                ["name"] = "bob",
                ["age"] = 100000,
                ["score"] = 0.001,
                ["gender"] = Foo.male.ToString(),
                ["isMarried"] = false,
            };

            var pet2 = new JsonObject()
            {
                ["name"] = "Paul",
                ["type"] = "dog",
            };
            bob["pet"] = pet2;
        }

        [TestMethod]
        public void TestAdd()
        {
            var jArray = new JsonArray
            {
                alice,
                bob
            };
            var jAlice = jArray[0];
            var jBob = jArray[1];
            Assert.AreEqual(alice["name"].StrictToString(false), jAlice["name"].StrictToString(false));
            Assert.AreEqual(alice["age"].StrictToString(false), jAlice["age"].StrictToString(false));
            Assert.AreEqual(alice["score"].StrictToString(false), jAlice["score"].StrictToString(false));
            Assert.AreEqual(alice["gender"].StrictToString(false), jAlice["gender"].StrictToString(false));
            Assert.AreEqual(alice["isMarried"].StrictToString(false), jAlice["isMarried"].StrictToString(false));
            Assert.AreEqual(alice["pet"].StrictToString(false), jAlice["pet"].StrictToString(false));
            Assert.AreEqual(bob["name"].StrictToString(false), jBob["name"].StrictToString(false));
            Assert.AreEqual(bob["age"].StrictToString(false), jBob["age"].StrictToString(false));
            Assert.AreEqual(bob["score"].StrictToString(false), jBob["score"].StrictToString(false));
            Assert.AreEqual(bob["gender"].StrictToString(false), jBob["gender"].StrictToString(false));
            Assert.AreEqual(bob["isMarried"].StrictToString(false), jBob["isMarried"].StrictToString(false));
            Assert.AreEqual(bob["pet"].StrictToString(false), jBob["pet"].StrictToString(false));
        }

        [TestMethod]
        public void TestSetItem()
        {
            var jArray = new JsonArray
            {
                alice
            };
            jArray[0] = bob;
            Assert.AreEqual(jArray[0], bob);

            Action action = () => jArray[1] = alice;
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);
        }

        [TestMethod]
        public void TestClear()
        {
            var jArray = new JsonArray
            {
                alice
            };
            var jAlice = jArray[0];
            Assert.AreEqual(alice["name"].StrictToString(false), jAlice["name"].StrictToString(false));
            Assert.AreEqual(alice["age"].StrictToString(false), jAlice["age"].StrictToString(false));
            Assert.AreEqual(alice["score"].StrictToString(false), jAlice["score"].StrictToString(false));
            Assert.AreEqual(alice["gender"].StrictToString(false), jAlice["gender"].StrictToString(false));
            Assert.AreEqual(alice["isMarried"].StrictToString(false), jAlice["isMarried"].StrictToString(false));
            Assert.AreEqual(alice["pet"].StrictToString(false), jAlice["pet"].StrictToString(false));

            jArray.Clear();
            Action action = () => jArray[0].StrictToString(false);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);
        }

        [TestMethod]
        public void TestContains()
        {
            var jArray = new JsonArray
            {
                alice
            };
            Assert.Contains(alice, jArray);
            Assert.DoesNotContain(bob, jArray);
        }

        [TestMethod]
        public void TestInsert()
        {
            var jArray = new JsonArray
            {
                alice,
                "Item1",
                "Item2",
                "Item3"
            };

            jArray.Insert(1, bob);
            Assert.AreEqual(5, jArray.Count);
            Assert.AreEqual(alice, jArray[0]);
            Assert.AreEqual(bob, jArray[1]);

            jArray.Insert(5, "Item5");
            Assert.AreEqual(6, jArray.Count);
            Assert.AreEqual("Item5", jArray[5].GetValue<string>());
        }

        [TestMethod]
        public void TestIndexOf()
        {
            var jArray = new JsonArray();
            Assert.AreEqual(-1, jArray.IndexOf(alice));

            jArray.Add(alice);
            jArray.Add("Item1");
            jArray.Add("Item2");
            jArray.Add("Item3");
            Assert.AreEqual(0, jArray.IndexOf(alice));

            jArray.Insert(1, bob);
            Assert.AreEqual(1, jArray.IndexOf(bob));
        }

        [TestMethod]
        public void TestRemove()
        {
            var jArray = new JsonArray
            {
                alice
            };
            Assert.AreEqual(1, jArray.Count);
            jArray.Remove(alice);
            Assert.AreEqual(0, jArray.Count);

            jArray.Add(alice);
            jArray.Add(bob);
            Assert.AreEqual(2, jArray.Count);
            jArray.Remove(alice);
            Assert.AreEqual(1, jArray.Count);
        }

        [TestMethod]
        public void TestRemoveAt()
        {
            var jArray = new JsonArray
            {
                alice,
                bob
            };
            jArray.RemoveAt(1);
            Assert.AreEqual(1, jArray.Count);
            Assert.DoesNotContain(bob, jArray);
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var jArray = new JsonArray
            {
                alice,
                bob
            };
            int i = 0;
            foreach (var item in jArray)
            {
                if (i % 2 == 0) Assert.AreEqual(alice, item);
                if (i % 2 != 0) Assert.AreEqual(bob, item);
                i++;
            }
            Assert.IsNotNull(((IEnumerable)jArray).GetEnumerator());
        }

        [TestMethod]
        public void TestCount()
        {
            var jArray = new JsonArray { alice, bob };
            Assert.HasCount(2, jArray);
        }

        [TestMethod]
        public void TestInvalidIndexAccess()
        {
            var jArray = new JsonArray { alice };
            Action action = () => { var item = jArray[1]; };
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);
        }

        [TestMethod]
        public void TestEmptyEnumeration()
        {
            var jArray = new JsonArray();
            foreach (var item in jArray)
            {
                Assert.Fail("Enumeration should not occur on an empty JArray");
            }
        }

        [TestMethod]
        public void TestAddNullValues()
        {
            var jArray = new JsonArray
            {
                null
            };
            Assert.HasCount(1, jArray);
            Assert.IsNull(jArray[0]);
        }

        [TestMethod]
        public void TestClone()
        {
            var jArray = new JsonArray { alice, bob };
            var clone = (JsonArray)jArray.DeepClone();

            Assert.AreNotSame(jArray, clone);
            Assert.AreEqual(jArray.Count, clone.Count);

            for (int i = 0; i < jArray.Count; i++)
            {
                Assert.AreEqual(jArray[i]?.StrictToString(false), clone[i]?.StrictToString(false));
            }

            var a = jArray.StrictToString(false);
            var b = jArray.DeepClone().StrictToString(false);
            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void TestAddNull()
        {
            var jArray = new JsonArray { null };

            Assert.HasCount(1, jArray);
            Assert.IsNull(jArray[0]);
        }

        [TestMethod]
        public void TestSetNull()
        {
            var jArray = new JsonArray { alice };
            jArray[0] = null;

            Assert.HasCount(1, jArray);
            Assert.IsNull(jArray[0]);
        }

        [TestMethod]
        public void TestInsertNull()
        {
            var jArray = new JsonArray { alice };
            jArray.Insert(0, null);

            Assert.HasCount(2, jArray);
            Assert.IsNull(jArray[0]);
            Assert.AreEqual(alice, jArray[1]);
        }

        [TestMethod]
        public void TestRemoveNull()
        {
            var jArray = new JsonArray { null, alice };
            jArray.Remove(null);

            Assert.HasCount(1, jArray);
            Assert.AreEqual(alice, jArray[0]);
        }

        [TestMethod]
        public void TestContainsNull()
        {
            var jArray = new JsonArray { null, alice };
            Assert.Contains((JsonNode)null, jArray);
            Assert.DoesNotContain(bob, jArray);
        }

        [TestMethod]
        public void TestIndexOfNull()
        {
            var jArray = new JsonArray { null, alice };
            Assert.AreEqual(0, jArray.IndexOf(null));
            Assert.AreEqual(1, jArray.IndexOf(alice));
        }

        [TestMethod]
        public void TestToStringWithNull()
        {
            var jArray = new JsonArray { null, alice, bob };
            var jsonString = jArray.StrictToString(false);
            // JSON string should properly represent the null value
            Assert.AreEqual("[null,{\"name\":\"alice\",\"age\":30,\"score\":100.001,\"gender\":\"female\",\"isMarried\":true,\"pet\":{\"name\":\"Tom\",\"type\":\"cat\"}},{\"name\":\"bob\",\"age\":100000,\"score\":0.001,\"gender\":\"male\",\"isMarried\":false,\"pet\":{\"name\":\"Paul\",\"type\":\"dog\"}}]", jsonString);
        }

        [TestMethod]
        public void TestFromStringWithNull()
        {
            var jsonString = "[null,{\"name\":\"alice\",\"age\":30,\"score\":100.001,\"gender\":\"female\",\"isMarried\":true,\"pet\":{\"name\":\"Tom\",\"type\":\"cat\"}},{\"name\":\"bob\",\"age\":100000,\"score\":0.001,\"gender\":\"male\",\"isMarried\":false,\"pet\":{\"name\":\"Paul\",\"type\":\"dog\"}}]";
            var jArray = (JsonArray)JsonNode.Parse(jsonString);

            Assert.HasCount(3, jArray);
            Assert.IsNull(jArray[0]);

            // Checking the second and third elements
            Assert.AreEqual("alice", jArray[1]["name"].GetValue<string>());
            Assert.AreEqual("bob", jArray[2]["name"].GetValue<string>());
        }
    }
}
