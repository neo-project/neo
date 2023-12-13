// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM
{
    /// <summary>
    /// Indicates the operand length of an <see cref="OpCode"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OperandSizeAttribute : Attribute
    {
        /// <summary>
        /// When it is greater than 0, indicates the size of the operand.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// When it is greater than 0, indicates the size prefix of the operand.
        /// </summary>
        public int SizePrefix { get; set; }
    }
}
