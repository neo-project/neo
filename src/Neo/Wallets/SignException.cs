// Copyright (C) 2015-2025 The Neo Project.
//
// SignException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Wallets
{
    /// <summary>
    /// The exception that is thrown when `Sign` fails.
    /// </summary>
    public class SignException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SignException(string message) : base(message) { }
    }
}
