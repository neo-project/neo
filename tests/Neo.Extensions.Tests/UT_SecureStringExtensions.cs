// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SecureStringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_SecureStringExtensions
    {
        [TestMethod]
        public void Test_String_To_SecureString()
        {
            var expected = "Hello World";
            var expectedSecureString = expected.ToSecureString();

            var actual = expectedSecureString.GetClearText();

            Assert.IsTrue(expectedSecureString.IsReadOnly());
            Assert.AreEqual(expected, actual);
        }
    }
}
