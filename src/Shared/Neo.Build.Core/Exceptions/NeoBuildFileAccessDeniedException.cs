// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildFileAccessDeniedException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.IO;

namespace Neo.Build.Core.Exceptions
{
    public class NeoBuildFileAccessDeniedException(
        FileInfo file) : NeoBuildException($"Accessing file '{file}' was denied.", NeoBuildErrorCodes.General.FileAccessDenied)
    {
        public NeoBuildFileAccessDeniedException(string filename) : this(new FileInfo(filename)) { }

        public FileInfo FileInfo => file;
    }
}
