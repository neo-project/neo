// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WildCardContainer_ToJson.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.SmartContract.Manifest;

namespace Neo.UnitTests.SmartContract.Manifest
{
    /// <summary>
    /// ToJson / CreateWildcard coverage not covered by UT_WildCardContainer.FromJson tests.
    /// </summary>
    [TestClass]
    public class UT_WildCardContainer_ToJson
    {
        [TestMethod]
        public void CreateWildcard_ToJson_IsStar()
        {
            var container = WildcardContainer<string>.CreateWildcard();
            Assert.IsTrue(container.IsWildcard);
            Assert.AreEqual("*", container.ToJson(s => s).AsString());
        }

        [TestMethod]
        public void Create_ToJson_IsArray()
        {
            var container = WildcardContainer<string>.Create("a", "b");
            var json = container.ToJson(s => s);
            Assert.IsInstanceOfType<JArray>(json);
            var arr = (JArray)json;
            Assert.HasCount(2, arr);
            Assert.AreEqual("a", arr[0].AsString());
            Assert.AreEqual("b", arr[1].AsString());
        }

        [TestMethod]
        public void CreateWildcard_Count_IsZero()
        {
            var container = WildcardContainer<int>.CreateWildcard();
            Assert.AreEqual(0, container.Count);
            Assert.IsFalse(container.GetEnumerator().MoveNext());
        }
    }
}
