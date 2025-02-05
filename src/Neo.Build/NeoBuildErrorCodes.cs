// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildErrorCodes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build
{
    internal static partial class NeoBuildErrorCodes
    {
        private const string ErrorCodePrefix = "NB";

        public static string MakeErrorCode(int errorCode) =>
            $"{ErrorCodePrefix}{errorCode:04}";
    }
}
