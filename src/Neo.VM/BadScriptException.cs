// Copyright (C) 2015-2024 The Neo Project.
//
// BadScriptException.cs file belongs to the neo project and is free
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
    /// Represents the exception thrown when the bad script is parsed.
    /// </summary>
    public class BadScriptException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadScriptException"/> class.
        /// </summary>
        public BadScriptException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadScriptException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BadScriptException(string message) : base(message) { }
    }
}
