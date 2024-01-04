// Copyright (C) 2015-2024 The Neo Project.
//
// SeekDirection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Persistence
{
    /// <summary>
    /// Represents the direction when searching from the database.
    /// </summary>
    public enum SeekDirection : sbyte
    {
        /// <summary>
        /// Indicates that the search should be performed in ascending order.
        /// </summary>
        Forward = 1,

        /// <summary>
        /// Indicates that the search should be performed in descending order.
        /// </summary>
        Backward = -1
    }
}
