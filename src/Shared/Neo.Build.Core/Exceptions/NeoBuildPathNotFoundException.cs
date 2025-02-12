// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildPathNotFoundException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Core.Exceptions
{
    public class NeoBuildPathNotFoundException(
        string path) : NeoBuildException($"Path '{path}' was not found.", NeoBuildErrorCodes.General.PathNotFound)
    {
        public string Path => path;
    }
}
