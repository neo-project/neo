// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoBuildException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using System;

namespace Neo.Build.Core.Tests.Exceptions
{
    [TestClass]
    public class UT_NeoBuildException
    {
        [TestMethod]
        public void TestIsPropertiesSet()
        {
            var exception = new NeoBuildException("Hello World");
            var exceptionInterface = exception as INeoBuildException;
            var exceptionBase = exception as Exception;

            // NeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exception.ExitCode);
            Assert.AreEqual(exception.ExitCode, exception.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exception.ExitCode:d04}", exception.ErrorCode);
            Assert.AreEqual($"{exception.ErrorCode} Hello World", exception.Message);
            Assert.AreEqual(exception.Message, $"{exception}");

            // INeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exceptionInterface.ExitCode);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionInterface.ExitCode:d04}", exceptionInterface.ErrorCode);
            Assert.AreEqual($"{exceptionInterface.ErrorCode} Hello World", exceptionInterface.Message);

            // System.Exception
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exceptionBase.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionBase.HResult} Hello World", exceptionBase.Message);
            Assert.AreEqual(exception.Message, $"{exceptionBase}");
        }

        [TestMethod]
        public void TestIsPropertiesSetForSystemException()
        {
            var exception = new Exception("Hello World");

            var neoException1 = new NeoBuildException(exception);
            var exceptionInterface1 = neoException1 as INeoBuildException;

            var neoException2 = new NeoBuildException(exception, NeoBuildErrorCodes.General.InternalException);
            var exceptionInterface2 = neoException2 as INeoBuildException;

            // NeoBuildException
            Assert.IsTrue(neoException1.ExitCode != 0);
            Assert.AreEqual(neoException1.ExitCode, neoException1.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)neoException1.ExitCode:d04}", neoException1.ErrorCode);
            Assert.AreEqual($"{neoException1.ErrorCode} Hello World", neoException1.Message);
            Assert.AreEqual(neoException1.Message, $"{neoException1}");

            // INeoBuildException
            Assert.IsTrue(exceptionInterface1.ExitCode != 0);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionInterface1.ExitCode:d04}", exceptionInterface1.ErrorCode);
            Assert.AreEqual($"{exceptionInterface1.ErrorCode} Hello World", exceptionInterface1.Message);

            // NeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, neoException2.ExitCode);
            Assert.AreEqual(neoException2.ExitCode, neoException2.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)neoException2.ExitCode:d04}", neoException2.ErrorCode);
            Assert.AreEqual($"{neoException2.ErrorCode} Hello World", neoException2.Message);
            Assert.AreEqual(neoException2.Message, $"{neoException2}");

            // INeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exceptionInterface2.ExitCode);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionInterface2.ExitCode:d04}", exceptionInterface2.ErrorCode);
            Assert.AreEqual($"{exceptionInterface2.ErrorCode} Hello World", exceptionInterface2.Message);
        }
    }
}
