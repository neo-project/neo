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
        private JObject alice;
        private JObject bob;

        [TestInitialize]
        public void SetUp()
        {
            alice = new JObject();
            alice["name"] = "alice";
            alice["age"] = 30;
            alice["score"] = 100.001;
            alice["gender"] = Foo.female;
            alice["isMarried"] = true;
            var pet1 = new JObject();
            pet1["name"] = "Tom";
            pet1["type"] = "cat";
            alice["pet"] = pet1;

            bob = new JObject();
            bob["name"] = "bob";
            bob["age"] = 100000;
            bob["score"] = 0.001;
            bob["gender"] = Foo.male;
            bob["isMarried"] = false;
            var pet2 = new JObject();
            pet2["name"] = "Paul";
            pet2["type"] = "dog";
            bob["pet"] = pet2;
        }

        [TestMethod]
        public void TestAdd()
        {
            var jArray = new JArray
            {
                alice,
                bob
            };
            var jAlice = jArray[0];
            var jBob = jArray[1];
            Assert.AreEqual(alice["name"].ToString(), jAlice["name"].ToString());
            Assert.AreEqual(alice["age"].ToString(), jAlice["age"].ToString());
            Assert.AreEqual(alice["score"].ToString(), jAlice["score"].ToString());
            Assert.AreEqual(alice["gender"].ToString(), jAlice["gender"].ToString());
            Assert.AreEqual(alice["isMarried"].ToString(), jAlice["isMarried"].ToString());
            Assert.AreEqual(alice["pet"].ToString(), jAlice["pet"].ToString());
            Assert.AreEqual(bob["name"].ToString(), jBob["name"].ToString());
            Assert.AreEqual(bob["age"].ToString(), jBob["age"].ToString());
            Assert.AreEqual(bob["score"].ToString(), jBob["score"].ToString());
            Assert.AreEqual(bob["gender"].ToString(), jBob["gender"].ToString());
            Assert.AreEqual(bob["isMarried"].ToString(), jBob["isMarried"].ToString());
            Assert.AreEqual(bob["pet"].ToString(), jBob["pet"].ToString());
        }

        [TestMethod]
        public void TestSetItem()
        {
            var jArray = new JArray
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
            var jArray = new JArray
            {
                alice
            };
            var jAlice = jArray[0];
            Assert.AreEqual(alice["name"].ToString(), jAlice["name"].ToString());
            Assert.AreEqual(alice["age"].ToString(), jAlice["age"].ToString());
            Assert.AreEqual(alice["score"].ToString(), jAlice["score"].ToString());
            Assert.AreEqual(alice["gender"].ToString(), jAlice["gender"].ToString());
            Assert.AreEqual(alice["isMarried"].ToString(), jAlice["isMarried"].ToString());
            Assert.AreEqual(alice["pet"].ToString(), jAlice["pet"].ToString());

            jArray.Clear();
            Action action = () => jArray[0].ToString();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);
        }

        [TestMethod]
        public void TestContains()
        {
            var jArray = new JArray
            {
                alice
            };
            Assert.IsTrue(jArray.Contains(alice));
            Assert.IsFalse(jArray.Contains(bob));
        }

        [TestMethod]
        public void TestCopyTo()
        {
            var jArray = new JArray
            {
                alice,
                bob
            };

            JObject[] jObjects1 = new JObject[2];
            jArray.CopyTo(jObjects1, 0);
            var jAlice1 = jObjects1[0];
            var jBob1 = jObjects1[1];
            Assert.AreEqual(alice, jAlice1);
            Assert.AreEqual(bob, jBob1);

            JObject[] jObjects2 = new JObject[4];
            jArray.CopyTo(jObjects2, 2);
            var jAlice2 = jObjects2[2];
            var jBob2 = jObjects2[3];
            Assert.IsNull(jObjects2[0]);
            Assert.IsNull(jObjects2[1]);
            Assert.AreEqual(alice, jAlice2);
            Assert.AreEqual(bob, jBob2);
        }

        [TestMethod]
        public void TestInsert()
        {
            var jArray = new JArray
            {
                alice,
                alice,
                alice,
                alice
            };

            jArray.Insert(1, bob);
            Assert.AreEqual(5, jArray.Count());
            Assert.AreEqual(alice, jArray[0]);
            Assert.AreEqual(bob, jArray[1]);
            Assert.AreEqual(alice, jArray[2]);

            jArray.Insert(5, bob);
            Assert.AreEqual(6, jArray.Count());
            Assert.AreEqual(bob, jArray[5]);
        }

        [TestMethod]
        public void TestIndexOf()
        {
            var jArray = new JArray();
            Assert.AreEqual(-1, jArray.IndexOf(alice));

            jArray.Add(alice);
            jArray.Add(alice);
            jArray.Add(alice);
            jArray.Add(alice);
            Assert.AreEqual(0, jArray.IndexOf(alice));

            jArray.Insert(1, bob);
            Assert.AreEqual(1, jArray.IndexOf(bob));
        }

        [TestMethod]
        public void TestIsReadOnly()
        {
            var jArray = new JArray();
            Assert.IsFalse(jArray.IsReadOnly);
        }

        [TestMethod]
        public void TestRemove()
        {
            var jArray = new JArray
            {
                alice
            };
            Assert.AreEqual(1, jArray.Count());
            jArray.Remove(alice);
            Assert.AreEqual(0, jArray.Count());

            jArray.Add(alice);
            jArray.Add(alice);
            Assert.AreEqual(2, jArray.Count());
            jArray.Remove(alice);
            Assert.AreEqual(1, jArray.Count());
        }

        [TestMethod]
        public void TestRemoveAt()
        {
            var jArray = new JArray
            {
                alice,
                bob,
                alice
            };
            jArray.RemoveAt(1);
            Assert.AreEqual(2, jArray.Count());
            Assert.IsFalse(jArray.Contains(bob));
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var jArray = new JArray
            {
                alice,
                bob,
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
        public void TestAsString()
        {
            var jArray = new JArray
            {
                alice,
                bob,
            };
            var s = jArray.AsString();
            Assert.AreEqual(s, "[{\"name\":\"alice\",\"age\":30,\"score\":100.001,\"gender\":\"female\",\"isMarried\":true,\"pet\":{\"name\":\"Tom\",\"type\":\"cat\"}},{\"name\":\"bob\",\"age\":100000,\"score\":0.001,\"gender\":\"male\",\"isMarried\":false,\"pet\":{\"name\":\"Paul\",\"type\":\"dog\"}}]");
        }

        [TestMethod]
        public void TestCount()
        {
            var jArray = new JArray { alice, bob };
            Assert.AreEqual(2, jArray.Count);
        }

        [TestMethod]
        public void TestInvalidIndexAccess()
        {
            var jArray = new JArray { alice };
            Action action = () => { var item = jArray[1]; };
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);
        }

        [TestMethod]
        public void TestEmptyEnumeration()
        {
            var jArray = new JArray();
            foreach (var item in jArray)
            {
                Assert.Fail("Enumeration should not occur on an empty JArray");
            }
        }

        [TestMethod]
        public void TestImplicitConversionFromJTokenArray()
        {
            JToken[] jTokens = { alice, bob };
            JArray jArray = jTokens;

            Assert.AreEqual(2, jArray.Count);
            Assert.AreEqual(alice, jArray[0]);
            Assert.AreEqual(bob, jArray[1]);
        }

        [TestMethod]
        public void TestAddNullValues()
        {
            var jArray = new JArray
            {
                null
            };
            Assert.AreEqual(1, jArray.Count);
            Assert.IsNull(jArray[0]);
        }

        [TestMethod]
        public void TestClone()
        {
            var jArray = new JArray { alice, bob };
            var clone = (JArray)jArray.Clone();

            Assert.AreNotSame(jArray, clone);
            Assert.AreEqual(jArray.Count, clone.Count);

            for (int i = 0; i < jArray.Count; i++)
            {
                Assert.AreEqual(jArray[i]?.AsString(), clone[i]?.AsString());
            }

            var a = jArray.AsString();
            var b = jArray.Clone().AsString();
            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void TestReadOnlyBehavior()
        {
            var jArray = new JArray();
            Assert.IsFalse(jArray.IsReadOnly);
        }

        [TestMethod]
        public void TestAddNull()
        {
            var jArray = new JArray { null };

            Assert.AreEqual(1, jArray.Count);
            Assert.IsNull(jArray[0]);
        }

        [TestMethod]
        public void TestSetNull()
        {
            var jArray = new JArray { alice };
            jArray[0] = null;

            Assert.AreEqual(1, jArray.Count);
            Assert.IsNull(jArray[0]);
        }

        [TestMethod]
        public void TestInsertNull()
        {
            var jArray = new JArray { alice };
            jArray.Insert(0, null);

            Assert.AreEqual(2, jArray.Count);
            Assert.IsNull(jArray[0]);
            Assert.AreEqual(alice, jArray[1]);
        }

        [TestMethod]
        public void TestRemoveNull()
        {
            var jArray = new JArray { null, alice };
            jArray.Remove(null);

            Assert.AreEqual(1, jArray.Count);
            Assert.AreEqual(alice, jArray[0]);
        }

        [TestMethod]
        public void TestContainsNull()
        {
            var jArray = new JArray { null, alice };
            Assert.IsTrue(jArray.Contains(null));
            Assert.IsFalse(jArray.Contains(bob));
        }

        [TestMethod]
        public void TestIndexOfNull()
        {
            var jArray = new JArray { null, alice };
            Assert.AreEqual(0, jArray.IndexOf(null));
            Assert.AreEqual(1, jArray.IndexOf(alice));
        }

        [TestMethod]
        public void TestCopyToWithNull()
        {
            var jArray = new JArray { null, alice };
            JObject[] jObjects = new JObject[2];
            jArray.CopyTo(jObjects, 0);

            Assert.IsNull(jObjects[0]);
            Assert.AreEqual(alice, jObjects[1]);
        }

        [TestMethod]
        public void TestToStringWithNull()
        {
            var jArray = new JArray { null, alice, bob };
            var jsonString = jArray.ToString();
            var asString = jArray.AsString();
            // JSON string should properly represent the null value
            Assert.AreEqual("[null,{\"name\":\"alice\",\"age\":30,\"score\":100.001,\"gender\":\"female\",\"isMarried\":true,\"pet\":{\"name\":\"Tom\",\"type\":\"cat\"}},{\"name\":\"bob\",\"age\":100000,\"score\":0.001,\"gender\":\"male\",\"isMarried\":false,\"pet\":{\"name\":\"Paul\",\"type\":\"dog\"}}]", jsonString);
            Assert.AreEqual("[null,{\"name\":\"alice\",\"age\":30,\"score\":100.001,\"gender\":\"female\",\"isMarried\":true,\"pet\":{\"name\":\"Tom\",\"type\":\"cat\"}},{\"name\":\"bob\",\"age\":100000,\"score\":0.001,\"gender\":\"male\",\"isMarried\":false,\"pet\":{\"name\":\"Paul\",\"type\":\"dog\"}}]", asString);
        }

        [TestMethod]
        public void TestFromStringWithNull()
        {
            var jsonString = "[null,{\"name\":\"alice\",\"age\":30,\"score\":100.001,\"gender\":\"female\",\"isMarried\":true,\"pet\":{\"name\":\"Tom\",\"type\":\"cat\"}},{\"name\":\"bob\",\"age\":100000,\"score\":0.001,\"gender\":\"male\",\"isMarried\":false,\"pet\":{\"name\":\"Paul\",\"type\":\"dog\"}}]";
            var jArray = (JArray)JArray.Parse(jsonString);

            Assert.AreEqual(3, jArray.Count);
            Assert.IsNull(jArray[0]);

            // Checking the second and third elements
            Assert.AreEqual("alice", jArray[1]["name"].AsString());
            Assert.AreEqual("bob", jArray[2]["name"].AsString());
        }
    }
}
