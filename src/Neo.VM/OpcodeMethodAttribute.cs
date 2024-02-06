// Copyright (C) 2015-2024 The Neo Project.
//
// OpcodeMethodAttribute.cs file belongs to the neo project and is free
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
    /// Indicates the <see cref="OpCode"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OpcodeMethodAttribute : Attribute
    {
        /// <summary>
        /// The method is for this Opcode
        /// </summary>
        public OpCode OpCode { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="opCode">OpCode</param>
        public OpcodeMethodAttribute(OpCode opCode)
        {
            OpCode = opCode;
        }
    }
}
