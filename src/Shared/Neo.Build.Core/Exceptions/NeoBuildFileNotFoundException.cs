// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildFileNotFoundException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Core.Exceptions
{
    public class NeoBuildFileNotFoundException(
        string filename) : NeoBuildException($"File '{filename}' was not found.", NeoBuildErrorCodes.General.FileNotFound)
    {
        public string Filename => filename;
    }
}
