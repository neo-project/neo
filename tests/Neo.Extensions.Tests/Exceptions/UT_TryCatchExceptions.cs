// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TryCatchExceptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.Exceptions;
using System;

namespace Neo.Extensions.Tests.Exceptions
{
    [TestClass]
    public class UT_TryCatchExceptions
    {
        [TestMethod]
        public void TestTryCatchMethods()
        {
            var actualObject = new object();

            // action
            actualObject.TryCatch(a => actualObject = a = null);
            Assert.IsNull(actualObject);

            // action
            actualObject.TryCatch<object, ArgumentException>(a => throw new ArgumentException(), (_, ex) => actualObject = ex);
            Assert.IsInstanceOfType<ArgumentException>(actualObject);

            var expectedObject = new object();

            // func
            actualObject = expectedObject.TryCatch<object, ArgumentException, ArgumentException>(
                a => throw new ArgumentException(),
                (_, ex) => ex);
            Assert.IsInstanceOfType<ArgumentException>(actualObject);
        }

        [TestMethod]
        public void TestTryCatchThrowMethods()
        {
            var actualObject = new object();

            //action
            Assert.ThrowsExactly<ArgumentException>(
                () => actualObject.TryCatchThrow<object, ArgumentException>(a => throw new ArgumentException()));

            Assert.ThrowsExactly<ArgumentException>(
                () => actualObject.TryCatchThrow<object, ArgumentException, object>(a =>
                {
                    throw new ArgumentException();
                }));

            var expectedMessage = "Hello World";

            try
            {
                actualObject.TryCatchThrow<object, ArgumentException>(a => throw new ArgumentException(), expectedMessage);
            }
            catch (ArgumentException actualException)
            {
                Assert.AreEqual(expectedMessage, actualException.Message);
            }

            try
            {
                actualObject.TryCatchThrow<object, ArgumentException, ArgumentException>(a => throw new ArgumentException(), expectedMessage);
            }
            catch (ArgumentException actualException)
            {
                Assert.AreEqual(expectedMessage, actualException.Message);
            }
        }
    }
}
