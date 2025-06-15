// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoBuildInvalidBooleanFormatException.cs file belongs to the neo project and is free
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
    public class UT_NeoBuildInvalidBooleanFormatException
    {
        [TestMethod]
        public void TestIsPropertiesSet()
        {
            var expectedMessage = "Invalid boolean format.";

            var exception = new NeoBuildInvalidBooleanFormatException();
            var exceptionInterface = exception as INeoBuildException;
            var exceptionBase = exception as Exception;

            // NeoBuildInvalidBooleanFormatException
            Assert.AreEqual(NeoBuildErrorCodes.General.InvalidBooleanFormat, exception.ExitCode);
            Assert.AreEqual(exception.ExitCode, exception.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exception.ExitCode:d04}", exception.ErrorCode);
            Assert.AreEqual($"Error {exception.ErrorCode} {expectedMessage}", exception.Message);
            Assert.AreEqual(exception.Message, $"{exception}");

            // INeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InvalidBooleanFormat, exceptionInterface.ExitCode);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionInterface.ExitCode:d04}", exceptionInterface.ErrorCode);
            Assert.AreEqual($"Error {exceptionInterface.ErrorCode} {expectedMessage}", exceptionInterface.Message);

            // System.Exception
            Assert.AreEqual(NeoBuildErrorCodes.General.InvalidBooleanFormat, exceptionBase.HResult);
            Assert.AreEqual($"Error {NeoBuildErrorCodes.StringPrefix}{(uint)exceptionBase.HResult} {expectedMessage}", exceptionBase.Message);
            Assert.AreEqual(exception.Message, $"{exceptionBase}");
        }
    }
}
