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

namespace Neo.Build
{
    internal static partial class NeoBuildErrorCodes
    {
        /// <summary>
        /// All error codes within the build process
        /// related to miss caught exceptions.
        /// </summary>
        public sealed class General
        {
            internal const int Base = 1000;

            private General() { }

            // Unknown Exception
            public static int InternalCrash => Base;

            // File Exceptions
            public static int FileNotFound => Base + 1;
            public static int FileAccessDenied => Base + 2;

            // Format Exceptions
            public static int InvalidFormat => Base + 10;
            public static int InvalidInputFormat => Base + 11;
            public static int InvalidJsonFormat => Base + 12;
            public static int InvalidFileFormat => Base + 13;
        }
    }
}
