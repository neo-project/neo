// Copyright (C) 2015-2024 The Neo Project.
//
// UnsafeData.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Extensions
{
    public static class UnsafeData
    {
        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        public static int GetVarSize(int value)
        {
            if (value < 0xFD)
                return sizeof(byte);
            else if (value <= 0xFFFF)
                return sizeof(byte) + sizeof(ushort);
            else
                return sizeof(byte) + sizeof(uint);
        }
    }
}
