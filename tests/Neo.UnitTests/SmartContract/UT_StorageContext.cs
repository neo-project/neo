// Copyright (C) 2015-2026 The Neo Project.
//
// UT_StorageContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_StorageContext
    {
        [TestMethod]
        public void Properties_CanBeSetAndRead()
        {
            var ctx = new StorageContext
            {
                Id = 42,
                IsReadOnly = true
            };

            Assert.AreEqual(42, ctx.Id);
            Assert.IsTrue(ctx.IsReadOnly);

            ctx.IsReadOnly = false;
            ctx.Id = -1;
            Assert.AreEqual(-1, ctx.Id);
            Assert.IsFalse(ctx.IsReadOnly);
        }
    }
}
