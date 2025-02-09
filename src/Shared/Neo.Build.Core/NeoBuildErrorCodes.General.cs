// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildErrorCodes.General.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Core
{
    public partial class NeoBuildErrorCodes
    {
        public sealed class General
        {
            public const int ModuleId = 1;
            private const int ModuleBaseErrorCode = BaseErrorCode * ModuleId;

            public const int InternalException = ModuleBaseErrorCode;

            // IO Exceptions
            public const int FileNotFound = ModuleBaseErrorCode + 100;
            public const int FileAccessDenied = ModuleBaseErrorCode + 101;
            public const int PathNotFound = ModuleBaseErrorCode + 102;

            // Format Exceptions
            public const int InvalidFileFormat = ModuleBaseErrorCode + 200;
            public const int InvalidJsonFormat = ModuleBaseErrorCode + 201;
            public const int InvalidHexFormat = ModuleBaseErrorCode + 202;
            public const int InvalidScriptHashFormat = ModuleBaseErrorCode + 203;
            public const int InvalidHash256Format = ModuleBaseErrorCode + 204;
            public const int InvalidECPointFormat = ModuleBaseErrorCode + 205;
            public const int InvalidNumberFormat = ModuleBaseErrorCode + 206;
            public const int InvalidBooleanFormat = ModuleBaseErrorCode + 207;
            public const int InvalidDateFormat = ModuleBaseErrorCode + 208;
            public const int InvalidBinaryFormat = ModuleBaseErrorCode + 209;
            public const int InvalidEncodingFormat = ModuleBaseErrorCode + 210;
            public const int InvalidVersionFormat = ModuleBaseErrorCode + 211;
            public const int InvalidAddressFormat = ModuleBaseErrorCode + 212;

            private General() { }
        }
    }
}
