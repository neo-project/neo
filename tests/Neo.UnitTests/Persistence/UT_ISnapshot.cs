// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ISnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Persistence;
using System;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_ISnapshot
    {
        [TestMethod]
        public void LoadFromJsonTest()
        {
            using MemoryStore store = new();
            using var snapshot = store.GetSnapshot();

            // empty

            var entries = store.Seek(Array.Empty<byte>(), SeekDirection.Forward).ToArray();
            Assert.AreEqual(entries.Length, 0);

            // simple object

            var json = @"{""a2V5"":""dmFsdWU=""}";

            snapshot.LoadFromJson((JObject)JToken.Parse(json));
            snapshot.Commit();

            entries = store.Seek(Array.Empty<byte>(), SeekDirection.Forward).ToArray();
            Assert.AreEqual(entries.Length, 1);

            Assert.AreEqual("key", Encoding.ASCII.GetString(entries[0].Key));
            Assert.AreEqual("value", Encoding.ASCII.GetString(entries[0].Value));

            // prefix object

            json = @"{""bXkt"":{""a2V5"":""bXktdmFsdWU=""}}";

            snapshot.LoadFromJson((JObject)JToken.Parse(json));
            snapshot.Commit();

            entries = store.Seek(Array.Empty<byte>(), SeekDirection.Forward).ToArray();
            Assert.AreEqual(entries.Length, 2);

            Assert.AreEqual("key", Encoding.ASCII.GetString(entries[0].Key));
            Assert.AreEqual("value", Encoding.ASCII.GetString(entries[0].Value));

            Assert.AreEqual("my-key", Encoding.ASCII.GetString(entries[1].Key));
            Assert.AreEqual("my-value", Encoding.ASCII.GetString(entries[1].Value));
        }
    }
}
