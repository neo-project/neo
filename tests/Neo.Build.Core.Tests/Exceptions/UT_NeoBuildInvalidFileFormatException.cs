// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoBuildInvalidFileFormatException.cs file belongs to the neo project and is free
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
    public class UT_NeoBuildInvalidFileFormatException
    {
        [TestMethod]
        public void TestIsPropertiesSet()
        {
            var expectedFilename = "/path/to/filename";
            var expectedMessage = $"File '{expectedFilename}' is not in the correct format.";

            var exception = new NeoBuildInvalidFileFormatException(expectedFilename);
            var exceptionInterface = exception as INeoBuildException;
            var exceptionBase = exception as Exception;

            // NeoBuildInvalidFileFormatException
            Assert.AreEqual(NeoBuildErrorCodes.General.InvalidFileFormat, exception.ExitCode);
            Assert.AreEqual(exception.ExitCode, exception.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exception.ExitCode:d04}", exception.ErrorCode);
            Assert.AreEqual($"Error {exception.ErrorCode} {expectedMessage}", exception.Message);
            Assert.AreEqual(exception.Message, $"{exception}");
            Assert.AreEqual(expectedFilename, exception.Filename);

            // INeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.General.InvalidFileFormat, exceptionInterface.ExitCode);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionInterface.ExitCode:d04}", exceptionInterface.ErrorCode);
            Assert.AreEqual($"Error {exceptionInterface.ErrorCode} {expectedMessage}", exceptionInterface.Message);

            // System.Exception
            Assert.AreEqual(NeoBuildErrorCodes.General.InvalidFileFormat, exceptionBase.HResult);
            Assert.AreEqual($"Error {NeoBuildErrorCodes.StringPrefix}{(uint)exceptionBase.HResult} {expectedMessage}", exceptionBase.Message);
            Assert.AreEqual(exception.Message, $"{exceptionBase}");
        }
    }
}
