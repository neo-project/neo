// Copyright (C) 2015-2024 The Neo Project.
//
// VMUTStackItemType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Test.Types
{
    public enum VMUTStackItemType
    {
        /// <summary>
        /// Null
        /// </summary>
        Null,

        /// <summary>
        /// An address of function
        /// </summary>
        Pointer,

        /// <summary>
        /// Boolean (true,false)
        /// </summary>
        Boolean,

        /// <summary>
        /// ByteString
        /// </summary>
        ByteString,

        /// <summary>	
        /// ByteString as UTF8 string	
        /// </summary>
        String,

        /// <summary>
        /// Mutable byte array
        /// </summary>
        Buffer,

        /// <summary>
        /// InteropInterface
        /// </summary>
        Interop,

        /// <summary>
        /// BigInteger
        /// </summary>
        Integer,

        /// <summary>
        /// Array
        /// </summary>
        Array,

        /// <summary>
        /// Struct
        /// </summary>
        Struct,

        /// <summary>
        /// Map
        /// </summary>
        Map
    }
}
