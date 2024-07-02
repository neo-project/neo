// Copyright (C) 2015-2024 The Neo Project.
//
// CompressionLevel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace LevelDB
{
    /// <summary>
    /// DB contents are stored in a set of blocks, each of which holds a
    /// sequence of key,value pairs.  Each block may be compressed before
    /// being stored in a file. The following enum describes which
    /// compression method (if any) is used to compress a block.
    /// </summary>
    public enum CompressionLevel
    {
        NoCompression = 0,
        SnappyCompression = 1
    }
}
