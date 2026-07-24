// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractPermissionDescriptor_Edges.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    /// <summary>
    /// Edges not covered by UT_ContractPermissionDescriptor happy-path tests.
    /// </summary>
    [TestClass]
    public class UT_ContractPermissionDescriptor_Edges
    {
        [TestMethod]
        public void CreateWildcard_IsWildcard()
        {
            var d = ContractPermissionDescriptor.CreateWildcard();
            Assert.IsTrue(d.IsWildcard);
            Assert.IsFalse(d.IsHash);
            Assert.IsFalse(d.IsGroup);
            Assert.IsNull(d.ToArray());
            Assert.AreEqual("*", d.ToJson().GetString());
        }

        [TestMethod]
        public void Create_ByHash_ToArray_And_Json()
        {
            var hash = UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf");
            var d = ContractPermissionDescriptor.Create(hash);
            Assert.IsTrue(d.IsHash);
            Assert.IsFalse(d.IsWildcard);
            CollectionAssert.AreEqual(hash.ToArray(), d.ToArray());
            Assert.AreEqual(hash.ToString(), d.ToJson().GetString());
        }

        [TestMethod]
        public void Create_FromStackItem_Null_IsWildcard()
        {
            var d = ContractPermissionDescriptor.Create(StackItem.Null);
            Assert.IsTrue(d.IsWildcard);
        }

        [TestMethod]
        public void Create_FromStackItem_Hash()
        {
            var hash = UInt160.Zero;
            var d = ContractPermissionDescriptor.Create((ByteString)hash.ToArray());
            Assert.IsTrue(d.IsHash);
            Assert.AreEqual(hash, d.Hash);
        }

        [TestMethod]
        public void FromJson_Invalid_Throws()
        {
            Assert.ThrowsExactly<FormatException>(() => ContractPermissionDescriptor.FromJson("not-a-descriptor"));
        }
    }
}
