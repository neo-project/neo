// Copyright (C) 2015-2024 The Neo Project.
//
// OperandSizeAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
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
