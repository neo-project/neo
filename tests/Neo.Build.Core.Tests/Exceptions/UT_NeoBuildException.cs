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
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exception.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{exception.ExitCode:d04}", exception.ErrorCode);
            Assert.AreEqual($"{exception.ErrorCode} Hello World", exception.Message);
            Assert.AreEqual(exception.Message, $"{exception}");

            // INeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exceptionInterface.ExitCode);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{exceptionInterface.ExitCode:d04}", exceptionInterface.ErrorCode);
            Assert.AreEqual($"{exceptionInterface.ErrorCode} Hello World", exceptionInterface.Message);

            // System.Exception
            Assert.AreEqual(NeoBuildErrorCodes.General.InternalException, exceptionBase.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{exceptionBase.HResult} Hello World", exceptionBase.Message);
            Assert.AreEqual(exception.Message, $"{exceptionBase}");
        }
    }
}
