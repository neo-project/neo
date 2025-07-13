// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoBuildWalletAccountNotFoundException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Build.Core.Exceptions.Wallet;
using System;

namespace Neo.Build.Core.Tests.Exceptions.Wallet
{
    [TestClass]
    public class UT_NeoBuildWalletAccountNotFoundException
    {
        [TestMethod]
        public void TestIsPropertiesSet()
        {
            var expectedAddressString = $"{UInt160.Zero}";
            var expectedMessageString = $"Wallet account '{expectedAddressString}' not found.";

            var exception = new NeoBuildWalletAccountNotFoundException(expectedAddressString);
            var exceptionInterface = exception as INeoBuildException;
            var exceptionBase = exception as Exception;

            // NeoBuildWalletAccountNotFoundException
            Assert.AreEqual(NeoBuildErrorCodes.Wallet.AccountNotFound, exception.ExitCode);
            Assert.AreEqual(exception.ExitCode, exception.HResult);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exception.ExitCode:d04}", exception.ErrorCode);
            Assert.AreEqual($"Error {exception.ErrorCode} {expectedMessageString}", exception.Message);
            Assert.AreEqual(exception.Message, $"{exception}");
            Assert.AreEqual(expectedAddressString, exception.WalletAddress);

            // INeoBuildException
            Assert.AreEqual(NeoBuildErrorCodes.Wallet.AccountNotFound, exceptionInterface.ExitCode);
            Assert.AreEqual($"{NeoBuildErrorCodes.StringPrefix}{(uint)exceptionInterface.ExitCode:d04}", exceptionInterface.ErrorCode);
            Assert.AreEqual($"Error {exceptionInterface.ErrorCode} {expectedMessageString}", exceptionInterface.Message);

            // System.Exception
            Assert.AreEqual(NeoBuildErrorCodes.Wallet.AccountNotFound, exceptionBase.HResult);
            Assert.AreEqual($"Error {NeoBuildErrorCodes.StringPrefix}{(uint)exceptionBase.HResult} {expectedMessageString}", exceptionBase.Message);
            Assert.AreEqual(exception.Message, $"{exceptionBase}");
        }
    }
}
