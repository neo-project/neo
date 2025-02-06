// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildErrorCodes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Build
{
    internal static partial class NeoBuildErrorCodes
    {
        private const string ErrorCodePrefix = "NB";

        public static string MakeErrorCode(int errorCode) =>
            $"{ErrorCodePrefix}{errorCode:d04}";

        public static string FormatErrorMessage(
            INeoBuildException exception,
            [DisallowNull] string message) =>
            $"{exception.ErrorCode}: {message}";

        public static string FormatErrorMessage(
            INeoBuildException exception,
            [DisallowNull] string message,
            [DisallowNull] Exception innerException) =>
            $"{FormatErrorMessage(exception, message)} {innerException.Message}";
    }
}
