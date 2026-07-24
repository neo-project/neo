// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SignException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Sign;
using System;

namespace Neo.UnitTests.Sign
{
    [TestClass]
    public class UT_SignException
    {
        [TestMethod]
        public void Constructor_MessageOnly()
        {
            var ex = new SignException("failed to sign");
            Assert.AreEqual("failed to sign", ex.Message);
            Assert.IsNull(ex.InnerException);
        }

        [TestMethod]
        public void Constructor_MessageAndCause()
        {
            var cause = new InvalidOperationException("inner");
            var ex = new SignException("outer", cause);
            Assert.AreEqual("outer", ex.Message);
            Assert.AreSame(cause, ex.InnerException);
        }

        [TestMethod]
        public void IsException()
        {
            Exception ex = new SignException("x");
            Assert.IsInstanceOfType<SignException>(ex);
        }
    }
}
