// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TransactionAttributesBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_TransactionAttributesBuilder
    {
        [TestMethod]
        public void TestCreateEmpty()
        {
            var builder = TransactionAttributesBuilder.CreateEmpty();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void TestConflict()
        {
            var attr = TransactionAttributesBuilder.CreateEmpty()
                .AddConflict(c => c.Hash = UInt256.Zero)
                .Build();

            Assert.IsNotNull(attr);
            Assert.AreEqual(1, attr.Length);
            Assert.IsInstanceOfType<Conflicts>(attr[0]);
            Assert.AreEqual(UInt256.Zero, ((Conflicts)attr[0]).Hash);
        }

        [TestMethod]
        public void TestOracleResponse()
        {
            var attr = TransactionAttributesBuilder.CreateEmpty()
                .AddOracleResponse(c =>
                {
                    c.Id = 1ul;
                    c.Code = OracleResponseCode.Success;
                    c.Result = new byte[] { 0x01, 0x02, 0x03 };
                })
                .Build();

            Assert.IsNotNull(attr);
            Assert.AreEqual(1, attr.Length);
            Assert.IsInstanceOfType<OracleResponse>(attr[0]);
            Assert.AreEqual(1ul, ((OracleResponse)attr[0]).Id);
            Assert.AreEqual(OracleResponseCode.Success, ((OracleResponse)attr[0]).Code);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, ((OracleResponse)attr[0]).Result.ToArray());
        }

        [TestMethod]
        public void TestHighPriority()
        {
            var attr = TransactionAttributesBuilder.CreateEmpty()
                .AddHighPriority()
                .Build();

            Assert.IsNotNull(attr);
            Assert.AreEqual(1, attr.Length);
            Assert.IsInstanceOfType<HighPriorityAttribute>(attr[0]);
        }

        [TestMethod]
        public void TestNotValidBefore()
        {
            var attr = TransactionAttributesBuilder.CreateEmpty()
                .AddNotValidBefore(10u)
                .Build();

            Assert.IsNotNull(attr);
            Assert.AreEqual(1, attr.Length);
            Assert.IsInstanceOfType<NotValidBefore>(attr[0]);
            Assert.AreEqual(10u, ((NotValidBefore)attr[0]).Height);
        }
    }
}
