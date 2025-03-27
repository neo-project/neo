// Copyright (C) 2015-2025 The Neo Project.
//
// StackItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM.Fuzzer.Runners
{
    /// <summary>
    /// Represents an item on the VM execution stack
    /// </summary>
    public class StackItem
    {
        /// <summary>
        /// Gets the type of the stack item
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the string representation of the stack item value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the StackItem class
        /// </summary>
        /// <param name="type">The type of the stack item</param>
        /// <param name="value">The string representation of the value</param>
        public StackItem(string type, string value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Returns a string representation of the stack item
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {Value}";
        }
    }
}
