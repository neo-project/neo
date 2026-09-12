// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ScriptBuilderExtensions_Edges.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.VM;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.Extensions
{
    /// <summary>
    /// CreateArray/CreateMap edges not covered by UT_Helper emit tests.
    /// </summary>
    [TestClass]
    public class UT_ScriptBuilderExtensions_Edges
    {
        [TestMethod]
        public void CreateArray_NullOrEmpty_EmitsNewArray0()
        {
            using var sbNull = new ScriptBuilder();
            sbNull.CreateArray<int>(null);
            Assert.AreEqual((byte)OpCode.NEWARRAY0, sbNull.ToArray()[0]);

            using var sbEmpty = new ScriptBuilder();
            sbEmpty.CreateArray(System.Array.Empty<int>());
            Assert.AreEqual((byte)OpCode.NEWARRAY0, sbEmpty.ToArray()[0]);
        }

        [TestMethod]
        public void CreateMap_Empty_EmitsNewMap()
        {
            using var sb = new ScriptBuilder();
            sb.CreateMap(new Dictionary<BigInteger, BigInteger>());
            Assert.AreEqual((byte)OpCode.NEWMAP, sb.ToArray()[0]);
        }

        [TestMethod]
        public void CreateMap_Enumerable_Empty_EmitsNewMap()
        {
            using var sb = new ScriptBuilder();
            IEnumerable<KeyValuePair<int, int>> empty = [];
            sb.CreateMap(empty);
            Assert.AreEqual((byte)OpCode.NEWMAP, sb.ToArray()[0]);
        }

        [TestMethod]
        public void CreateArray_NonEmpty_StartsWithPushes()
        {
            using var sb = new ScriptBuilder();
            sb.CreateArray(new BigInteger[] { 1, 2 });
            var bytes = sb.ToArray();
            Assert.IsTrue(bytes.Length > 2);
            Assert.AreEqual((byte)OpCode.PACK, bytes[^1]);
        }
    }
}
