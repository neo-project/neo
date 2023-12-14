// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Types
{
    /// <summary>
    /// An enumeration representing the types in the VM.
    /// </summary>
    public enum StackItemType : byte
    {
        /// <summary>
        /// Represents any type.
        /// </summary>
        Any = 0x00,

        /// <summary>
        /// Represents a code pointer.
        /// </summary>
        Pointer = 0x10,

        /// <summary>
        /// Represents the boolean (<see langword="true" /> or <see langword="false" />) type.
        /// </summary>
        Boolean = 0x20,

        /// <summary>
        /// Represents an integer.
        /// </summary>
        Integer = 0x21,

        /// <summary>
        /// Represents an immutable memory block.
        /// </summary>
        ByteString = 0x28,

        /// <summary>
        /// Represents a memory block that can be used for reading and writing.
        /// </summary>
        Buffer = 0x30,

        /// <summary>
        /// Represents an array or a complex object.
        /// </summary>
        Array = 0x40,

        /// <summary>
        /// Represents a structure.
        /// </summary>
        Struct = 0x41,

        /// <summary>
        /// Represents an ordered collection of key-value pairs.
        /// </summary>
        Map = 0x48,

        /// <summary>
        /// Represents an interface used to interoperate with the outside of the the VM.
        /// </summary>
        InteropInterface = 0x60,
    }
}
